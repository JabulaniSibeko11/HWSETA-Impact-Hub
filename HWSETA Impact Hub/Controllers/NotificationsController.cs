using HWSETA_Impact_Hub.Models.ViewModels.Notifications;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HWSETA_Impact_Hub.Controllers
{
    [Authorize(Policy = "AdminManage")]
    public sealed class NotificationsController : Controller
    {
        private readonly INotificationFeedService _svc;

        public NotificationsController(INotificationFeedService svc)
        {
            _svc = svc;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            NotificationFeedFilter filter = NotificationFeedFilter.All,
            string? search = null,
            CancellationToken ct = default)
        {
            var vm = await _svc.GetFeedAsync(filter, search, ct);
            return View(vm);
        }
    }
}
