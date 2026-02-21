using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{

    public enum EnrollmentStatus { Enrolled = 1, InTraining = 2, DroppedOut = 3, Completed = 4 }

    public sealed class Programme : BaseEntity
    {

        public Guid BeneficiaryId { get; set; }
        public Beneficiary Beneficiary { get; set; } = null!;

        public Guid CohortId { get; set; }
        public Cohort Cohort { get; set; } = null!;

        public EnrollmentStatus CurrentStatus { get; set; } = EnrollmentStatus.Enrolled;

        public DateTime StartDate { get; set; }
        public DateTime? ActualEndDate { get; set; }

        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;

        public string ProgrammeName { get; set; } = "";
        public string? ProgrammeCode { get; set; } // unique optional

        public Guid QualificationTypeId { get; set; }
        public QualificationType QualificationType { get; set; } = null!;

        public string NqfLevel { get; set; } = ""; // "NQF 4" (portable)
        public string? SAQAId { get; set; }
        public string? OFOCode { get; set; }
        public int? Credits { get; set; }
        public int DurationMonths { get; set; }

        
       
       
        public int CohortYear { get; set; }

        
        public DateTime EndDate { get; set; }

        public string Province { get; set; } = "";
        public int TargetBeneficiaries { get; set; }
       
       
       
    }
}
