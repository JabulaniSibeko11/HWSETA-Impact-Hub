using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public sealed class Address : BaseEntity
    {
        public string AddressLine1 { get; set; } = "";
        public string? Suburb { get; set; }
        public string City { get; set; } = "";
        public Guid ProvinceId { get; set; }
        public Province Province { get; set; } = null!;
        public string PostalCode { get; set; } = "";
    }
}
