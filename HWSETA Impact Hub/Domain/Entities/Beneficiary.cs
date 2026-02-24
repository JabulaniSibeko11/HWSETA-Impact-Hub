using HWSETA_Impact_Hub.Domain.Common;
using HWSETA_Impact_Hub.Infrastructure.Identity;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public enum IdentifierType
    {
        SaId = 1,
        Passport = 2
    }
    public enum BeneficiaryRegistrationStatus
    {
        AddedByAdmin = 1,
        InviteSent = 2,
        PasswordSet = 3,
        LocationCaptured = 4,
        RegistrationSubmitted = 5,
        Completed = 6
    }

    public sealed class Beneficiary : BaseEntity
    {

        public IdentifierType IdentifierType { get; set; }
        public string IdentifierValue { get; set; } = "";

        public string FirstName { get; set; } = "";
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = "";
        public DateTime DateOfBirth { get; set; }

        // Mandatory SA reporting lookups
        public Guid GenderId { get; set; }
        public Gender Gender { get; set; } = null!;
        public Guid RaceId { get; set; }
        public Race Race { get; set; } = null!;
        public Guid CitizenshipStatusId { get; set; }
        public CitizenshipStatus CitizenshipStatus { get; set; } = null!;
        public Guid DisabilityStatusId { get; set; }
        public DisabilityStatus DisabilityStatus { get; set; } = null!;
        public Guid? DisabilityTypeId { get; set; }   // required when DisabilityStatus = Yes
        public DisabilityType? DisabilityType { get; set; }

        public Guid EducationLevelId { get; set; }
        public EducationLevel EducationLevel { get; set; } = null!;
        public Guid EmploymentStatusId { get; set; }
        public EmploymentStatus EmploymentStatus { get; set; } = null!;

        // Contact (at least one should be mandatory in validation)
        public string? Email { get; set; }
        public string MobileNumber { get; set; } = "";
        public string? AltNumber { get; set; }

        // Address
        public Guid AddressId { get; set; }
        public Address Address { get; set; } = null!;

        // POPIA
        public bool ConsentGiven { get; set; }
        public DateTime ConsentDate { get; set; }

        // Optional Identity account link
        public string? UserId { get; set; }

        public bool IsActive { get; set; } = true;

   

        
        public string? Phone { get; set; }

        // Address (simple MVP)
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? AddressLine1 { get; set; }
        public string? PostalCode { get; set; }

        // Future link to Identity user account (optional)
       
        public ApplicationUser? User { get; set; }



        public BeneficiaryRegistrationStatus RegistrationStatus { get; set; } = BeneficiaryRegistrationStatus.AddedByAdmin;

        public DateTime? InvitedAt { get; set; }
        public DateTime? PasswordSetAt { get; set; }
        public DateTime? LocationCapturedAt { get; set; }
        public DateTime? RegistrationSubmittedAt { get; set; }

        // GPS
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // If registration indicates “Completed”, we require proof upload:
        public string? ProofOfCompletionPath { get; set; }
        public DateTime? ProofUploadedAt { get; set; }

        public string? Programme { get; set; }          // e.g. "Community Health Worker"
        public string? TrainingProvider { get; set; }   // provider name (MVP string)
        public string? Employer { get; set; }

    }
}
