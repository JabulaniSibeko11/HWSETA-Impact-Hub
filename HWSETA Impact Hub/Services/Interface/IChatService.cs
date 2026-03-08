using HWSETA_Impact_Hub.Models.ViewModels;
using HWSETA_Impact_Hub.Models.ViewModels.Chats;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IChatService
    {
        Task<ChatInboxVm> GetInboxAsync(CancellationToken ct);

        Task<ChatThreadVm?> GetThreadAsync(Guid threadId, string currentUserId, CancellationToken ct);

        Task<CreateThreadVm> BuildCreateVmAsync(CancellationToken ct);

        Task<(bool ok, string? error, Guid? threadId)> CreateThreadAsync(
            CreateThreadVm vm,
            string currentUserId,
            CancellationToken ct);

        Task<(bool ok, string? error)> ReplyAsync(
            Guid threadId,
            string replyText,
            Guid? adminChatProfileId,
            string currentUserId,
            CancellationToken ct);

        Task<(bool ok, string? error)> SendFormAsync(
            Guid threadId,
            Guid? adminChatProfileId,
            Guid? formPublishId,
            string? note,
            string currentUserId,
            CancellationToken ct);

        Task<(bool ok, string? error)> CloseThreadAsync(Guid threadId, CancellationToken ct);

        Task<List<AdminChatProfileOptionVm>> GetAdminChatProfilesAsync(CancellationToken ct);
    }
}
