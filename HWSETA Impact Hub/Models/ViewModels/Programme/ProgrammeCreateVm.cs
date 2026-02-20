using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Programme
{
    public sealed class ProgrammeCreateVm
    {
        [Required, MaxLength(200)]
        public string ProgrammeName { get; set; } = "";

        [MaxLength(50)]
        public string? ProgrammeCode { get; set; }

        [MaxLength(20)]
        public string? NqfLevel { get; set; }

        [MaxLength(60)]
        public string? QualificationType { get; set; }

        [Range(1, 60)]
        public int? DurationMonths { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
