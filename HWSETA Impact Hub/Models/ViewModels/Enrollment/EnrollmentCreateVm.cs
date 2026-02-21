using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Enrollment
{
    public sealed class EnrollmentCreateVm
    {

        // Filters (UI only)
        public Guid? ProgrammeId { get; set; }
        public Guid? ProviderId { get; set; }
        public Guid? EmployerId { get; set; }

        // Required selections
        [Required]
        public Guid BeneficiaryId { get; set; }

        [Required]
        public Guid CohortId { get; set; }

        // Optional: default to cohort start date
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Dropdowns
        public List<SelectListItem> Programmes { get; set; } = new();
        public List<SelectListItem> Providers { get; set; } = new();
        public List<SelectListItem> Employers { get; set; } = new();
        public List<SelectListItem> Beneficiaries { get; set; } = new();

        // Cohorts will be loaded dynamically by filters
        public List<SelectListItem> Cohorts { get; set; } = new();


      

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }


        


      

        
    }
}
