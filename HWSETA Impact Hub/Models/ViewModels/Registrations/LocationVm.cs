using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Registrations
{
    public sealed class LocationVm
    {
        [Required]
        public string Token { get; set; } = "";

        [Required]
        public string Latitude { get; set; }

        [Required]
        public string Longitude { get; set; }
    }
}
