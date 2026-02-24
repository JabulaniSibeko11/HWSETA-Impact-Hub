using HWSETA_Impact_Hub.Services.Interface;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class DevSmsSenderService : ISmsSenderService
    {
        public Task<(bool ok, string? providerMessageId, string? error)> SendAsync(string toMsisdn, string message, CancellationToken ct)
        {
            // Real sending not happening here; you can log to DB if you want.
            Console.WriteLine($"[DEV SMS] TO={toMsisdn} MSG={message}");
            return Task.FromResult((true, "DEV-OK", (string?)null));
        }
    }
}
