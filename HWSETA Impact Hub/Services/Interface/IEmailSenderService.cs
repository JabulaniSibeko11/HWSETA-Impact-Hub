namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IEmailSenderService
    {
        Task<(bool ok, string? error)> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct);
    }
}
