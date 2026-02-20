using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public sealed class Provider : BaseEntity
    {
        public string ProviderName { get; set; } = "";
        public string AccreditationNo { get; set; } = "";
        public string Province { get; set; } = "";

        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }
    }
}
