using ClosedXML.Excel;
using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Beneficiaries;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Controllers
{
    [Authorize(Policy = "AdminManage")]
    public sealed class BeneficiariesController : Controller
    {
        private readonly IBeneficiaryService _svc;
        private readonly IAuditService _audit;
        private readonly ApplicationDbContext _db;
        private readonly IBeneficiaryInviteService _invites;

        public BeneficiariesController(IBeneficiaryService svc, IAuditService audit, ApplicationDbContext db, IBeneficiaryInviteService invites)
        {
            _svc = svc;
            _audit = audit;
            _db = db;
            _invites = invites;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var list = await _svc.ListAsync(ct);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var vm = new BeneficiaryCreateVm();

            await LoadDropdowns(vm, ct);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BeneficiaryCreateVm vm, CancellationToken ct)
        {
            await LoadDropdowns(vm, ct);

            if (!ModelState.IsValid)
                return View(vm);

            var (ok, error, beneficiaryId) = await _svc.CreateAsync(vm, ct);

            await _audit.LogViewAsync(
                "Beneficiary",
                $"{vm.IdentifierType}:{vm.IdentifierValue}",
                ok ? "Single create success" : $"Single create failed: {error}",
                ct);

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed.");
                return View(vm);
            }

            // Auto-send invite on create (email only)
            if (beneficiaryId.HasValue)
            {
                var (invOk, invErr) = await _invites.SendInviteAsync(
                    beneficiaryId.Value,
                    sendEmail: true,
                    sendSms: false,
                    ct);

                if (!invOk)
                {
                    // beneficiary created, but invite failed
                    TempData["Error"] = "Beneficiary added, but invite email failed: " + (invErr ?? "Unknown error.");
                    return RedirectToAction(nameof(Index));
                }
            }

            TempData["Success"] = "Beneficiary added successfully and invite email sent.";
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
                "BeneficiaryImport",
                file?.FileName ?? "no-file",
                $"Bulk import: Total={res.TotalRows}, Inserted={res.Inserted}, Updated={res.Updated}, Skipped={res.Skipped}, Errors={res.Errors.Count}",
                ct);

            return View("UploadResult", res);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadTemplate(CancellationToken ct)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Beneficiaries");

            var headers = new[]
            {
                "IdentifierType",
                "IdentifierValue",
                "FirstName",
                "LastName",
                "DateOfBirth",
                "Gender",
                "Email",
                "Phone",
                "Province",
                "City",
                "AddressLine1",
                "PostalCode",
                "IsActive"
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            ws.Row(1).Style.Font.Bold = true;
            ws.SheetView.FreezeRows(1);

            // Example row
            ws.Cell(2, 1).Value = "SaId"; // or Passport
            ws.Cell(2, 2).Value = "9001015009087";
            ws.Cell(2, 3).Value = "Thabo";
            ws.Cell(2, 4).Value = "Mokoena";
            ws.Cell(2, 5).Value = "1990-01-01";
            ws.Cell(2, 6).Value = "Male";
            ws.Cell(2, 7).Value = "thabo@example.com";
            ws.Cell(2, 8).Value = "0820000000";
            ws.Cell(2, 9).Value = "Gauteng";
            ws.Cell(2, 10).Value = "Johannesburg";
            ws.Cell(2, 11).Value = "1 Main Road";
            ws.Cell(2, 12).Value = "2001";
            ws.Cell(2, 13).Value = true;

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var bytes = ms.ToArray();

            await _audit.LogViewAsync(
                "BeneficiaryTemplate",
                "BeneficiariesTemplate.xlsx",
                "Downloaded beneficiaries Excel template",
                ct);

            var fileName = $"Beneficiaries_Template_{DateTime.Today:yyyyMMdd}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        private async Task LoadDropdowns(BeneficiaryCreateVm vm, CancellationToken ct)
        {
            vm.Genders = await _db.Genders.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync(ct);

            vm.Races = await _db.Races.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync(ct);

            vm.CitizenshipStatuses = await _db.CitizenshipStatuses.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync(ct);

            vm.DisabilityStatuses = await _db.DisabilityStatuses.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync(ct);

            vm.DisabilityTypes = await _db.DisabilityTypes.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync(ct);

            vm.EducationLevels = await _db.EducationLevels.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync(ct);

            vm.EmploymentStatuses = await _db.EmploymentStatuses.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync(ct);

            vm.Provinces = await _db.Provinces.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync(ct);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInvite(Guid id, bool sendEmail = true, bool sendSms = true, CancellationToken ct = default)
        {
            // Ensure exactly one Registration form exists & is published & active
            var registrationTemplateExists = await _db.FormTemplates
                .AnyAsync(t =>
                    t.IsActive &&
                    t.Status == FormStatus.Published &&
                    t.Purpose == FormPurpose.Registration,
                    ct);

            if (!registrationTemplateExists)
            {
                TempData["Error"] = "No ACTIVE PUBLISHED Registration Form found. Please publish the Registration Form first.";
                return RedirectToAction(nameof(Index));
            }

            var (ok, err) = await _invites.SendInviteAsync(id, sendEmail, sendSms, ct);
            TempData[ok ? "Success" : "Error"] = ok ? "Invite sent successfully." : err;
            return RedirectToAction(nameof(Index));
        }
    }
}