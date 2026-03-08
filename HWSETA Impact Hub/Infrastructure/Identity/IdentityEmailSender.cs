using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace HWSETA_Impact_Hub.Infrastructure.Identity
{
    public sealed class IdentityEmailSender : IEmailSender
    {
        private readonly IEmailSenderService _emailSender;

        public IdentityEmailSender(IEmailSenderService emailSender)
        {
            _emailSender = emailSender;
        }
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // TEMP: no-op so Forgot Password stops crashing
            // Replace this with your real email sending logic next
            return Task.CompletedTask;
        }
      
    }
}
