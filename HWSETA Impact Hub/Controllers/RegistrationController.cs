using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Registrations;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Controllers
{
    [AllowAnonymous]
    public sealed class RegistrationController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBeneficiaryInviteService _invites;
        private readonly IRegistrationService _reg;

        public RegistrationController(ApplicationDbContext db, IBeneficiaryInviteService invites, IRegistrationService reg)
        {
            _db = db;
            _invites = invites;
            _reg = reg;
        }

        // Step 1: claim token -> show password page
        [HttpGet("/register/claim")]
        public async Task<IActionResult> Claim([FromQuery] string token, CancellationToken ct)
        {
            var (ok, beneficiaryId, err) = await _invites.ValidateTokenAsync(token, ct);
            if (!ok) return View("InvalidToken", err);

            var ben = await _db.Beneficiaries.AsNoTracking().FirstAsync(x => x.Id == beneficiaryId, ct);

            var vm = new SetPasswordVm
            {
                Token = token,
                Email = ben.Email ?? ""
            };

            return View("SetPassword", vm);
        }

        // Step 2: set password -> then force location
        [HttpPost("/register/set-password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View("SetPassword", vm);

            var (okToken, beneficiaryId, errToken) = await _invites.ValidateTokenAsync(vm.Token, ct);
            if (!okToken) return View("InvalidToken", errToken);

            var (ok, err) = await _reg.SetPasswordAsync(beneficiaryId, vm.Email, vm.Password, ct);
            if (!ok)
            {
                ModelState.AddModelError("", err ?? "Failed to set password.");
                return View("SetPassword", vm);
            }

            await _invites.MarkPasswordSetAsync(beneficiaryId, ct);

            // Go to location page (forced)
            return RedirectToAction(nameof(Location), new { token = vm.Token });
        }

        // Step 3: ask location permission then save coords
        [HttpGet("/register/location")]
        public IActionResult Location([FromQuery] string token)
        {
            var vm = new LocationVm { Token = token };
            return View("Location", vm);
        }

        [HttpPost("/register/location")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveLocation(LocationVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View("Location", vm);

            var (okToken, beneficiaryId, errToken) = await _invites.ValidateTokenAsync(vm.Token, ct);
            if (!okToken) return View("InvalidToken", errToken);

            await _invites.MarkLocationCapturedAsync(beneficiaryId, vm.Latitude, vm.Longitude, ct);

            // Now go directly to Registration Form (not dashboard)
            return RedirectToAction(nameof(RegistrationForm), new { token = vm.Token });
        }

        // Step 4: render registration form (dynamic form)
        [HttpGet("/register/form")]
        public async Task<IActionResult> RegistrationForm([FromQuery] string token, CancellationToken ct)
        {
            var (okToken, beneficiaryId, errToken) = await _invites.ValidateTokenAsync(token, ct);
            if (!okToken) return View("InvalidToken", errToken);

            // Fetch your single published Registration form
            // NOTE: adapt query to your real publish model
            var form = await _db.FormTemplates
                .AsNoTracking()
                .Where(x => x.IsActive && x.Status == FormStatus.Published)
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefaultAsync(ct);

            if (form is null)
                return View("InvalidToken", "Registration Form not published.");

            // Redirect to your existing dynamic form renderer endpoint
            // Example assumes: FormsController.Fill(templateId, beneficiaryId)
            return RedirectToAction("Fill", "Forms", new { templateId = form.Id, beneficiaryId, token });
        }

        // Step 5: upload proof of completion (only if status == Completed)
        [HttpGet("/register/proof")]
        public async Task<IActionResult> Proof([FromQuery] string token, CancellationToken ct)
        {
            var (okToken, beneficiaryId, errToken) = await _invites.ValidateTokenAsync(token, ct);
            if (!okToken) return View("InvalidToken", errToken);

            var ben = await _db.Beneficiaries.AsNoTracking().FirstAsync(x => x.Id == beneficiaryId, ct);

            if (ben.RegistrationStatus != BeneficiaryRegistrationStatus.Completed)
                return RedirectToAction(nameof(RegistrationDone));

            return View("UploadProof", new UploadProofVm { Token = token });
        }

        [HttpPost("/register/proof")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProof(UploadProofVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View("UploadProof", vm);

            var (okToken, beneficiaryId, errToken) = await _invites.ValidateTokenAsync(vm.Token, ct);
            if (!okToken) return View("InvalidToken", errToken);

            var ben = await _db.Beneficiaries.FirstAsync(x => x.Id == beneficiaryId, ct);

            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "proof");
            Directory.CreateDirectory(uploads);

            var ext = Path.GetExtension(vm.File.FileName);
            var safeName = $"{beneficiaryId:N}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
            var fullPath = Path.Combine(uploads, safeName);

            using (var fs = System.IO.File.Create(fullPath))
                await vm.File.CopyToAsync(fs, ct);

            ben.ProofOfCompletionPath = "/uploads/proof/" + safeName;
            ben.ProofUploadedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return RedirectToAction(nameof(RegistrationDone));
        }

        [HttpGet("/register/done")]
        public IActionResult RegistrationDone()
            => View("Done");
    }
}
