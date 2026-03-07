using HWSETA_Impact_Hub.Models.ViewModels.Notifications;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface INotificationFeedService
    {
        Task<NotificationFeedVm> GetFeedAsync(
            NotificationFeedFilter filter,
            string? search,
            CancellationToken ct);

        Task<TopbarNotificationVm> GetTopbarSummaryAsync(CancellationToken ct);
    }
}
