using HWSETA_Impact_Hub.Infrastructure.Confugations;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class SmtpEmailSenderService : IEmailSenderService
    {
        private readonly EmailOptions _opt;

        public SmtpEmailSenderService(IOptions<EmailOptions> opt)
        {
            _opt = opt.Value;
        }

        public async Task<(bool ok, string? error)> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
        {
            try
            {
                using var msg = new MailMessage();
                msg.From = new MailAddress(_opt.FromAddress, _opt.FromName);
                msg.To.Add(new MailAddress(toEmail));
                msg.Subject = subject;
                msg.Body = htmlBody;
                msg.IsBodyHtml = true;

                using var client = new SmtpClient(_opt.Smtp.Host, _opt.Smtp.Port)
                {
                    EnableSsl = _opt.Smtp.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_opt.Smtp.Username, _opt.Smtp.Password)
                };

                // SmtpClient has no true CancellationToken support
                await client.SendMailAsync(msg);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
