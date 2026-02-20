using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Models.ViewModels.Admin;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class AdminUserService : IAdminUserService
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly RoleManager<IdentityRole> _roles;
        private readonly SecurityOptions _security;

        public AdminUserService(
            UserManager<ApplicationUser> users,
            RoleManager<IdentityRole> roles,
            IOptions<SecurityOptions> security)
        {
            _users = users;
            _roles = roles;
            _security = security.Value;
        }

        public async Task<List<UserRowVm>> ListAsync(CancellationToken ct)
        {
            var all = await _users.Users
                .AsNoTracking()
                .OrderBy(u => u.Email)
                .ToListAsync(ct);

            var rows = new List<UserRowVm>();

            foreach (var u in all)
            {
                var roles = await _users.GetRolesAsync(u);

                rows.Add(new UserRowVm
                {
                    Id = u.Id,
                    Email = u.Email ?? u.UserName ?? "",
                    EmailConfirmed = u.EmailConfirmed,
                    Roles = string.Join(", ", roles.OrderBy(r => r))
                });
            }

            return rows;
        }

        public async Task<(bool ok, string? error)> CreateAsync(CreateUserVm vm, CancellationToken ct)
        {
            var role = (vm.Role ?? "").Trim();

            if (!_security.Roles.Contains(role))
                return (false, "Invalid role. Update appsettings.json Security:Roles.");

            // Ensure role exists (seed should do it, but be safe)
            if (!await _roles.RoleExistsAsync(role))
                return (false, $"Role '{role}' does not exist.");

            var existing = await _users.FindByEmailAsync(vm.Email);
            if (existing != null)
                return (false, "A user with this email already exists.");

            var user = new ApplicationUser
            {
                UserName = vm.Email.Trim(),
                Email = vm.Email.Trim(),
                EmailConfirmed = vm.EmailConfirmed
            };

            var create = await _users.CreateAsync(user, vm.TempPassword);
            if (!create.Succeeded)
                return (false, string.Join("; ", create.Errors.Select(e => e.Description)));

            var addRole = await _users.AddToRoleAsync(user, role);
            if (!addRole.Succeeded)
                return (false, string.Join("; ", addRole.Errors.Select(e => e.Description)));

            return (true, null);
        }
    }
}

