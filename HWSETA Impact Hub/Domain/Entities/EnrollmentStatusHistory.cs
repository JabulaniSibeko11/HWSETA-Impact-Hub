using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public sealed class EnrollmentStatusHistory : BaseEntity
    {

        public Guid EnrollmentId { get; set; }
        public Enrollment Enrollment { get; set; } = null!;

        public EnrollmentStatus Status { get; set; }
        public DateTime StatusDate { get; set; }

        public string? Reason { get; set; }
        public string? Comment { get; set; }

        public string? ChangedByUserId { get; set; }
      
    }

    public sealed class EnrollmentDocument : BaseEntity
    {
        public Guid EnrollmentId { get; set; }
        public Enrollment Enrollment { get; set; } = null!;

        public Guid DocumentTypeId { get; set; }
        public DocumentType DocumentType { get; set; } = null!;

        public string FileName { get; set; } = "";
        public string StoredPath { get; set; } = "";
        public string? Sha256 { get; set; }

        public string UploadedByUserId { get; set; } = "";
        public DateTime UploadedOnUtc { get; set; } = DateTime.UtcNow;
    }
}
