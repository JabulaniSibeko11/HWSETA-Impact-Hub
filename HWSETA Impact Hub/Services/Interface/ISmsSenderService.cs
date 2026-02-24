namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface ISmsSenderService
    {
        Task<(bool ok, string? providerMessageId, string? error)> SendAsync(string toMsisdn, string message, CancellationToken ct);
    }
}
