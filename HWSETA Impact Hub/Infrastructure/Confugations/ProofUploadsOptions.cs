namespace HWSETA_Impact_Hub.Infrastructure.Confugations
{
    public sealed class ProofUploadsOptions
    {
        public string CompletionLettersRootPath { get; set; } = "";
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB default
        public string[] AllowedExtensions { get; set; } = new[] { ".pdf" };
    }
}
