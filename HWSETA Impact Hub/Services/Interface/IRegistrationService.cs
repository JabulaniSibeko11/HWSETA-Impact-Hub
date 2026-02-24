namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IRegistrationService
    {
        Task<(bool ok, string? error)> SetPasswordAsync(Guid beneficiaryId, string email, string password, CancellationToken ct);
    }
}
