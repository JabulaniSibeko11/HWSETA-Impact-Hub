namespace HWSETA_Impact_Hub.Models.ViewModels.Enrollment
{
    public sealed class EnrollmentDocumentRowVm
    {
        public Guid Id { get; set; }
        public Guid EnrollmentId { get; set; }

        public string DocumentType { get; set; } = "";
        public string FileName { get; set; } = "";
        public DateTime UploadedOnUtc { get; set; }
        public string UploadedByUserId { get; set; } = "";
    }
}
