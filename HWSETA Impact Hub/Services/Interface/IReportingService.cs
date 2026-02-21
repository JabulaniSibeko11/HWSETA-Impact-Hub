using HWSETA_Impact_Hub.Models.ViewModels.Reporting;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IReportingService
    {
        Task<CohortDeliveryReportVm> BuildCohortDeliveryAsync(CohortDeliveryReportFiltersVm filters, CancellationToken ct);
        Task<byte[]> ExportCohortDeliveryExcelAsync(CohortDeliveryReportFiltersVm filters, CancellationToken ct);
        Task<string> ExportCohortDeliveryCsvAsync(CohortDeliveryReportFiltersVm filters, CancellationToken ct);
    }
}
