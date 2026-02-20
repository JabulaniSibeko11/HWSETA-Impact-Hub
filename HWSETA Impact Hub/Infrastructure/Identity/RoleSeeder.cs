using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HWSETA_Impact_Hub.Infrastructure.Identity
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var opts = scope.ServiceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value;

            foreach (var r in opts.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!await roleManager.RoleExistsAsync(r))
                    await roleManager.CreateAsync(new IdentityRole(r));
            }
        }
    }
}
