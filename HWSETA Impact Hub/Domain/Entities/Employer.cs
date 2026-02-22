using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public sealed class Employer : BaseEntity
    {

        public string EmployerCode { get; set; } = ""; // unique
        public string EmployerName { get; set; } = "";
        public string? TradingName { get; set; }

        public Guid RegistrationTypeId { get; set; }
        public EmployerRegistrationType RegistrationType { get; set; } = null!;

        public string RegistrationNumber { get; set; } = ""; // CIPC (mandatory)
        public string SetaLevyNumber { get; set; } = "";     // mandatory

        public Guid AddressId { get; set; }
        public Address Address { get; set; } = null!;

        public string ContactName { get; set; } = "";
        public string ContactEmail { get; set; } = "";
        public string ContactPhone { get; set; } = "";

        public bool IsActive { get; set; } = true;
        public string Sector { get; set; } = "";
        public string Province { get; set; } = "";
      
        public string? Phone { get; set; }
    }
}
