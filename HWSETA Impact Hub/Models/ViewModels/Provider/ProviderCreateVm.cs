using Microsoft.AspNetCore.Mvc.Rendering;
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


        [MaxLength(120)]
        public string ContactName { get; set; }

        [EmailAddress, MaxLength(256)]
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        public DateTime? AccreditationStartDate { get; set; }
        public DateTime? AccreditationEndDate { get; set; }

        public bool IsActive { get; set; } = true;

        //Address fields (creates Address entity)
        [Required, MaxLength(80)] public string City { get; set; } = "";
        [Required, MaxLength(200)] public string AddressLine1 { get; set; } = "";
        [Required, MaxLength(12)] public string PostalCode { get; set; } = "";

        [Required] public Guid ProvinceId { get; set; }
        public List<SelectListItem> Provinces { get; set; } = new();
    }
}
