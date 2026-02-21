using HWSETA_Impact_Hub.Models.ViewModels.Forms;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HWSETA_Impact_Hub.Controllers
{
    [AllowAnonymous]
    public class FormsController : Controller
    {

        private readonly IFormTemplateService _svc;

        public FormsController(IFormTemplateService svc)
        {
            _svc = svc;
        }
        [HttpGet("/f/{token}")]
        public async Task<IActionResult> Public(string token, CancellationToken ct)
        {
            var vm = await _svc.GetPublicFormAsync(token, ct);
            if (vm == null) return View("NotFoundForm");

            if (!vm.IsOpen)
                return View("Closed", vm);

            return View("Public", vm);
        }


        [HttpPost("/f/{token}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(string token, PublicFormSubmitVm vm, CancellationToken ct)
        {
            vm.Token = token;

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers.UserAgent.ToString();

            var (ok, error, submissionId) = await _svc.SubmitPublicAsync(vm, ip, ua, ct);
            if (!ok)
            {
                TempData["Error"] = error ?? "Unable to submit.";

                // re-render form
                var form = await _svc.GetPublicFormAsync(token, ct);
                if (form == null) return View("NotFoundForm");
                if (!form.IsOpen) return View("Closed", form);

                // keep entered email/phone only (answers are in Request.Form anyway)
                form.PrefillEmail = vm.Email;
                form.PrefillPhone = vm.Phone;

                return View("Public", form);
            }

            return RedirectToAction(nameof(ThankYou), new { token, id = submissionId });
        }

        [HttpGet("/f/{token}/thanks")]
        public IActionResult ThankYou(string token, Guid? id) => View();
    }
}

