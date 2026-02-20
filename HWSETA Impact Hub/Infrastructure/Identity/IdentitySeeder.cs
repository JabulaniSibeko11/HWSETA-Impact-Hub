using HWSETA_Impact_Hub.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HWSETA_Impact_Hub.Infrastructure.Identity
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var opts = scope.ServiceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value;

            // 1) Seed roles
            foreach (var r in opts.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!await roleManager.RoleExistsAsync(r))
                    await roleManager.CreateAsync(new IdentityRole(r));
            }

            // 2) Seed bootstrap admin (from appsettings.json)
            var email = opts.BootstrapAdmin?.Email?.Trim();
            var password = opts.BootstrapAdmin?.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return;

            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    // IMPORTANT: If RequireConfirmedAccount=true, this MUST be true
                    EmailConfirmed = true
                };

                var create = await userManager.CreateAsync(user, password);
                if (!create.Succeeded)
                {
                    var msg = string.Join("; ", create.Errors.Select(e => e.Description));
                    throw new InvalidOperationException("Bootstrap admin creation failed: " + msg);
                }
            }
            else
            {
                // Ensure confirmed so login works
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await userManager.UpdateAsync(user);
                }
            }

            // Ensure Admin role assigned
            if (!await userManager.IsInRoleAsync(user, "Admin"))
                await userManager.AddToRoleAsync(user, "Admin");
        }
    }
}

