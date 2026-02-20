using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Provider
{
    public sealed class ProviderCreateVm
    {
        [Required, MaxLength(200)]
        public string ProviderName { get; set; } = "";

        [MaxLength(50)]
        public string? ProviderCode { get; set; }

        [Required, MaxLength(80)]
        public string AccreditationNo { get; set; } = "";

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
