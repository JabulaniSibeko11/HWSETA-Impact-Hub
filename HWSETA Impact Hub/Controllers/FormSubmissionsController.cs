using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HWSETA_Impact_Hub.Controllers
{
    [Authorize(Policy = "AdminManage")]
    public class FormSubmissionsController : Controller
    {
        private readonly IFormSubmissionService _svc;

        public FormSubmissionsController(IFormSubmissionService svc)
        {
            _svc = svc;
        }
        // /FormSubmissions/ByToken?token=xxxx
        [HttpGet]
        public async Task<IActionResult> ByToken(string token, CancellationToken ct)
        {
            var vm = await _svc.ListByTokenAsync(token, ct);
            if (vm == null) return NotFound();
            return View(vm);
        }

        // /FormSubmissions/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var vm = await _svc.GetDetailsAsync(id, ct);
            if (vm == null) return NotFound();
            return View(vm);
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
