using HWSETA_Impact_Hub.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Beneficiaries
{
    public sealed class BeneficiaryCreateVm
    {
        [Required]
        public IdentifierType IdentifierType { get; set; } = IdentifierType.SaId;

        [Required, MaxLength(80)]
        [Display(Name = "ID / Passport Number")]
        public string IdentifierValue { get; set; } = "";

        [Required, MaxLength(120)]
        public string FirstName { get; set; } = "";

        [Required, MaxLength(120)]
        public string LastName { get; set; } = "";

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        [EmailAddress, MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(60)]
        public string? Province { get; set; }

        [MaxLength(80)]
        public string? City { get; set; }

        [MaxLength(200)]
        public string? AddressLine1 { get; set; }

        [MaxLength(12)]
        public string? PostalCode { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
