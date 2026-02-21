using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public sealed class Provider : BaseEntity
    {
        public string? ProviderCode { get; set; }         // optional unique
        public string ProviderName { get; set; } = "";

        // FK to the shared Lookups table (TPH root = LookupBase)
        public Guid ProviderTypeId { get; set; }
        public LookupBase ProviderType { get; set; } = null!;

        public string AccreditationNo { get; set; } = "";   // unique
        public DateTime AccreditationStartDate { get; set; }
        public DateTime AccreditationEndDate { get; set; }

        // Address already contains Province via Address.Province (ProvinceId FK)
        // Do NOT duplicate province as a raw string here
        public Guid AddressId { get; set; }
        public Address Address { get; set; } = null!;

        public string ContactName { get; set; } = "";
        public string ContactEmail { get; set; } = "";
        public string ContactPhone { get; set; } = "";

        public string? Phone { get; set; }         // optional alt number

        public bool IsActive { get; set; } = true;
      

       


       

      
        public string Province { get; set; } = "";

    
      
    }
}
