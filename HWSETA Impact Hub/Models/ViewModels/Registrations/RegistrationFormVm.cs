using HWSETA_Impact_Hub.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Registrations
{
    public sealed class RegistrationFormVm
    {
        // -------------------------
        // Invite / Page Metadata
        // -------------------------
        [Required]
        public string Token { get; set; } = "";

        public string FormName { get; set; } = "Beneficiary Registration";

        public string Description { get; set; } =
            "Please confirm your personal details, accept consent, and provide your programme information.";

        // (Optional) if you want to show beneficiary id on view (not required)
        public Guid? BeneficiaryId { get; set; }

        // -------------------------
        // Section 1: Beneficiary Details (same as BeneficiaryCreateVm)
        // -------------------------
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

        // Address
        [Required] public Guid ProvinceId { get; set; }
        [Required, MaxLength(80)] public string City { get; set; } = "";
        [Required, MaxLength(200)] public string AddressLine1 { get; set; } = "";
        [Required, MaxLength(12)] public string PostalCode { get; set; } = "";

        // -------------------------
        // Consent (moved from Admin -> captured by Beneficiary)
        // -------------------------
        [Required(ErrorMessage = "You must accept the consent to continue.")]
        [Display(Name = "I consent to the processing of my personal information.")]
        public bool ConsentGiven { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "Consent Date")]
        public DateTime ConsentDate { get; set; } = DateTime.Today;

        // -------------------------
        // Section 2: Enrolment Capture (beneficiary selects only these)
        // -------------------------
        [Required]
        [Display(Name = "Qualification Type")]
        public Guid QualificationTypeId { get; set; }

        [Required]
        [Display(Name = "Programme")]
        public Guid ProgrammeId { get; set; }

        [Required]
        [Display(Name = "Employer")]
        public Guid EmployerId { get; set; }

        // Status (UI: Complete / InComplete / DropOut)
        // We'll map these to your EnrollmentStatus enum in controller/service.
        [Required]
        [Display(Name = "Programme Status")]
        public string ProgressStatus { get; set; } = ""; // expected: "Completed", "Incomplete", "DroppedOut"

        [Required, MaxLength(2000)]
        [Display(Name = "Comment / Experience")]
        public string Comment { get; set; } = "";

        // These are derived in backend (hidden from user)
        public Guid? DerivedCohortId { get; set; }
        public Guid? DerivedProviderId { get; set; }

        // -------------------------
        // System flags
        // -------------------------
        public bool IsActive { get; set; } = true;

        // Locking rules for view (ID/Passport locked per your requirement)
        public bool LockIdentifierFields { get; set; } = true;

        // -------------------------
        // Dropdown lists
        // -------------------------
        public List<SelectListItem> Genders { get; set; } = new();
        public List<SelectListItem> Races { get; set; } = new();
        public List<SelectListItem> CitizenshipStatuses { get; set; } = new();
        public List<SelectListItem> DisabilityStatuses { get; set; } = new();
        public List<SelectListItem> DisabilityTypes { get; set; } = new();
        public List<SelectListItem> EducationLevels { get; set; } = new();
        public List<SelectListItem> EmploymentStatuses { get; set; } = new();
        public List<SelectListItem> Provinces { get; set; } = new();

        public List<SelectListItem> QualificationTypes { get; set; } = new();
        public List<SelectListItem> Programmes { get; set; } = new();
        public List<SelectListItem> Employers { get; set; } = new();

        // Progress statuses list (for dropdown)
        public List<SelectListItem> ProgressStatuses { get; set; } = new()
        {
            new SelectListItem { Value = "Incomplete", Text = "InComplete" },
            new SelectListItem { Value = "DroppedOut", Text = "DropOut" },
            new SelectListItem { Value = "Completed", Text = "Complete" }
        };
    }
}