using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class LoginRedirectService : ILoginRedirectService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SecurityOptions _securityOptions;

        public LoginRedirectService(
            UserManager<ApplicationUser> userManager,
            IOptions<SecurityOptions> securityOptions)
        {
            _userManager = userManager;
            _securityOptions = securityOptions.Value;
        }

        public async Task<(string controller, string action)> GetRedirectAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return (
                    _securityOptions.LoginRedirects.AdminDefaultController,
                    _securityOptions.LoginRedirects.AdminDefaultAction
                );
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (
                    _securityOptions.LoginRedirects.AdminDefaultController,
                    _securityOptions.LoginRedirects.AdminDefaultAction
                );
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Any(r => r.Equals("Beneficiary", StringComparison.OrdinalIgnoreCase)))
            {
                return (
                    _securityOptions.LoginRedirects.BeneficiaryDefaultController,
                    _securityOptions.LoginRedirects.BeneficiaryDefaultAction
                );
            }

            return (
                _securityOptions.LoginRedirects.AdminDefaultController,
                _securityOptions.LoginRedirects.AdminDefaultAction
            );
        }
    }
}
