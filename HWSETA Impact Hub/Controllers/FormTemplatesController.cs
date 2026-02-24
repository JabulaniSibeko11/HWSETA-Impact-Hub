using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Forms;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Controllers
{
    [Authorize(Policy = "AdminManage")]
    public sealed class FormTemplatesController : Controller
    {
        private readonly IFormTemplateService _svc;
        private readonly ApplicationDbContext _db;
        private readonly IBeneficiaryInviteService _invites;
        public FormTemplatesController(IFormTemplateService svc,ApplicationDbContext db,IBeneficiaryInviteService inviteService)
        {
            _svc = svc;
            _db= db;
            _invites=inviteService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var list = await _svc.ListAsync(ct);
            return View(list);
        }

        [HttpGet]
        public IActionResult Create() => View(new FormTemplateCreateVm());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FormTemplateCreateVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(vm);

            var (ok, error, id) = await _svc.CreateAsync(vm, ct);
            if (!ok)
            {
                ModelState.AddModelError("", error ?? "Unable to create template.");
                return View(vm);
            }

            TempData["Success"] = "Form template created.";
            return RedirectToAction(nameof(Builder), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var entity = await _svc.GetEntityAsync(id, ct);
            if (entity == null) return NotFound();

            var vm = new FormTemplateEditVm
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                IsActive = entity.IsActive
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FormTemplateEditVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(vm);

            var (ok, error) = await _svc.UpdateAsync(vm, ct);
            if (!ok)
            {
                ModelState.AddModelError("", error ?? "Unable to update template.");
                return View(vm);
            }

            TempData["Success"] = "Form template updated.";
            return RedirectToAction(nameof(Builder), new { id = vm.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Builder(Guid id, CancellationToken ct)
        {
            var vm = await _svc.GetBuilderAsync(id, ct);
            if (vm == null) return NotFound();
            return View(vm);
        }

        // -------- Sections --------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSection(FormSectionCreateVm vm, CancellationToken ct)
        {
            var (ok, error, _) = await _svc.AddSectionAsync(vm, ct);
            if (!ok) TempData["Error"] = error ?? "Unable to add section.";
            else TempData["Success"] = "Section added.";

            return RedirectToAction(nameof(Builder), new { id = vm.FormTemplateId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSection(FormSectionEditVm vm, Guid templateId, CancellationToken ct)
        {
            var (ok, error) = await _svc.UpdateSectionAsync(vm, ct);
            if (!ok) TempData["Error"] = error ?? "Unable to update section.";
            else TempData["Success"] = "Section updated.";

            return RedirectToAction(nameof(Builder), new { id = templateId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSection(Guid sectionId, Guid templateId, CancellationToken ct)
        {
            var (ok, error) = await _svc.DeleteSectionAsync(sectionId, ct);
            if (!ok) TempData["Error"] = error ?? "Unable to delete section.";
            else TempData["Success"] = "Section deleted.";

            return RedirectToAction(nameof(Builder), new { id = templateId });
        }

        // -------- Fields --------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddField(FormFieldCreateVm vm, Guid templateId, CancellationToken ct)
        {
            var (ok, error, _) = await _svc.AddFieldAsync(vm, ct);
            if (!ok) TempData["Error"] = error ?? "Unable to add field.";
            else TempData["Success"] = "Question added.";

            return RedirectToAction(nameof(Builder), new { id = templateId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateField(FormFieldEditVm vm, Guid templateId, CancellationToken ct)
        {
            var (ok, error) = await _svc.UpdateFieldAsync(vm, ct);
            if (!ok) TempData["Error"] = error ?? "Unable to update field.";
            else TempData["Success"] = "Question updated.";

            return RedirectToAction(nameof(Builder), new { id = templateId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteField(Guid fieldId, Guid templateId, CancellationToken ct)
        {
            var (ok, error) = await _svc.DeleteFieldAsync(fieldId, ct);
            if (!ok) TempData["Error"] = error ?? "Unable to delete field.";
            else TempData["Success"] = "Question removed.";

            return RedirectToAction(nameof(Builder), new { id = templateId });
        }

        // -------- Options --------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOption(FormFieldOptionCreateVm vm, Guid templateId, CancellationToken ct)
        {
            var (ok, error, _) = await _svc.AddOptionAsync(vm, ct);
            if (!ok) TempData["Error"] = error ?? "Unable to add option.";
            else TempData["Success"] = "Option added.";

            return RedirectToAction(nameof(Builder), new { id = templateId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOption(FormFieldOptionEditVm vm, Guid templateId, CancellationToken ct)
        {
            var (ok, error) = await _svc.UpdateOptionAsync(vm, ct);
            if (!ok) TempData["Error"] = error ?? "Unable to update option.";
            else TempData["Success"] = "Option updated.";

            return RedirectToAction(nameof(Builder), new { id = templateId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOption(Guid optionId, Guid templateId, CancellationToken ct)
        {
            var (ok, error) = await _svc.DeleteOptionAsync(optionId, ct);
            if (!ok) TempData["Error"] = error ?? "Unable to delete option.";
            else TempData["Success"] = "Option removed.";

            return RedirectToAction(nameof(Builder), new { id = templateId });
        }

        [HttpGet]
        public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var vm = await _svc.GetPublishVmAsync(id, baseUrl, ct);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(FormPublishVm vm, CancellationToken ct)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            // Always re-hydrate VM so the page is correct even after errors
            var fresh = await _svc.GetPublishVmAsync(vm.TemplateId, baseUrl, ct);
            if (fresh == null) return NotFound();

            // Copy user inputs onto fresh VM (so they don't lose entries)
            fresh.OpenFromUtc = vm.OpenFromUtc;
            fresh.CloseAtUtc = vm.CloseAtUtc;
            fresh.MaxSubmissions = vm.MaxSubmissions;
            fresh.AllowMultipleSubmissions = vm.AllowMultipleSubmissions;
            fresh.IsOpen = vm.IsOpen;

            if (!ModelState.IsValid)
                return View(fresh);

            var (ok, error) = await _svc.PublishAsync(fresh, ct);
            if (!ok)
            {
                ModelState.AddModelError("", error ?? "Unable to publish.");
                return View(fresh);
            }

            TempData["Success"] = "Form published.";
            return RedirectToAction(nameof(SendCenter), new { templateId = fresh.TemplateId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unpublish(Guid templateId, CancellationToken ct)
        {
            var (ok, error) = await _svc.UnpublishAsync(templateId, ct);
            if (!ok)
            {
                TempData["Error"] = error ?? "Unable to unpublish.";
                return RedirectToAction(nameof(Publish), new { id = templateId });
            }

            TempData["Success"] = "Form unpublished.";
            return RedirectToAction(nameof(Publish), new { id = templateId });
        }

        [HttpGet]
        public async Task<IActionResult> SendCenter(Guid? templateId, CancellationToken ct)
        {
            // list published templates
            var templates = await _db.FormTemplates.AsNoTracking()
                .Where(t => t.IsActive && t.Status == FormStatus.Published)
                .OrderByDescending(t => t.Purpose == FormPurpose.Registration)
                .ThenBy(t => t.Title)
                .Select(t => new SendTemplatePickVm
                {
                    TemplateId = t.Id,
                    Title = t.Title,
                    Purpose = t.Purpose
                })
                .ToListAsync(ct);

            if (templates.Count == 0)
            {
                TempData["Error"] = "No published templates available to send.";
                return RedirectToAction(nameof(Index));
            }

            var selectedId = templateId ?? templates[0].TemplateId;

            var vm = new SendCenterVm
            {
                Templates = templates,
                SelectedTemplateId = selectedId,
                Filters = new FormSendBulkVm { TemplateId = selectedId, SendEmail = true, SendSms = true }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewSend(SendCenterVm vm, CancellationToken ct)
        {
            // reload template dropdown
            vm.Templates = await _db.FormTemplates.AsNoTracking()
                .Where(t => t.IsActive && t.Status == FormStatus.Published)
                .OrderByDescending(t => t.Purpose == FormPurpose.Registration)
                .ThenBy(t => t.Title)
                .Select(t => new SendTemplatePickVm
                {
                    TemplateId = t.Id,
                    Title = t.Title,
                    Purpose = t.Purpose
                })
                .ToListAsync(ct);

            // apply filters
            var q = _db.Beneficiaries.AsNoTracking().Where(b => b.IsActive);

            var f = vm.Filters;

            if (f.Status.HasValue) q = q.Where(b => b.RegistrationStatus == f.Status.Value);
            if (!string.IsNullOrWhiteSpace(f.Province)) q = q.Where(b => b.Province == f.Province);

            if (!string.IsNullOrWhiteSpace(f.Programme)) q = q.Where(b => b.Programme == f.Programme);
            if (!string.IsNullOrWhiteSpace(f.Provider)) q = q.Where(b => b.TrainingProvider == f.Provider);
            if (!string.IsNullOrWhiteSpace(f.Employer)) q = q.Where(b => b.Employer == f.Employer);

            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var s = f.Search.Trim();
                q = q.Where(b =>
                    (b.FirstName + " " + b.LastName).Contains(s) ||
                    (b.Email ?? "").Contains(s) ||
                    (b.MobileNumber ?? "").Contains(s) ||
                    (b.IdentifierValue ?? "").Contains(s));
            }

            if (f.SendEmail) q = q.Where(b => b.Email != null && b.Email != "");
            if (f.SendSms) q = q.Where(b => b.MobileNumber != null && b.MobileNumber != "");

            // show small preview list (top 50)
            vm.PreviewTotal = await q.CountAsync(ct);
            vm.PreviewRows = await q.Take(50).Select(b => new SendCenterRowVm
            {
                BeneficiaryId = b.Id,
                FullName = b.FirstName + " " + b.LastName,
                Email = b.Email,
                Mobile = b.MobileNumber,
                Province = b.Province,
                Status = b.RegistrationStatus
            }).ToListAsync(ct);

            return View("SendCenter", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendOne(Guid templateId, Guid beneficiaryId, bool sendEmail, bool sendSms, CancellationToken ct)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var sentBy = User?.Identity?.Name;

            var (ok, error) = await _invites.SendOneAsync(templateId, beneficiaryId, sendEmail, sendSms, baseUrl, sentBy, ct);

            TempData[ok ? "Success" : "Error"] = ok ? "Invite sent." : (error ?? "Send failed.");
            return RedirectToAction(nameof(SendCenter), new { templateId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendBulkNow(FormSendBulkVm vm, CancellationToken ct)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var sentBy = User?.Identity?.Name;

            var (ok, error, sent, failed) = await _invites.SendBulkAsync(vm, baseUrl, sentBy, ct);

            TempData[ok ? "Success" : "Error"] = ok
                ? $"Sent: {sent}, Failed: {failed}"
                : (error ?? "Bulk send failed.");

            return RedirectToAction(nameof(SendCenter), new { templateId = vm.TemplateId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendBulk(FormSendBulkVm vm, CancellationToken ct)
        {
            // Validate template + must be Published Registration
            var t = await _db.FormTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == vm.TemplateId, ct);
            if (t is null) return NotFound();

            if (t.Purpose != FormPurpose.Registration || t.Status != FormStatus.Published || !t.IsActive)
            {
                TempData["Error"] = "Bulk send is only available for an ACTIVE PUBLISHED Registration form.";
                return RedirectToAction(nameof(Publish), new { id = vm.TemplateId });
            }

            // Query beneficiaries
            var q = _db.Beneficiaries.AsQueryable();

            if (vm.Status.HasValue)
                q = q.Where(b => b.RegistrationStatus == vm.Status.Value);

            if (vm.OnlyNotInvitedYet)
                q = q.Where(b => b.InvitedAt == null);

            if (vm.OnlyMissingPasswordOrUser)
                q = q.Where(b => string.IsNullOrWhiteSpace(b.UserId) || b.PasswordSetAt == null);

            if (!string.IsNullOrWhiteSpace(vm.Programme))
                q = q.Where(b => b.Programme == vm.Programme);

            if (!string.IsNullOrWhiteSpace(vm.Provider))
                q = q.Where(b => b.TrainingProvider == vm.Provider);

            if (!string.IsNullOrWhiteSpace(vm.Employer))
                q = q.Where(b => b.Employer == vm.Employer);

            if (!string.IsNullOrWhiteSpace(vm.Province))
                q = q.Where(b => b.Province == vm.Province);

            if (!string.IsNullOrWhiteSpace(vm.Search))
            {
                var s = vm.Search.Trim();
                q = q.Where(b =>
                    (b.FirstName + " " + b.LastName).Contains(s) ||
                    (b.Email ?? "").Contains(s) ||
                    (b.MobileNumber ?? "").Contains(s) ||
                    (b.IdentifierValue ?? "").Contains(s));
            }

            // If sending email/SMS require those fields
            if (vm.SendEmail) q = q.Where(b => b.Email != null && b.Email != "");
            if (vm.SendSms) q = q.Where(b => b.MobileNumber != null && b.MobileNumber != "");

            var list = await q.Select(b => b.Id).ToListAsync(ct);

            int sent = 0, failed = 0;
            foreach (var id in list)
            {
                var (ok, err) = await _invites.SendInviteAsync(id, vm.SendEmail, vm.SendSms, ct);
                if (ok) sent++;
                else failed++;
            }

            TempData["Success"] = $"Bulk send done. Sent: {sent}, Failed: {failed}";
            return RedirectToAction(nameof(Publish), new { id = vm.TemplateId });
        }
    }
}