namespace HWSETA_Impact_Hub.Infrastructure.Encryption
{
    public interface IAesEncryptionService
    {
        /// <summary>
        /// Encrypts plaintext with AES-256-CBC.
        /// A fresh random 16-byte IV is generated per call and prepended to the
        /// ciphertext before Base64 encoding, so identical plaintexts produce
        /// different ciphertexts — safe for storage.
        /// Returns null when input is null.
        /// </summary>
        string? Encrypt(string? plaintext);

        /// <summary>
        /// Decrypts a value produced by Encrypt().
        /// Returns null when input is null or empty.
        /// </summary>
        string? Decrypt(string? ciphertext);

        /// <summary>
        /// Produces a deterministic HMAC-SHA256 hex digest of the value (lowercase, 64 chars).
        /// Used as a "blind index" so encrypted fields can still be searched by exact match
        /// without exposing the plaintext.  Store this alongside the encrypted column.
        /// Returns null when input is null or whitespace.
        /// </summary>
        string? BlindIndex(string? value);
    }
}