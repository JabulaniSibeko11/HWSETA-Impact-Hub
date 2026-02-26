using HWSETA_Impact_Hub.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        [MaxLength(120)]
        public string? MiddleName { get; set; }

        [Required, MaxLength(120)]
        [Display(Name = "Surname")]
        public string LastName { get; set; } = "";

        [Required, DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        // Lookups (GUIDs)
        [Required] public Guid GenderId { get; set; }
        [Required] public Guid RaceId { get; set; }
        [Required] public Guid CitizenshipStatusId { get; set; }
        [Required] public Guid DisabilityStatusId { get; set; }
        public Guid? DisabilityTypeId { get; set; }
        [Required] public Guid EducationLevelId { get; set; }
        [Required] public Guid EmploymentStatusId { get; set; }

        // Contact
        [EmailAddress, MaxLength(256)]
        public string? Email { get; set; }

        [Required, MaxLength(30)]
        public string MobileNumber { get; set; } = "";

        [MaxLength(30)]
        public string? AltNumber { get; set; }

        // Address (creates Address entity)
        [Required] public Guid ProvinceId { get; set; }
        [Required, MaxLength(80)] public string City { get; set; } = "";
        [Required, MaxLength(200)] public string AddressLine1 { get; set; } = "";
        [Required, MaxLength(12)] public string PostalCode { get; set; } = "";

        // POPIA
        //[Required] public bool ConsentGiven { get; set; }
        //[Required, DataType(DataType.Date)] public DateTime ConsentDate { get; set; } = DateTime.Today;

        public bool IsActive { get; set; } = true;

        // Dropdown lists
        public List<SelectListItem> Genders { get; set; } = new();
        public List<SelectListItem> Races { get; set; } = new();
        public List<SelectListItem> CitizenshipStatuses { get; set; } = new();
        public List<SelectListItem> DisabilityStatuses { get; set; } = new();
        public List<SelectListItem> DisabilityTypes { get; set; } = new();
        public List<SelectListItem> EducationLevels { get; set; } = new();
        public List<SelectListItem> EmploymentStatuses { get; set; } = new();
        public List<SelectListItem> Provinces { get; set; } = new();
    }
}
