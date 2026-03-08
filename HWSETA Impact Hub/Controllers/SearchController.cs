using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HWSETA_Impact_Hub.Controllers
{
    [Authorize]
    public sealed class SearchController : Controller
    {
        private readonly IGlobalSearchService _svc;
        private readonly ICurrentUserService _user;

        public SearchController(IGlobalSearchService svc, ICurrentUserService user)
        {
            _svc = svc;
            _user = user;
        }

        [HttpGet]
        public async Task<IActionResult> Quick(string q, CancellationToken ct)
        {
            var result = await _svc.SearchAsync(q, _user.UserId ?? "", ct);
            return Json(result);
        }
    }
}