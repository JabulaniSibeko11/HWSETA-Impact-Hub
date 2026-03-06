using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace HWSETA_Impact_Hub.Infrastructure.Encryption
{
    /// <summary>
    /// AES-256-CBC encryption service.
    ///
    /// PASSTHROUGH MODE: If Encryption:Key or Encryption:HmacKey are missing or
    /// empty in configuration the service starts in passthrough mode — Encrypt()
    /// and Decrypt() return the value unchanged and BlindIndex() returns the
    /// raw normalised value. A startup warning is logged so the gap is visible.
    ///
    /// Passthrough mode is safe for local development but must NEVER reach
    /// production. Set Encryption__Key and Encryption__HmacKey as environment
    /// variables (or Azure Key Vault) on any environment that holds real data.
    ///
    /// Storage format when keys ARE configured (Base64):
    ///   [ 16 bytes IV ][ N bytes ciphertext (PKCS7 padded) ]
    /// </summary>
    public sealed class AesEncryptionService : IAesEncryptionService
    {
        private const int IvBytes = 16;
        private const int KeyBytes = 32;

        private readonly byte[]? _key;
        private readonly byte[]? _hmacKey;
        private readonly bool _enabled;

        public AesEncryptionService(
            IOptions<AesEncryptionOptions> opts,
            ILogger<AesEncryptionService> logger)
        {
            var options = opts.Value;

            if (string.IsNullOrWhiteSpace(options.Key) ||
                string.IsNullOrWhiteSpace(options.HmacKey))
            {
                _enabled = false;
                logger.LogWarning(
                    "AesEncryptionService: Encryption:Key or Encryption:HmacKey is missing " +
                    "from configuration. Running in PASSTHROUGH mode — PII is stored as " +
                    "plain text. Add the Encryption section to appsettings.json before " +
                    "storing real data. See README for key generation instructions.");
                return;
            }

            try
            {
                var keyBytes = Convert.FromBase64String(options.Key);
                var hmacBytes = Convert.FromBase64String(options.HmacKey);

                if (keyBytes.Length != KeyBytes || hmacBytes.Length != KeyBytes)
                {
                    _enabled = false;
                    logger.LogWarning(
                        "AesEncryptionService: Keys are not 32 bytes each after Base64 " +
                        "decoding. Running in PASSTHROUGH mode. Regenerate keys with: " +
                        "Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))");
                    return;
                }

                _key = keyBytes;
                _hmacKey = hmacBytes;
                _enabled = true;
                logger.LogInformation("AesEncryptionService: AES-256-CBC encryption is active.");
            }
            catch (FormatException)
            {
                _enabled = false;
                logger.LogWarning(
                    "AesEncryptionService: Keys are not valid Base64. Running in PASSTHROUGH mode.");
            }
        }

        // ── Encrypt ───────────────────────────────────────────────────────
        public string? Encrypt(string? plaintext)
        {
            if (plaintext is null) return null;
            if (!_enabled) return plaintext; // passthrough

            using var aes = CreateAes();
            aes.GenerateIV();
            var iv = aes.IV;

            using var encryptor = aes.CreateEncryptor(aes.Key, iv);
            var plain = Encoding.UTF8.GetBytes(plaintext);
            var cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);

            var payload = new byte[IvBytes + cipher.Length];
            Buffer.BlockCopy(iv, 0, payload, 0, IvBytes);
            Buffer.BlockCopy(cipher, 0, payload, IvBytes, cipher.Length);

            return Convert.ToBase64String(payload);
        }

        // ── Decrypt ───────────────────────────────────────────────────────
        public string? Decrypt(string? ciphertext)
        {
            if (string.IsNullOrEmpty(ciphertext)) return null;
            if (!_enabled) return ciphertext; // passthrough

            byte[] payload;
            try { payload = Convert.FromBase64String(ciphertext); }
            catch { return ciphertext; } // not encrypted — return as-is (migration safety)

            if (payload.Length <= IvBytes) return ciphertext;

            var iv = new byte[IvBytes];
            var cipher = new byte[payload.Length - IvBytes];
            Buffer.BlockCopy(payload, 0, iv, 0, IvBytes);
            Buffer.BlockCopy(payload, IvBytes, cipher, 0, cipher.Length);

            using var aes = CreateAes();
            using var dec = aes.CreateDecryptor(aes.Key, iv);

            try
            {
                var plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
                return Encoding.UTF8.GetString(plain);
            }
            catch (CryptographicException)
            {
                return null; // wrong key or corrupted — surface as null
            }
        }

        // ── Blind index ───────────────────────────────────────────────────
        public string? BlindIndex(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var normalised = value.Trim().ToLowerInvariant();

            if (!_enabled)
            {
                // Passthrough: use a simple SHA256 so the column still has
                // a consistent non-empty value and the unique index still works.
                var sha = SHA256.HashData(Encoding.UTF8.GetBytes(normalised));
                return Convert.ToHexString(sha).ToLowerInvariant();
            }

            var bytes = Encoding.UTF8.GetBytes(normalised);
            using var hmac = new HMACSHA256(_hmacKey!);
            return Convert.ToHexString(hmac.ComputeHash(bytes)).ToLowerInvariant();
        }

        // ── Helper ────────────────────────────────────────────────────────
        private Aes CreateAes()
        {
            var aes = Aes.Create();
            aes.Key = _key!;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }
    }
}