using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Employers
{
    public sealed class EmployerCreateVm
    {
        [Required, MaxLength(200)]
        public string EmployerName { get; set; } = "";

        [MaxLength(50)]
        public string? EmployerCode { get; set; }

        [Required, MaxLength(120)]
        public string Sector { get; set; } = "";

        [Required, MaxLength(60)]
        public string Province { get; set; } = "";

        [MaxLength(120)]
        public string? ContactName { get; set; }

        [EmailAddress, MaxLength(256)]
        public string? ContactEmail { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }
    }
}
