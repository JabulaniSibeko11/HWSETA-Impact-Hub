using ClosedXML.Excel;
using DocumentFormat.OpenXml.Presentation;
using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Models.ViewModels.Employers;
using HWSETA_Impact_Hub.Models.ViewModels.Provider;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Controllers
{

    [Authorize(Policy = "AdminManage")]
    public sealed class EmployersController : Controller
    {
        private readonly IEmployerService _svc;
        private readonly IAuditService _audit;
        private readonly ApplicationDbContext _db;

        public EmployersController(IEmployerService svc, IAuditService audit, ApplicationDbContext db)
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
            var vm = new EmployerCreateVm();

            await LoadDropdowns(vm);

            return View(vm);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployerCreateVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(vm);

            var (ok, error) = await _svc.CreateAsync(vm, ct);

            await _audit.LogViewAsync(
                "Employer",
                vm.EmployerCode ?? vm.EmployerName,
                ok ? "Single create success" : $"Single create failed: {error}",
                ct);

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed.");
                return View(vm);
            }

            TempData["Success"] = "Employer added successfully.";
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
                "EmployerImport",
                file?.FileName ?? "no-file",
                $"Bulk import: Total={res.TotalRows}, Inserted={res.Inserted}, Updated={res.Updated}, Skipped={res.Skipped}, Errors={res.Errors.Count}",
                ct);

            return View("UploadResult", res);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadTemplate(CancellationToken ct)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Employers");

            // Headers (Row 1) - must match your importer header names
            var headers = new[]
            {
        "EmployerCode",
        "EmployerName",
        "Sector",
        "Province",
        "ContactName",
        "ContactEmail",
        "Phone"
    };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            // Make headers bold + freeze top row
            ws.Row(1).Style.Font.Bold = true;
            ws.SheetView.FreezeRows(1);

            // Add example row (optional but VERY helpful)
            ws.Cell(2, 1).Value = "EMP-0001";
            ws.Cell(2, 2).Value = "Example Employer Pty Ltd";
            ws.Cell(2, 3).Value = "Health & Welfare";
            ws.Cell(2, 4).Value = "Gauteng";
            ws.Cell(2, 5).Value = "Jane Doe";
            ws.Cell(2, 6).Value = "jane.doe@example.com";
            ws.Cell(2, 7).Value = "010 123 4567";

            // Make columns readable
            ws.Columns().AdjustToContents();

            // Save to memory
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();

            // ✅ Audit
            await _audit.LogViewAsync(
                "EmployerTemplate",
                "EmployersTemplate.xlsx",
                "Downloaded employers Excel template",
                ct);

            var fileName = $"Employers_Template_{DateTime.Today:yyyyMMdd}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private async Task LoadDropdowns(EmployerCreateVm vm)
        {
            vm.Provinces = await _db.Provinces.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync();

            vm.RegistrationTypes = await _db.EmployerRegistrationTypes.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync();
        }
    }

}