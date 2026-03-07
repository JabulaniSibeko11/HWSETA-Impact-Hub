namespace HWSETA_Impact_Hub.Models.ViewModels.Enrollment
{
    public sealed class EnrollmentListVm
    {
        public Guid Id { get; set; }

        public string BeneficiaryName { get; set; } = "";
        public string BeneficiaryIdentifier { get; set; } = "";

        public string CohortCode { get; set; } = "";
        public string ProgrammeName { get; set; } = "";
        public string QualificationType { get; set; } = "";
        public string ProviderName { get; set; } = "";
        public string EmployerName { get; set; } = "";
        public string FundingType { get; set; } = "";

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string Status { get; set; } = "";
        public bool IsActive { get; set; }
    }
}
