using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class RegistrationService : IRegistrationService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public RegistrationService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<(bool ok, string? error)> SetPasswordAsync(Guid beneficiaryId, string email, string password, CancellationToken ct)
        {
            var ben = await _db.Beneficiaries.FirstOrDefaultAsync(x => x.Id == beneficiaryId, ct);
            if (ben is null) return (false, "Beneficiary not found.");

            // If beneficiary already has a user, allow update password by reset token (safe).
            if (!string.IsNullOrWhiteSpace(ben.UserId))
            {
                var existingUser = await _userManager.FindByIdAsync(ben.UserId);
                if (existingUser is null) return (false, "Linked user not found.");

                if (!string.Equals(existingUser.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    existingUser.Email = email;
                    existingUser.UserName = email;
                    await _userManager.UpdateAsync(existingUser);
                }

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                var resetRes = await _userManager.ResetPasswordAsync(existingUser, resetToken, password);

                if (!resetRes.Succeeded)
                    return (false, string.Join("; ", resetRes.Errors.Select(e => e.Description)));

                return (true, null);
            }

            // Else create user
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true // your choice; if you want confirm email, set false.
            };

            var res = await _userManager.CreateAsync(user, password);
            if (!res.Succeeded)
                return (false, string.Join("; ", res.Errors.Select(e => e.Description)));

            ben.UserId = user.Id;
            ben.Email = email;

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }
    }
}
