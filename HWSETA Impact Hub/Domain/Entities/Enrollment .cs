using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public sealed class Enrollment : BaseEntity
    {
        public Guid BeneficiaryId { get; set; }
        public Beneficiary Beneficiary { get; set; } = null!;

        public Guid ProgrammeId { get; set; }
        public Programme Programme { get; set; } = null!;

        public Guid ProviderId { get; set; }
        public Provider Provider { get; set; } = null!;

        public Guid? EmployerId { get; set; }
        public Employer? Employer { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime? EndDate { get; set; }

        public EnrollmentStatus CurrentStatus { get; set; } = EnrollmentStatus.Enrolled;

        public string? Notes { get; set; } // short summary
        public bool IsActive { get; set; } = true;
    }
}
