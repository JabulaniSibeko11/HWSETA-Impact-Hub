using Microsoft.AspNetCore.Mvc;

namespace HWSETA_Impact_Hub.Services.Interface.ViewComponents
{
    public sealed class TopbarNotificationsViewComponent : ViewComponent
    {
        private readonly INotificationFeedService _svc;

        public TopbarNotificationsViewComponent(INotificationFeedService svc)
        {
            _svc = svc;
        }

        public async Task<IViewComponentResult> InvokeAsync(CancellationToken ct = default)
        {
            var vm = await _svc.GetTopbarSummaryAsync(ct);
            return View(vm);
        }
    }
}
