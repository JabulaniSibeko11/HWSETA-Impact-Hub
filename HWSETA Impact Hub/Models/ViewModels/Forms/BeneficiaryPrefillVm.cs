namespace HWSETA_Impact_Hub.Models.ViewModels.Forms
{
    public sealed class BeneficiaryPrefillVm
    {
        public Guid BeneficiaryId { get; set; }

        public string IdentifierType { get; set; } = "";
        public string IdentifierValue { get; set; } = "";

        public string FirstName { get; set; } = "";
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = "";
        public DateTime DateOfBirth { get; set; }

        public string? Email { get; set; }
        public string MobileNumber { get; set; } = "";

        public string? Province { get; set; }
        public string? City { get; set; }
        public string? AddressLine1 { get; set; }
        public string? PostalCode { get; set; }

        public string Gender { get; set; } = "";
        public string Race { get; set; } = "";
        public string CitizenshipStatus { get; set; } = "";
        public string DisabilityStatus { get; set; } = "";
        public string? DisabilityType { get; set; }
        public string EducationLevel { get; set; } = "";
        public string EmploymentStatus { get; set; } = "";
    }
}
