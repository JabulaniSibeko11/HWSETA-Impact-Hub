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
       

        
        public string? QualificationType { get; set; }   // lookup name (or Guid QualificationTypeId)
     

       

    

        [MaxLength(20)]
        public string? NqfLevel { get; set; }


        [Range(1, 60)]
        public int? DurationMonths { get; set; }

     

  
    }
}
