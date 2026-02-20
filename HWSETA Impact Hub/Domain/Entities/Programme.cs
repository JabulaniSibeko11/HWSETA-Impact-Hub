using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public sealed class Programme : BaseEntity
    {
        public string ProgrammeName { get; set; } = "";
        public string ProgrammeType { get; set; } = "";  // Learnership / Skills Programme
        public int CohortYear { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string Province { get; set; } = "";
        public int TargetBeneficiaries { get; set; }
        public string? Notes { get; set; }
       
        public string? ProgrammeCode { get; set; }   // optional unique
        public string? NqfLevel { get; set; }        // e.g. "NQF 4"
        public string? QualificationType { get; set; } // Learnership / Internship / Skills Programme
        public int? DurationMonths { get; set; }     // e.g. 12
        public bool IsActive { get; set; } = true;
    }
}
