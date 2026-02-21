using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Cohort
{
    public sealed class CohortCreateVm
    {
        [Required, StringLength(60)]
        public string CohortCode { get; set; } = "";

        [Required]
        public Guid ProgrammeId { get; set; }

        [Required]
        public Guid ProviderId { get; set; }

        public Guid? EmployerId { get; set; }

        [Required]
        public Guid FundingTypeId { get; set; }

        [Range(2000, 2100)]
        public int IntakeYear { get; set; } = DateTime.Today.Year;

        [Required, DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required, DataType(DataType.Date)]
        public DateTime PlannedEndDate { get; set; } = DateTime.Today.AddMonths(12);

        public bool IsActive { get; set; } = true;

        // Dropdowns
        public List<SelectListItem> Programmes { get; set; } = new();
        public List<SelectListItem> Providers { get; set; } = new();
        public List<SelectListItem> Employers { get; set; } = new();
        public List<SelectListItem> FundingTypes { get; set; } = new();
    }
}
