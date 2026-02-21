using HWSETA_Impact_Hub.Models.ViewModels.Reporting;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HWSETA_Impact_Hub.Controllers
{
    [Authorize(Policy = "ExecRead")]
    public sealed class ReportsController : Controller
    {
        private readonly IReportingService _svc;

        public ReportsController(IReportingService svc)
        {
            _svc = svc;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] CohortDeliveryReportFiltersVm filters, CancellationToken ct)
        {
            var vm = await _svc.BuildCohortDeliveryAsync(filters, ct);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv([FromQuery] CohortDeliveryReportFiltersVm filters, CancellationToken ct)
        {
            var csv = await _svc.ExportCohortDeliveryCsvAsync(filters, ct);
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"cohort_delivery_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel([FromQuery] CohortDeliveryReportFiltersVm filters, CancellationToken ct)
        {
            var bytes = await _svc.ExportCohortDeliveryExcelAsync(filters, ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"cohort_delivery_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
        }
    }
}
