using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Feedback;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IFeedbackService
    {
        Task<FeedbackIndexVm> GetIndexAsync(
            FeedbackStatus? status,
            FeedbackCategory? category,
            string? search,
            CancellationToken ct);

        Task<FeedbackDetailsVm?> GetDetailsAsync(Guid id, CancellationToken ct);

        Task<(bool ok, string? error, Guid? id)> CreateAsync(
            FeedbackCreateVm vm, string submittedByUserId, CancellationToken ct);

        Task<(bool ok, string? error)> ReplyAsync(
            Guid id, string reply, FeedbackStatus newStatus,
            string repliedByUserId, CancellationToken ct);

        Task<(bool ok, string? error)> UpdateStatusAsync(
            Guid id, FeedbackStatus status, CancellationToken ct);
    }
}
