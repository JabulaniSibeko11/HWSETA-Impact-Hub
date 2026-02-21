using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public sealed class Cohort : BaseEntity
    {
        public string CohortCode { get; set; } = ""; // unique like "2026-LRN-GP-0001"

        public Guid ProgrammeId { get; set; }
        public Programme Programme { get; set; } = null!;

        public Guid ProviderId { get; set; }
        public Provider Provider { get; set; } = null!;

        public Guid? EmployerId { get; set; }
        public Employer? Employer { get; set; }

        public Guid FundingTypeId { get; set; }
        public FundingType FundingType { get; set; } = null!;

        public int IntakeYear { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime PlannedEndDate { get; set; }

        public Guid? CreatedByUserId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
