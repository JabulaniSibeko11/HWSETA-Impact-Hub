using ClosedXML.Excel;
using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Beneficiaries;
using HWSETA_Impact_Hub.Models.ViewModels.Programme;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Controllers
{
    [Authorize(Policy = "AdminManage")]
    public sealed class ProgrammesController : Controller
    {
        private readonly IProgrammeService _svc;
        private readonly IAuditService _audit;
        private readonly ApplicationDbContext _db;

        public ProgrammesController(IProgrammeService svc, IAuditService audit, ApplicationDbContext db)
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
        //public IActionResult Create() => View(new ProgrammeCreateVm());

        public async Task<IActionResult> CreateAsync()
        {
            var vm = new ProgrammeCreateVm();

            await LoadDropdowns(vm);

            return View(vm);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProgrammeCreateVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(vm);

            var (ok, error) = await _svc.CreateAsync(vm, ct);

            await _audit.LogViewAsync(
                "Programme",
                vm.ProgrammeCode ?? vm.ProgrammeName,
                ok ? "Single create success" : $"Single create failed: {error}",
                ct);

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed.");
                return View(vm);
            }

            TempData["Success"] = "Programme added successfully.";
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
                "ProgrammeImport",
                file?.FileName ?? "no-file",
                $"Bulk import: Total={res.TotalRows}, Inserted={res.Inserted}, Updated={res.Updated}, Skipped={res.Skipped}, Errors={res.Errors.Count}",
                ct);

            return View("UploadResult", res);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadTemplate(CancellationToken ct)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Programmes");

            var headers = new[]
            {
                "ProgrammeCode",
                "ProgrammeName",
                "NqfLevel",
                "QualificationType",
                "DurationMonths",
                "IsActive"
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            ws.Row(1).Style.Font.Bold = true;
            ws.SheetView.FreezeRows(1);

            ws.Cell(2, 1).Value = "PRG-0001";
            ws.Cell(2, 2).Value = "Example Learnership Programme";
            ws.Cell(2, 3).Value = "NQF 4";
            ws.Cell(2, 4).Value = "Learnership";
            ws.Cell(2, 5).Value = 12;
            ws.Cell(2, 6).Value = true;

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();

            await _audit.LogViewAsync(
                "ProgrammeTemplate",
                "ProgrammesTemplate.xlsx",
                "Downloaded programmes Excel template",
                ct);

            var fileName = $"Programmes_Template_{DateTime.Today:yyyyMMdd}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }



        private async Task LoadDropdowns(ProgrammeCreateVm vm)
        {
            vm.QualificationTypes = await _db.QualificationTypes.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync();

            vm.Provinces = await _db.Provinces.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync();
        }
    }
}