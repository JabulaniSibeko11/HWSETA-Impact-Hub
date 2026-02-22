using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Programme
{
    public sealed class ProgrammeCreateVm
    {

        public string? ProgrammeCode { get; set; }

        [Required]
        public string ProgrammeName { get; set; } = "";

        [Required]
        public Guid QualificationTypeId { get; set; }

        public List<SelectListItem> QualificationTypes { get; set; } = new();

        public bool IsActive { get; set; } = true;



        [MaxLength(20)]
        public string? NqfLevel { get; set; }
        public int? Credits { get; set; }
        public string? SAQAId { get; set; }
        public string? OFOCode { get; set; }


        [Range(1, 60)]
        public int? DurationMonths { get; set; }

        [Required] public Guid ProvinceId { get; set; }
        public List<SelectListItem> Provinces { get; set; } = new();




    }
}
