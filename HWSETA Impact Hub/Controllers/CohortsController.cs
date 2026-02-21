using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Models.ViewModels.Cohort; // <- keep your actual namespace
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Controllers
{
    public sealed class CohortsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _user;
        private readonly ICohortService _svc;

        public CohortsController(ApplicationDbContext db, ICurrentUserService user, ICohortService svc)
        {
            _db = db;
            _user = user;
            _svc = svc;
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
            var vm = new CohortCreateVm
            {
                IntakeYear = DateTime.Today.Year,
                StartDate = DateTime.Today,
                PlannedEndDate = DateTime.Today.AddMonths(12),
                IsActive = true
            };

            await ReloadDropdowns(vm, ct);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CohortCreateVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await ReloadDropdowns(vm, ct);
                return View(vm);
            }

            if (string.IsNullOrWhiteSpace(_user.UserId))
            {
                ModelState.AddModelError("", "Current user is not available. Please sign in again.");
                await ReloadDropdowns(vm, ct);
                return View(vm);
            }

            var entity = new Cohort
            {
                CohortCode = vm.CohortCode,
                ProgrammeId = vm.ProgrammeId,
                ProviderId = vm.ProviderId,
                EmployerId = vm.EmployerId,
                FundingTypeId = vm.FundingTypeId,

                IntakeYear = vm.IntakeYear,
                StartDate = vm.StartDate.Date,
                PlannedEndDate = vm.PlannedEndDate.Date,

                IsActive = vm.IsActive
            };
            if (!Guid.TryParse(_user.UserId, out var userGuid) || userGuid == Guid.Empty)
            {
                ModelState.AddModelError("", "Current user is not available. Please sign in again.");
                await ReloadDropdowns(vm, ct);
                return View(vm);
            }

            var (ok, error) = await _svc.CreateAsync(userGuid, entity, ct);
            TempData["Success"] = "Cohort created successfully.";
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var item = await _db.Cohorts
                .Include(x => x.Programme).ThenInclude(p => p.QualificationType)
                .Include(x => x.Provider)
                .Include(x => x.Employer)
                .Include(x => x.FundingType)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (item == null) return NotFound();
            return View(item);
        }

        private async Task ReloadDropdowns(CohortCreateVm vm, CancellationToken ct)
        {
            vm.Programmes = await _db.Programmes.Where(x => x.IsActive)
                .OrderBy(x => x.ProgrammeName)
                .Select(x => new SelectListItem { Text = x.ProgrammeName, Value = x.Id.ToString() })
                .ToListAsync(ct);

            vm.Providers = await _db.Providers.Where(x => x.IsActive)
                .OrderBy(x => x.ProviderName)
                .Select(x => new SelectListItem { Text = x.ProviderName, Value = x.Id.ToString() })
                .ToListAsync(ct);

            vm.Employers = await _db.Employers.Where(x => x.IsActive)
                .OrderBy(x => x.EmployerCode)
                .Select(x => new SelectListItem
                {
                    Text = $"{x.EmployerCode} - {x.RegistrationNumber}",
                    Value = x.Id.ToString()
                })
                .ToListAsync(ct);

            vm.FundingTypes = await _db.FundingTypes.Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                .ToListAsync(ct);
        }
    }
}