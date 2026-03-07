using Microsoft.EntityFrameworkCore;
using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Confugations;
using Microsoft.Extensions.Options;

namespace HWSETA_Impact_Hub.Infrastructure.Seed
{
    public sealed class AdminChatProfileSeeder
    {
        private readonly ApplicationDbContext _db;
        private readonly ChatProfileSeedOptions _options;

        public AdminChatProfileSeeder(
            ApplicationDbContext db,
            IOptions<ChatProfileSeedOptions> options)
        {
            _db = db;
            _options = options.Value ?? new ChatProfileSeedOptions();
        }

        public async Task SeedAsync()
        {
            if (_options.Profiles == null || _options.Profiles.Count == 0)
                return;

            var configuredNames = _options.Profiles
                .Where(x => !string.IsNullOrWhiteSpace(x.DisplayName))
                .Select(x => x.DisplayName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existing = await _db.AdminChatProfiles
                .ToListAsync();

            foreach (var profile in _options.Profiles)
            {
                var displayName = (profile.DisplayName ?? "").Trim();
                if (string.IsNullOrWhiteSpace(displayName))
                    continue;

                var found = existing.FirstOrDefault(x =>
                    x.DisplayName != null &&
                    x.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase));

                if (found == null)
                {
                    _db.AdminChatProfiles.Add(new AdminChatProfile
                    {
                        DisplayName = displayName,
                        AvatarColor = string.IsNullOrWhiteSpace(profile.AvatarColor) ? null : profile.AvatarColor.Trim(),
                        IsActive = profile.IsActive,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                }
                else
                {
                    found.AvatarColor = string.IsNullOrWhiteSpace(profile.AvatarColor) ? null : profile.AvatarColor.Trim();
                    found.IsActive = profile.IsActive;
                    found.UpdatedOnUtc = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}