namespace HWSETA_Impact_Hub.Models.ViewModels.Beneficiaries
{
    public sealed class BeneficiaryDetailsVm
    {
        public Guid Id { get; set; }

        public string IdentifierType { get; set; } = "";
        public string IdentifierValue { get; set; } = "";

        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string FullName { get; set; } = "";

        public DateTime? DateOfBirth { get; set; }

        public string Gender { get; set; } = "";
        public string Race { get; set; } = "";
        public string CitizenshipStatus { get; set; } = "";
        public string DisabilityStatus { get; set; } = "";
        public string DisabilityType { get; set; } = "";
        public string EducationLevel { get; set; } = "";
        public string EmploymentStatus { get; set; } = "";

        public string Email { get; set; } = "";
        public string MobileNumber { get; set; } = "";
        public string AltNumber { get; set; } = "";
        public string Phone { get; set; } = "";

        public string Province { get; set; } = "";
        public string City { get; set; } = "";
        public string AddressLine1 { get; set; } = "";
        public string PostalCode { get; set; } = "";

        public bool ConsentGiven { get; set; }
        public DateTime? ConsentDate { get; set; }

        public string RegistrationStatus { get; set; } = "";
        public DateTime? InvitedAt { get; set; }
        public DateTime? PasswordSetAt { get; set; }
        public DateTime? LocationCapturedAt { get; set; }
        public DateTime? RegistrationSubmittedAt { get; set; }

        public string? Latitude { get; set; }
        public string? Longitude { get; set; }

        public string ProofOfCompletionPath { get; set; } = "";
        public DateTime? ProofUploadedAt { get; set; }

        public string Programme { get; set; } = "";
        public string TrainingProvider { get; set; } = "";
        public string Employer { get; set; } = "";

        public bool IsActive { get; set; }

        public DateTime CreatedOnUtc { get; set; }
        public string CreatedByUserId { get; set; } = "";
        public DateTime? UpdatedOnUtc { get; set; }
        public string UpdatedByUserId { get; set; } = "";
    }
}
