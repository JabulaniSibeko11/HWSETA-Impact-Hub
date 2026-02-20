using HWSETA_Impact_Hub.Domain.Common;
using HWSETA_Impact_Hub.Infrastructure.Identity;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public enum IdentifierType
    {
        SaId = 1,
        Passport = 2
    }

    public sealed class Beneficiary : BaseEntity
    {
        // Unique identity for dedup
        public IdentifierType IdentifierType { get; set; } = IdentifierType.SaId;
        public string IdentifierValue { get; set; } = "";

        // Personal info
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; } // keep free text for now ("Male/Female/Other")

        // Contact
        public string? Email { get; set; }
        public string? Phone { get; set; }

        // Address (simple MVP)
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? AddressLine1 { get; set; }
        public string? PostalCode { get; set; }

        // Future link to Identity user account (optional)
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
