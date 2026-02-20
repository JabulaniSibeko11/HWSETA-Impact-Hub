using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public sealed class EnrollmentStatusHistory : BaseEntity
    {
        public Guid EnrollmentId { get; set; }
        public Enrollment Enrollment { get; set; } = null!;

        public EnrollmentStatus Status { get; set; }
        public DateTime StatusDate { get; set; } = DateTime.Today;

        public string? Reason { get; set; }  // e.g. "Dropped due to relocation"
        public string? Comment { get; set; } // extra context
    }
}
