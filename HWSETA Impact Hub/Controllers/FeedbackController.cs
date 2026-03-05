using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Models.ViewModels.Feedback;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Controllers
{
    [Authorize(Policy = "AdminManage")]
    public sealed class FeedbackController : Controller
    {
        private readonly IFeedbackService _svc;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public FeedbackController(
            IFeedbackService svc,
            ApplicationDbContext db,
            UserManager<ApplicationUser> users)
        {
            _svc = svc;
            _db = db;
            _users = users;
        }

        // GET /Feedback
        [HttpGet]
        public async Task<IActionResult> Index(
            FeedbackStatus? status = null,
            FeedbackCategory? category = null,
            string? search = null,
            CancellationToken ct = default)
        {
            var vm = await _svc.GetIndexAsync(status, category, search, ct);
            return View(vm);
        }

        // GET /Feedback/Create
        [HttpGet]
        public async Task<IActionResult> Create(Guid? beneficiaryId = null, CancellationToken ct = default)
        {
            var vm = new FeedbackCreateVm
            {
                BeneficiaryId = beneficiaryId ?? Guid.Empty,
                SubmittedByAdmin = true
            };

            await LoadCreateDropdowns(vm, beneficiaryId, ct);
            return View(vm);
        }

        // POST /Feedback/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FeedbackCreateVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadCreateDropdowns(vm, vm.BeneficiaryId, ct);
                return View(vm);
            }

            var userId = _users.GetUserId(User) ?? "";
            var (ok, error, id) = await _svc.CreateAsync(vm, userId, ct);

            if (!ok)
            {
                ModelState.AddModelError("", error ?? "Unable to save feedback.");
                await LoadCreateDropdowns(vm, vm.BeneficiaryId, ct);
                return View(vm);
            }

            TempData["Success"] = "Feedback recorded successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET /Feedback/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var vm = await _svc.GetDetailsAsync(id, ct);
            if (vm == null) return NotFound();
            return View(vm);
        }

        // POST /Feedback/Reply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(Guid id, string newReply, FeedbackStatus newStatus, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(newReply))
            {
                TempData["Error"] = "Reply cannot be empty.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = _users.GetUserId(User) ?? "";
            var (ok, error) = await _svc.ReplyAsync(id, newReply, newStatus, userId, ct);

            TempData[ok ? "Success" : "Error"] = ok
                ? "Reply saved and status updated."
                : error ?? "Unable to save reply.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST /Feedback/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(Guid id, FeedbackStatus status, CancellationToken ct)
        {
            var (ok, error) = await _svc.UpdateStatusAsync(id, status, ct);

            TempData[ok ? "Success" : "Error"] = ok
                ? "Status updated."
                : error ?? "Unable to update status.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // ── AJAX: load enrollments for a beneficiary ──────────────────
        [HttpGet]
        public async Task<IActionResult> GetEnrollments(Guid beneficiaryId, CancellationToken ct)
        {
            if (beneficiaryId == Guid.Empty) return Ok(Array.Empty<object>());

            var items = await _db.Enrollments
                .AsNoTracking()
                .Include(e => e.Cohort)
                    .ThenInclude(c => c.Programme)
                .Where(e => e.BeneficiaryId == beneficiaryId && e.IsActive)
                .OrderByDescending(e => e.StartDate)
                .Select(e => new
                {
                    id = e.Id,
                    name = e.Cohort.Programme.ProgrammeName + " (" + e.Cohort.CohortCode + ")"
                })
                .ToListAsync(ct);

            return Ok(items);
        }

        // ── Private helpers ───────────────────────────────────────────
        private async Task LoadCreateDropdowns(FeedbackCreateVm vm, Guid? selectedBeneficiary, CancellationToken ct)
        {
            vm.Beneficiaries = await _db.Beneficiaries
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
                .Select(x => new SelectListItem(
                    $"{x.FirstName} {x.LastName} ({x.IdentifierValue})",
                    x.Id.ToString()))
                .ToListAsync(ct);

            if (selectedBeneficiary.HasValue && selectedBeneficiary != Guid.Empty)
            {
                vm.Enrollments = await _db.Enrollments
                    .AsNoTracking()
                    .Include(e => e.Cohort)
                        .ThenInclude(c => c.Programme)
                    .Where(e => e.BeneficiaryId == selectedBeneficiary && e.IsActive)
                    .OrderByDescending(e => e.StartDate)
                    .Select(e => new SelectListItem(
                        e.Cohort.Programme.ProgrammeName + " (" + e.Cohort.CohortCode + ")",
                        e.Id.ToString()))
                    .ToListAsync(ct);
            }
        }
    }
}