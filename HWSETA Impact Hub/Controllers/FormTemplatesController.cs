using HWSETA_Impact_Hub.Models.ViewModels.Forms;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HWSETA_Impact_Hub.Controllers
{
    [Authorize(Policy = "AdminManage")]
    public sealed class FormTemplatesController : Controller
    {
        private readonly IFormTemplateService _svc;

        public FormTemplatesController(IFormTemplateService svc)
        {
            _svc = svc;
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
    }
}