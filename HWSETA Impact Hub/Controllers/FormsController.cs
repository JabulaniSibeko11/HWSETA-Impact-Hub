using DocumentFormat.OpenXml.Spreadsheet;
using HWSETA_Impact_Hub.Models.ViewModels.Forms;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace HWSETA_Impact_Hub.Controllers
{
    [AllowAnonymous]
    public class FormsController : Controller
    {

        private readonly IFormTemplateService _svc;
        private readonly IBeneficiaryInviteService _invites;
        public FormsController(IFormTemplateService svc,IBeneficiaryInviteService inviteService)
        {
            _svc = svc;
            _invites = inviteService;
        }
        
        public async Task<IActionResult> Public1(string token, string? email, string? phone, CancellationToken ct)
        {
            var vm = await _svc.GetPublicFormAsync(token, email, phone, ct);
            if (vm == null) return View("NotFoundForm");

            if (!vm.IsOpen)
                return View("Closed", vm);

            return View("Public", vm);
        }
        [HttpGet("/f/{token}")]
        public async Task<IActionResult> Public(string token, string? invite, string? email, string? phone, CancellationToken ct)
        {
            // If invite is required, validate invite first
            if (string.IsNullOrWhiteSpace(invite))
                return View("NotFoundForm"); // or show "Invalid invite"

            var inv = await _invites.GetInviteAsync(invite.Trim(), ct);
            if (!inv.ok || inv.invite == null) return View("NotFoundForm");

            // Load public form and prefill using beneficiary from invite
            var vm = await _svc.GetPublicFormAsync(token, email, phone, ct);
            if (vm == null) return View("NotFoundForm");
            if (!vm.IsOpen) return View("Closed", vm);

            // attach invite context to VM (add these props)
            vm.InviteToken = invite;
            vm.BeneficiaryId = inv.invite.BeneficiaryId;

            // (Optional) also attach prefill block:
            // vm.Prefill = await _svc.GetBeneficiaryPrefill(inv.invite.BeneficiaryId);

            return View("Public", vm);
        }

        [HttpPost("/f/{token}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(string token, PublicFormSubmitVm vm, CancellationToken ct)
        {
            vm.Token = token;
           
            vm.InviteToken = Request.Query["invite"];
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers.UserAgent.ToString();

            var (ok, error, submissionId, nextUrl) = await _svc.SubmitPublicAsync(vm, ip, ua, ct);

            if (!ok)
            {
                TempData["Error"] = error ?? "Unable to submit.";

                // re-render form
                var form = await _svc.GetPublicFormAsync(token,vm.Email,vm.Phone, ct);
                if (form == null) return View("NotFoundForm");
                if (!form.IsOpen) return View("Closed", form);

               

                return View("Public", form);
            }

            // ✅ If Registration form says Completed → Upload Proof
            if (!string.IsNullOrWhiteSpace(nextUrl))
                return Redirect(nextUrl);

            return RedirectToAction(nameof(ThankYou), new { token, id = submissionId });
        }


        [HttpGet("/f/{token}/thanks")]
        public IActionResult ThankYou(string token, Guid? id) => View();
    }
}

