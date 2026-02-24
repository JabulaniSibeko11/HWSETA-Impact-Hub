using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Forms;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IBeneficiaryInviteService
    {
        Task<(bool ok, string? error)> SendInviteAsync(Guid beneficiaryId, bool sendEmail, bool sendSms, CancellationToken ct);

        // Validates token and returns beneficiaryId
        Task<(bool ok, Guid beneficiaryId, string? error)> ValidateTokenAsync(string token, CancellationToken ct);

        Task MarkPasswordSetAsync(Guid beneficiaryId, CancellationToken ct);
        Task MarkLocationCapturedAsync(Guid beneficiaryId, decimal lat, decimal lon, CancellationToken ct);



        Task<(bool ok, string? error)> SendOneAsync(Guid formTemplateId, Guid beneficiaryId, bool sendEmail, bool sendSms, string baseUrl, string? sentByUserId, CancellationToken ct);

        Task<(bool ok, string? error, int sent, int failed)> SendBulkAsync(FormSendBulkVm vm, string baseUrl, string? sentByUserId, CancellationToken ct);

        Task<(bool ok, string? error, BeneficiaryFormInvite? invite)> GetInviteAsync(string inviteToken, CancellationToken ct);

    }
}
