namespace HWSETA_Impact_Hub.Models.ViewModels.Beneficiaries
{
    public sealed class BeneficiaryListVm
    {
        public Guid Id { get; set; }

        public string IdentifierType { get; set; } = "";
        public string IdentifierValue { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string MobileNumber { get; set; } = "";

        public string Province { get; set; } = "";
        public string City { get; set; } = "";

        public string RegistrationStatus { get; set; } = "";
        public bool IsActive { get; set; }
    }
}
