using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HWSETA_Impact_Hub.Controllers
{
    [Authorize(Policy = "AdminManage")]
    public class ProgrammesController : Controller
    {
        private readonly IProgrammeService _svc;
        private readonly IAuditService _audit;

        public ProgrammesController(IProgrammeService svc, IAuditService audit)
        {
            _svc = svc;
            _audit = audit;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var list = await _svc.ListAsync(ct);
            return View(list);
        }

        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var item = await _svc.GetAsync(id, ct);
            if (item is null) return NotFound();

            await _audit.LogViewAsync("Programme", id.ToString(), null, ct);

            return View(item);
        }

        public IActionResult Create()
        {
            return View(new Programme
            {
                CohortYear = DateTime.UtcNow.Year,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(3)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Programme model, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(model);

            await _svc.CreateAsync(model, ct);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var item = await _svc.GetAsync(id, ct);
            if (item is null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Programme model, CancellationToken ct)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var ok = await _svc.UpdateAsync(model, ct);
            if (!ok) return NotFound();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var item = await _svc.GetAsync(id, ct);
            if (item is null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return RedirectToAction(nameof(Index));
        }
    }
}
