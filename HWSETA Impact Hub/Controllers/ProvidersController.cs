using ClosedXML.Excel;
using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Models.ViewModels.Programme;
using HWSETA_Impact_Hub.Models.ViewModels.Provider;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Controllers
{
    [Authorize(Policy = "AdminManage")]
    public sealed class ProvidersController : Controller
    {
        private readonly IProviderService _svc;
        private readonly IAuditService _audit;
        private readonly ApplicationDbContext _db;
        public ProvidersController(IProviderService svc, IAuditService audit, ApplicationDbContext db)
        {
            _svc = svc;
            _audit = audit;
            _db = db;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var list = await _svc.ListAsync(ct);
            return View(list);
        }

        [HttpGet]

        public async Task<IActionResult> CreateAsync()
        {
            var vm = new ProviderCreateVm();

            await LoadDropdowns(vm);

            return View(vm);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProviderCreateVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(vm);

            var (ok, error) = await _svc.CreateAsync(vm, ct);

            await _audit.LogViewAsync(
                "Provider",
                vm.AccreditationNo,
                ok ? "Single create success" : $"Single create failed: {error}",
                ct);

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed.");
                return View(vm);
            }

            TempData["Success"] = "Provider added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Upload() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
        {
            var res = await _svc.ImportFromExcelAsync(file, ct);

            await _audit.LogViewAsync(
                "ProviderImport",
                file?.FileName ?? "no-file",
                $"Bulk import: Total={res.TotalRows}, Inserted={res.Inserted}, Updated={res.Updated}, Skipped={res.Skipped}, Errors={res.Errors.Count}",
                ct);

            return View("UploadResult", res);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadTemplate(CancellationToken ct)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Providers");

            var headers = new[]
            {
                "ProviderCode",
                "ProviderName",
                "AccreditationNo",
                "AccreditationStartDate",
                "AccreditationEndDate",
                
                "ContactName",
                "ContactEmail",
                "ContactPhone",

                "AddressLine1",
                "Suburb",
                "City",
                "PostalCode",
                "Province",
                "IsActive"
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            ws.Row(1).Style.Font.Bold = true;
            ws.SheetView.FreezeRows(1);

            // Example row
            ws.Cell(2, 1).Value = "PROV-0001";
            ws.Cell(2, 2).Value = "Example Training Provider";
            ws.Cell(2, 3).Value = "ACC-12345";
            ws.Cell(2, 4).Value = "2024-12-01";
            ws.Cell(2, 5).Value = "2027-12-31";
            ws.Cell(2, 6).Value = "John Doe";
            ws.Cell(2, 7).Value = "john.doe@example.com";
            ws.Cell(2, 8).Value = "010 000 0000";
            ws.Cell(2, 9).Value = "123 Main Street";
            ws.Cell(2, 10).Value = "Sandton";
            ws.Cell(2, 11).Value = "Johannesburg";
            ws.Cell(2, 12).Value = "2000";
            ws.Cell(2, 13).Value = "Gauteng";
            ws.Cell(2, 14).Value = true;

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();

            await _audit.LogViewAsync(
                "ProviderTemplate",
                "ProvidersTemplate.xlsx",
                "Downloaded providers Excel template",
                ct);

            var fileName = $"Providers_Template_{DateTime.Today:yyyyMMdd}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }



        private async Task LoadDropdowns(ProviderCreateVm vm)
        {
            vm.Provinces = await _db.Provinces.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync();
        }
    }
}
