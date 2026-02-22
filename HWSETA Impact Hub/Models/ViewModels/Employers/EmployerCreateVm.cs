using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Employers
{
    public sealed class EmployerCreateVm
    {
        [Required, MaxLength(200)]
        public string EmployerName { get; set; } = "";

        [MaxLength(50)]
        public string? EmployerCode { get; set; }


        [Required]
        public string RegistrationNumber { get; set; }
        public string SetaLevyNumber { get; set; } = "";

        [Required, MaxLength(120)]
        public string Sector { get; set; } = "";

        //[Required, MaxLength(60)]
        //public string Province { get; set; } = "";


        [MaxLength(120)]
        public string? ContactName { get; set; }

        [EmailAddress, MaxLength(256)]
        public string? ContactEmail { get; set; }

        public string ContactPhone { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }


        //Address fields (creates Address entity)
        [Required, MaxLength(80)] public string City { get; set; } = "";
        [Required, MaxLength(200)] public string AddressLine1 { get; set; } = "";
        [Required, MaxLength(12)] public string PostalCode { get; set; } = "";


        [Required]  public Guid RegistrationTypeId { get; set; }
        public List<SelectListItem> RegistrationTypes { get; set; } = new();

        [Required] public Guid ProvinceId { get; set; }
        public List<SelectListItem> Provinces { get; set; } = new();


        public bool IsActive { get; set; } = true;
    }
}
