using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Forms;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
                Filters = new FormSendBulkVm
                {
                    TemplateId = selectedId,
                    SendEmail = true,
                    SendSms = true
                },
                PreviewRows = new List<SendCenterRowVm>(),
                PreviewTotal = 0
            };

            await LoadSendCenterDropdownsAsync(vm.Filters, ct);

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

            // ✅ Ensure TemplateId in Filters matches SelectedTemplateId
            vm.Filters ??= new FormSendBulkVm();
            vm.Filters.TemplateId = vm.SelectedTemplateId;

            await LoadSendCenterDropdownsAsync(vm.Filters, ct);

            var f = vm.Filters;

            // ✅ Base query (use navigation, not join)
            var q = _db.Beneficiaries
                .AsNoTracking()
                .Where(b => b.IsActive)
                .Include(b => b.Address)
                    .ThenInclude(a => a.Province) // if Province navigation exists
                .AsQueryable();

            // Status filter
            if (f.Status.HasValue)
                q = q.Where(b => b.RegistrationStatus == f.Status.Value);

            if (f.OnlyNotInvitedYet)
                q = q.Where(b => b.InvitedAt == null);

            if (f.OnlyMissingPasswordOrUser)
                q = q.Where(b => string.IsNullOrWhiteSpace(b.UserId) || b.PasswordSetAt == null);

            // Province filter (Address lookup)
            if (f.ProvinceId.HasValue && f.ProvinceId.Value != Guid.Empty)
                q = q.Where(b => b.Address != null && b.Address.ProvinceId == f.ProvinceId.Value);

            // Cohort-based filters (via Enrollment -> Cohort)
            if (f.QualificationTypeId.HasValue && f.QualificationTypeId.Value != Guid.Empty)
            {
                var qid = f.QualificationTypeId.Value;
                q = q.Where(b => _db.Enrollments.Any(e =>
                    e.BeneficiaryId == b.Id &&
                    _db.Cohorts.Any(c => c.Id == e.CohortId && c.QualificationTypeId == qid)));
            }

            if (f.ProgrammeId.HasValue && f.ProgrammeId.Value != Guid.Empty)
            {
                var pid = f.ProgrammeId.Value;
                q = q.Where(b => _db.Enrollments.Any(e =>
                    e.BeneficiaryId == b.Id &&
                    _db.Cohorts.Any(c => c.Id == e.CohortId && c.ProgrammeId == pid)));
            }

            if (f.ProviderId.HasValue && f.ProviderId.Value != Guid.Empty)
            {
                var prid = f.ProviderId.Value;
                q = q.Where(b => _db.Enrollments.Any(e =>
                    e.BeneficiaryId == b.Id &&
                    _db.Cohorts.Any(c => c.Id == e.CohortId && c.ProviderId == prid)));
            }

            if (f.EmployerId.HasValue && f.EmployerId.Value != Guid.Empty)
            {
                var eid = f.EmployerId.Value;
                q = q.Where(b => _db.Enrollments.Any(e =>
                    e.BeneficiaryId == b.Id &&
                    _db.Cohorts.Any(c => c.Id == e.CohortId && c.EmployerId == eid)));
            }

            // Search
            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var s = f.Search.Trim();
                q = q.Where(b =>
                    (b.FirstName + " " + b.LastName).Contains(s) ||
                    (b.Email ?? "").Contains(s) ||
                    (b.MobileNumber ?? "").Contains(s) ||
                    (b.IdentifierValue ?? "").Contains(s));
            }

            // If sending email/SMS require those fields
            if (f.SendEmail)
                q = q.Where(b => b.Email != null && b.Email != "");

            if (f.SendSms)
                q = q.Where(b => b.MobileNumber != null && b.MobileNumber != "");

            vm.PreviewTotal = await q.CountAsync(ct);

            // ✅ Project preview (Top 50)
            // Note: Programme/Provider/Employer pulled from the latest enrollment’s cohort via subquery
            vm.PreviewRows = await q.Take(50).Select(b => new SendCenterRowVm
            {
                BeneficiaryId = b.Id,
                FullName = (b.FirstName + " " + b.LastName).Trim(),
                Email = b.Email,
                Mobile = b.MobileNumber,

                Province = b.Address != null && b.Address.Province != null ? b.Address.Province.Name : null,

                Programme = _db.Enrollments
                    .Where(e => e.BeneficiaryId == b.Id)
                    .OrderByDescending(e => e.UpdatedOnUtc ?? e.CreatedOnUtc)
                    .Select(e => e.Cohort.Programme.ProgrammeName)
                    .FirstOrDefault(),

                TrainingProvider = _db.Enrollments
                    .Where(e => e.BeneficiaryId == b.Id)
                    .OrderByDescending(e => e.UpdatedOnUtc ?? e.CreatedOnUtc)
                    .Select(e => e.Cohort.Provider.ProviderName)
                    .FirstOrDefault(),

                Employer = _db.Enrollments
                    .Where(e => e.BeneficiaryId == b.Id)
                    .OrderByDescending(e => e.UpdatedOnUtc ?? e.CreatedOnUtc)
                    .Select(e => e.Cohort.Employer.EmployerName)
                    .FirstOrDefault(),

                RegistrationStatus = b.RegistrationStatus,
                InvitedAt = b.InvitedAt
            }).ToListAsync(ct);

            return View("SendCenter", vm);
        }
        private async Task LoadSendCenterDropdownsAsync(FormSendBulkVm f, CancellationToken ct)
        {
            // Provinces
            f.Provinces = await _db.Provinces.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToListAsync(ct);

            // Qualification Types
            f.QualificationTypes = await _db.QualificationTypes.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                .ToListAsync(ct);

            // These are cohort-derived to avoid showing items that can never match
            f.Programmes = await _db.Cohorts.AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new { c.ProgrammeId, c.Programme.ProgrammeName })
                .Distinct()
                .OrderBy(x => x.ProgrammeName)
                .Select(x => new SelectListItem { Value = x.ProgrammeId.ToString(), Text = x.ProgrammeName })
                .ToListAsync(ct);

            f.Providers = await _db.Cohorts.AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new { c.ProviderId, c.Provider.ProviderName })
                .Distinct()
                .OrderBy(x => x.ProviderName)
                .Select(x => new SelectListItem { Value = x.ProviderId.ToString(), Text = x.ProviderName })
                .ToListAsync(ct);

            f.Employers = await _db.Cohorts.AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new { c.EmployerId, c.Employer.EmployerName })
                .Distinct()
                .OrderBy(x => x.EmployerName)
                .Select(x => new SelectListItem { Value = x.EmployerId.ToString(), Text = x.EmployerName })
                .ToListAsync(ct);
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
            // Validate template exists (we allow bulk for any published template, but you can restrict)
            var t = await _db.FormTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == vm.TemplateId, ct);
            if (t is null)
            {
                TempData["Error"] = "Template not found.";
                return RedirectToAction(nameof(SendCenter));
            }

            if (!t.IsActive || t.Status != FormStatus.Published)
            {
                TempData["Error"] = "Template must be ACTIVE and PUBLISHED to send.";
                return RedirectToAction(nameof(SendCenter), new { templateId = vm.TemplateId });
            }

            // Build same query logic as Preview (GUID-based)
            var q = BuildSendCenterQuery(vm);

            // Require email/sms if selected
            if (vm.SendEmail) q = q.Where(b => b.Email != null && b.Email != "");
            if (vm.SendSms) q = q.Where(b => b.MobileNumber != null && b.MobileNumber != "");

            // Execute list
            var ids = await q.Select(b => b.Id).ToListAsync(ct);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var sentBy = User?.Identity?.Name;

            int sent = 0, failed = 0;
            foreach (var id in ids)
            {
                var (ok, err) = await _invites.SendOneAsync(vm.TemplateId, id, vm.SendEmail, vm.SendSms, baseUrl, sentBy, ct);
                if (ok) sent++;
                else failed++;
            }

            TempData[sent > 0 ? "Success" : "Error"] = $"Bulk send done. Sent: {sent}, Failed: {failed}";
            return RedirectToAction(nameof(SendCenter), new { templateId = vm.TemplateId });
        }

        // ✅ single source of truth for filtering (same as Preview)
        private IQueryable<Beneficiary> BuildSendCenterQuery(FormSendBulkVm f)
        {
            var q = _db.Beneficiaries
                .AsNoTracking()
                .Where(b => b.IsActive)
                .Include(b => b.Address)
                    .ThenInclude(a => a.Province)
                .AsQueryable();

            if (f.Status.HasValue)
                q = q.Where(b => b.RegistrationStatus == f.Status.Value);

            if (f.OnlyNotInvitedYet)
                q = q.Where(b => b.InvitedAt == null);

            if (f.OnlyMissingPasswordOrUser)
                q = q.Where(b => string.IsNullOrWhiteSpace(b.UserId) || b.PasswordSetAt == null);

            if (f.ProvinceId.HasValue && f.ProvinceId.Value != Guid.Empty)
                q = q.Where(b => b.Address != null && b.Address.ProvinceId == f.ProvinceId.Value);

            // Enrollment/Cohort-based filters
            if (f.QualificationTypeId.HasValue && f.QualificationTypeId.Value != Guid.Empty)
            {
                var qid = f.QualificationTypeId.Value;
                q = q.Where(b => _db.Enrollments.Any(e =>
                    e.BeneficiaryId == b.Id &&
                    _db.Cohorts.Any(c => c.Id == e.CohortId && c.QualificationTypeId == qid)));
            }

            if (f.ProgrammeId.HasValue && f.ProgrammeId.Value != Guid.Empty)
            {
                var pid = f.ProgrammeId.Value;
                q = q.Where(b => _db.Enrollments.Any(e =>
                    e.BeneficiaryId == b.Id &&
                    _db.Cohorts.Any(c => c.Id == e.CohortId && c.ProgrammeId == pid)));
            }

            if (f.ProviderId.HasValue && f.ProviderId.Value != Guid.Empty)
            {
                var prid = f.ProviderId.Value;
                q = q.Where(b => _db.Enrollments.Any(e =>
                    e.BeneficiaryId == b.Id &&
                    _db.Cohorts.Any(c => c.Id == e.CohortId && c.ProviderId == prid)));
            }

            if (f.EmployerId.HasValue && f.EmployerId.Value != Guid.Empty)
            {
                var eid = f.EmployerId.Value;
                q = q.Where(b => _db.Enrollments.Any(e =>
                    e.BeneficiaryId == b.Id &&
                    _db.Cohorts.Any(c => c.Id == e.CohortId && c.EmployerId == eid)));
            }

            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var s = f.Search.Trim();
                q = q.Where(b =>
                    (b.FirstName + " " + b.LastName).Contains(s) ||
                    (b.Email ?? "").Contains(s) ||
                    (b.MobileNumber ?? "").Contains(s) ||
                    (b.IdentifierValue ?? "").Contains(s));
            }

            return q;
        }
    }
}