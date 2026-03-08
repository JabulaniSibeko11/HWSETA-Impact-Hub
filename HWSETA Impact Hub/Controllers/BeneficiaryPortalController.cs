
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HWSETA_Impact_Hub.Controllers
{
    [Authorize]
    public sealed class BeneficiaryPortalController : Controller
    {
        private readonly IBeneficiaryPortalService _svc;
        private readonly ICurrentUserService _user;

        public BeneficiaryPortalController(
            IBeneficiaryPortalService svc,
            ICurrentUserService user)
        {
            _svc = svc;
            _user = user;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var vm = await _svc.GetDashboardAsync(_user.UserId ?? "", ct);
            if (vm == null)
                return Forbid();

            return View(vm);
        }
    }
}
