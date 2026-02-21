using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public sealed class Enrollment : BaseEntity
    {
        public Guid BeneficiaryId { get; set; }
        public Beneficiary Beneficiary { get; set; } = null!;

        public Guid CohortId { get; set; }
        public Cohort Cohort { get; set; } = null!;

        public EnrollmentStatus CurrentStatus { get; set; } = EnrollmentStatus.Enrolled;

        // When beneficiary starts within cohort window (default = Cohort.StartDate)
        public DateTime StartDate { get; set; }

        // Actual end date (set when Completed or DroppedOut)
        public DateTime? ActualEndDate { get; set; }

        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        
        public Guid ProgrammeId { get; set; }
        public Programme Programme { get; set; } = null!;

        public Guid ProviderId { get; set; }
        public Provider Provider { get; set; } = null!;

        public Guid? EmployerId { get; set; }
        public Employer? Employer { get; set; }

        
        public DateTime? EndDate { get; set; }

      
    }
}
