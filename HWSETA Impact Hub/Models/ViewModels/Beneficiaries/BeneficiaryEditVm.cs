using HWSETA_Impact_Hub.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Beneficiaries
{
    public sealed class BeneficiaryEditVm
    {
        public Guid Id { get; set; }

        [Required]
        public IdentifierType IdentifierType { get; set; }

        [Required]
        [Display(Name = "Identifier Value")]
        public string IdentifierValue { get; set; } = "";

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";

        [Required]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [Display(Name = "Gender")]
        public Guid GenderId { get; set; }

        [Required]
        [Display(Name = "Race")]
        public Guid RaceId { get; set; }

        [Required]
        [Display(Name = "Citizenship Status")]
        public Guid CitizenshipStatusId { get; set; }

        [Required]
        [Display(Name = "Disability Status")]
        public Guid DisabilityStatusId { get; set; }

        [Display(Name = "Disability Type")]
        public Guid? DisabilityTypeId { get; set; }

        [Required]
        [Display(Name = "Education Level")]
        public Guid EducationLevelId { get; set; }

        [Required]
        [Display(Name = "Employment Status")]
        public Guid EmploymentStatusId { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Display(Name = "Mobile Number")]
        public string? MobileNumber { get; set; }

        [Display(Name = "Alternative Number")]
        public string? AltNumber { get; set; }

        public string? Phone { get; set; }

        [Required]
        [Display(Name = "Province")]
        public Guid ProvinceId { get; set; }

        [Required]
        [Display(Name = "City")]
        public string City { get; set; } = "";

        [Required]
        [Display(Name = "Address Line 1")]
        public string AddressLine1 { get; set; } = "";

        [Required]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public string? Programme { get; set; }
        public string? TrainingProvider { get; set; }
        public string? Employer { get; set; }

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
