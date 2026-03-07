using HWSETA_Impact_Hub.Infrastructure.Identity;

using HWSETA_Impact_Hub.Models.ViewModels.Chats;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HWSETA_Impact_Hub.Controllers
{
    [Authorize(Policy = "AdminManage")]
    public sealed class ChatController : Controller
    {
        private readonly IChatService _svc;
        private readonly ICurrentUserService _user;

        public ChatController(IChatService svc, ICurrentUserService user)
        {
            _svc = svc;
            _user = user;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var vm = await _svc.GetInboxAsync(ct);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var vm = await _svc.BuildCreateVmAsync(ct);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateThreadVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var reloadVm = await _svc.BuildCreateVmAsync(ct);
                reloadVm.BeneficiaryId = vm.BeneficiaryId;
                reloadVm.Subject = vm.Subject;
                reloadVm.MessageText = vm.MessageText;
                reloadVm.AdminChatProfiles = reloadVm.AdminChatProfiles;
                reloadVm.AdminChatProfileId = vm.AdminChatProfileId;
                return View(reloadVm);
            }

            var (ok, error, threadId) = await _svc.CreateThreadAsync(
                vm,
                _user.UserId ?? "",
                ct);

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to create conversation.");

                var reloadVm = await _svc.BuildCreateVmAsync(ct);
                reloadVm.BeneficiaryId = vm.BeneficiaryId;
                reloadVm.Subject = vm.Subject;
                reloadVm.MessageText = vm.MessageText;
                reloadVm.AdminChatProfileId = vm.AdminChatProfileId;
                return View(reloadVm);
            }

            TempData["Success"] = "Conversation started successfully.";
            return RedirectToAction(nameof(Thread), new { id = threadId });
        }

        [HttpGet]
        public async Task<IActionResult> Thread(Guid id, CancellationToken ct)
        {
            var vm = await _svc.GetThreadAsync(id, _user.UserId ?? "", ct);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(ChatThreadVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please select a chatter name and enter a reply.";
                return RedirectToAction(nameof(Thread), new { id = vm.ThreadId });
            }

            var (ok, error) = await _svc.ReplyAsync(
                vm.ThreadId,
                vm.ReplyText,
                vm.AdminChatProfileId,
                _user.UserId ?? "",
                ct);

            if (!ok)
            {
                TempData["Error"] = error ?? "Failed to send reply.";
                return RedirectToAction(nameof(Thread), new { id = vm.ThreadId });
            }

            TempData["Success"] = "Reply sent.";
            return RedirectToAction(nameof(Thread), new { id = vm.ThreadId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(Guid id, CancellationToken ct)
        {
            var (ok, error) = await _svc.CloseThreadAsync(id, ct);

            if (!ok)
            {
                TempData["Error"] = error ?? "Failed to close conversation.";
                return RedirectToAction(nameof(Thread), new { id });
            }

            TempData["Success"] = "Conversation closed.";
            return RedirectToAction(nameof(Thread), new { id });
        }
    }
}