using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Models.ViewModels.Admin;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HWSETA_Impact_Hub.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminUsersController : Controller
    {
        private readonly IAdminUserService _svc;
        private readonly IAuditService _audit;
        private readonly SecurityOptions _security;

        public AdminUsersController(
            IAdminUserService svc,
            IAuditService audit,
            IOptions<SecurityOptions> security)
        {
            _svc = svc;
            _audit = audit;
            _security = security.Value;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var users = await _svc.ListAsync(ct);
            return View(users);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Roles = _security.Roles;
            return View(new CreateUserVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserVm vm, CancellationToken ct)
        {
            ViewBag.Roles = _security.Roles;

            if (!ModelState.IsValid)
                return View(vm);

            var (ok, error) = await _svc.CreateAsync(vm, ct);

            // ✅ Audit (manual)
            await _audit.LogViewAsync(
                entityName: "AdminUserManagement",
                entityId: vm.Email,
                note: ok ? $"Created user and assigned role: {vm.Role}" : $"Create failed: {error}",
                ct: ct);

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, error ?? "Create failed.");
                return View(vm);
            }

            TempData["Success"] = $"User created: {vm.Email} ({vm.Role})";
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
