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
        private readonly ApplicationDbContext _db;
        private readonly IEnrollmentService _svc;
        private readonly IEnrollmentDocumentService _sdc;
        public EnrollmentsController(ApplicationDbContext db, IEnrollmentService svc, IEnrollmentDocumentService sdc)
        {
            _db = db;
            _svc = svc;
            _sdc = sdc;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var list = await _svc.ListAsync(ct);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var vm = new EnrollmentCreateVm
            {
                StartDate = DateTime.Today
            };

            await LoadCreateDropdowns(vm, ct);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EnrollmentCreateVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadCreateDropdowns(vm, ct);
                return View(vm);
            }

            var (ok, error, enrollmentId) = await _svc.CreateAsync(vm, ct);

            if (!ok)
            {
                ModelState.AddModelError("", error ?? "Unable to create enrollment.");
                await LoadCreateDropdowns(vm, ct);
                return View(vm);
            }

            TempData["Success"] = "Enrollment created successfully.";
            return RedirectToAction(nameof(Details), new { id = enrollmentId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var enr = await _svc.GetAsync(id, ct);
            if (enr == null) return NotFound();

            var history = await _svc.GetHistoryAsync(id, ct);
            ViewBag.History = history;

            var statusVm = new EnrollmentStatusUpdateVm { EnrollmentId = id };
            ViewBag.StatusVm = statusVm;

            return View(enr);
        }

        // ✅ AJAX: filter cohorts by Programme/Provider/Employer
        [HttpGet]
        public async Task<IActionResult> GetCohorts(Guid? programmeId, Guid? providerId, Guid? employerId, CancellationToken ct)
        {
            var q = _db.Cohorts.AsNoTracking().Where(x => x.IsActive);

            if (programmeId.HasValue) q = q.Where(x => x.ProgrammeId == programmeId.Value);
            if (providerId.HasValue) q = q.Where(x => x.ProviderId == providerId.Value);
            if (employerId.HasValue) q = q.Where(x => x.EmployerId == employerId.Value);

            var list = await q
                .OrderByDescending(x => x.StartDate)
                .Select(x => new
                {
                    id = x.Id,
                    text = x.CohortCode + " | " + x.IntakeYear +
                           " | " + x.StartDate.ToString("yyyy-MM-dd") +
                           " → " + x.PlannedEndDate.ToString("yyyy-MM-dd")
                })
                .ToListAsync(ct);

            return Json(list);
        }

        private async Task LoadCreateDropdowns(EnrollmentCreateVm vm, CancellationToken ct)
        {
            vm.Programmes = await _db.Programmes.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.ProgrammeName)
                .Select(x => new SelectListItem { Text = x.ProgrammeName, Value = x.Id.ToString() })
                .ToListAsync(ct);

            vm.Providers = await _db.Providers.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.ProviderName)
                .Select(x => new SelectListItem { Text = x.ProviderName, Value = x.Id.ToString() })
                .ToListAsync(ct);

            vm.Employers = await _db.Employers.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.EmployerCode)
                .Select(x => new SelectListItem
                {
                    Text = $"{x.EmployerCode} - {x.RegistrationNumber}",
                    Value = x.Id.ToString()
                })
                .ToListAsync(ct);

            vm.Beneficiaries = await _db.Beneficiaries.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
                .Select(x => new SelectListItem
                {
                    Text = $"{x.LastName}, {x.FirstName} ({x.IdentifierValue})",
                    Value = x.Id.ToString()
                })
                .ToListAsync(ct);

            // Cohorts list stays empty until user filters
            vm.Cohorts = new List<SelectListItem>();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(EnrollmentStatusUpdateVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid status update.";
                return RedirectToAction(nameof(Details), new { id = vm.EnrollmentId });
            }

            var (ok, error) = await _svc.UpdateStatusAsync(vm, ct);

            if (!ok)
            {
                TempData["Error"] = error ?? "Unable to update status.";
                return RedirectToAction(nameof(Details), new { id = vm.EnrollmentId });
            }

            TempData["Success"] = "Status updated.";
            return RedirectToAction(nameof(Details), new { id = vm.EnrollmentId });
        }



        [HttpGet]
        public async Task<IActionResult> EnrolDocumentIndex(Guid enrollmentId, CancellationToken ct)
        {
            ViewBag.EnrollmentId = enrollmentId;

            var list = await _sdc.ListAsync(enrollmentId, ct);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> UploadDocument(Guid enrollmentId, CancellationToken ct)
        {
            var vm = new EnrollmentDocumentUploadVm
            {
                EnrollmentId = enrollmentId,
                DocumentTypes = await _db.DocumentTypes.AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                    .ToListAsync(ct)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(EnrollmentDocumentUploadVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                vm.DocumentTypes = await _db.DocumentTypes.AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                    .ToListAsync(ct);

                return View(vm);
            }

            var (ok, error) = await _sdc.UploadAsync(vm, ct);
            if (!ok)
            {
                ModelState.AddModelError("", error ?? "Upload failed.");

                vm.DocumentTypes = await _db.DocumentTypes.AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                    .ToListAsync(ct);

                return View(vm);
            }

            TempData["Success"] = "Document uploaded successfully.";
            return RedirectToAction(nameof(EnrolDocumentIndex), new { enrollmentId = vm.EnrollmentId });
        }

        [HttpGet]
        public async Task<IActionResult> Download(Guid id, CancellationToken ct)
        {
            var (ok, error, filePath, downloadName) = await _sdc.GetFileAsync(id, ct);
            if (!ok) return NotFound(error);

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath, ct);
            return File(bytes, "application/octet-stream", downloadName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, Guid enrollmentId, CancellationToken ct)
        {
            var (ok, error) = await _sdc.DeleteAsync(id, ct);
            if (!ok) TempData["Error"] = error ?? "Delete failed.";
            else TempData["Success"] = "Document deleted.";

            return RedirectToAction(nameof(Index), new { enrollmentId });
        }
    }
}
