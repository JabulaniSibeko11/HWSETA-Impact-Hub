using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public sealed class Employer : BaseEntity
    {
        public string EmployerNumber { get; set; } = "";
        public string EmployerName { get; set; } = "";
        public string Sector { get; set; } = "";
        public string Province { get; set; } = "";

        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }
    }
}
