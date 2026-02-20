using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Enrollment;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Controllers
{
    [Authorize(Policy = "AdminManage")]
    public sealed class EnrollmentsController : Controller
    {
        private readonly IEnrollmentService _svc;
        private readonly IAuditService _audit;
        private readonly ApplicationDbContext _db;

        public EnrollmentsController(IEnrollmentService svc, IAuditService audit, ApplicationDbContext db)
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
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await LoadDropdowns(ct);
            return View(new EnrollmentCreateVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EnrollmentCreateVm vm, CancellationToken ct)
        {
            await LoadDropdowns(ct);

            if (!ModelState.IsValid) return View(vm);

            var (ok, error, id) = await _svc.CreateAsync(vm, ct);

            await _audit.LogViewAsync(
                "Enrollment",
                id?.ToString() ?? "new",
                ok ? $"Enrollment created (BeneficiaryId={vm.BeneficiaryId})" : $"Enrollment create failed: {error}",
                ct);

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed.");
                return View(vm);
            }

            TempData["Success"] = "Enrollment created successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var enr = await _svc.GetAsync(id, ct);
            if (enr == null) return NotFound();

            var history = await _svc.GetHistoryAsync(id, ct);
            ViewBag.History = history;

            // For status update form
            ViewBag.StatusOptions = Enum.GetValues(typeof(EnrollmentStatus))
                .Cast<EnrollmentStatus>()
                .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString()))
                .ToList();

            return View(enr);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(EnrollmentStatusUpdateVm vm, CancellationToken ct)
        {
            var (ok, error) = await _svc.UpdateStatusAsync(vm, ct);

            await _audit.LogViewAsync(
                "EnrollmentStatus",
                vm.EnrollmentId.ToString(),
                ok ? $"Status updated to {vm.Status}" : $"Status update failed: {error}",
                ct);

            if (!ok)
                TempData["Error"] = error ?? "Failed to update status.";
            else
                TempData["Success"] = "Status updated.";

            return RedirectToAction(nameof(Details), new { id = vm.EnrollmentId });
        }

        private async Task LoadDropdowns(CancellationToken ct)
        {
            var beneficiaries = await _db.Beneficiaries.AsNoTracking()
                .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
                .Select(x => new SelectListItem(
                    $"{x.LastName}, {x.FirstName} ({x.IdentifierType}-{x.IdentifierValue})",
                    x.Id.ToString()))
                .ToListAsync(ct);

            var programmes = await _db.Programmes.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.ProgrammeName)
                .Select(x => new SelectListItem($"{x.ProgrammeName} ({x.ProgrammeCode})", x.Id.ToString()))
                .ToListAsync(ct);

            var providers = await _db.Providers.AsNoTracking()
                .OrderBy(x => x.ProviderName)
                .Select(x => new SelectListItem($"{x.ProviderName} ({x.AccreditationNo})", x.Id.ToString()))
                .ToListAsync(ct);

            var employers = await _db.Employers.AsNoTracking()
                .OrderBy(x => x.EmployerName)
                .Select(x => new SelectListItem($"{x.EmployerName} ({x.EmployerCode})", x.Id.ToString()))
                .ToListAsync(ct);

            ViewBag.Beneficiaries = beneficiaries;
            ViewBag.Programmes = programmes;
            ViewBag.Providers = providers;

            // Employer is optional: include blank option
            employers.Insert(0, new SelectListItem("(None)", ""));
            ViewBag.Employers = employers;
        }
    }
}
