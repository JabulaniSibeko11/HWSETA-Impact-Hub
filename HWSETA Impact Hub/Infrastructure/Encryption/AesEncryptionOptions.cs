namespace HWSETA_Impact_Hub.Infrastructure.Encryption
{
    /// <summary>
    /// Bound from appsettings.json "Encryption" section.
    /// In production these values must come from environment variables or Azure Key Vault —
    /// never commit real keys to source control.
    /// </summary>
    public sealed class AesEncryptionOptions
    {
        /// <summary>
        /// Base-64 encoded 32-byte (256-bit) AES key.
        /// Generate with: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        /// </summary>
        public string Key { get; set; } = "";

        /// <summary>
        /// Base-64 encoded 32-byte HMAC-SHA256 key used for blind-index hashes on
        /// searchable fields (IdentifierValue, Email).  Must be different from Key.
        /// Generate with: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        /// </summary>
        public string HmacKey { get; set; } = "";
    }
}