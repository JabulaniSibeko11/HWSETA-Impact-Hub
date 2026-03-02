using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace HWSETA_Impact_Hub.Infrastructure.Seed
{
    public static class FormTemplateSeeder
    {
        private static string NewPublicToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(18);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
        {
            // 1) Find any existing active Registration templates
            var existingRegs = await db.FormTemplates
                .Where(x => x.IsActive && x.Purpose == FormPurpose.Registration)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .ToListAsync(ct);

            FormTemplate sys;

            // 2) Prefer our fixed System ID record
            sys = existingRegs.FirstOrDefault(x => x.Id == SystemTemplateIds.RegistrationTemplateId);

            // 3) If not found, but there is an existing registration template, "upgrade" it into the system template
            if (sys == null && existingRegs.Count > 0)
            {
                sys = existingRegs[0];

                // mark all others inactive (only one allowed)
                foreach (var other in existingRegs.Skip(1))
                {
                    other.IsActive = false;
                    other.Status = FormStatus.Archived;
                    other.UnpublishedAt = DateTime.UtcNow;
                    other.UpdatedAt = DateTime.UtcNow;
                }

                // upgrade primary
                sys.IsSystem = true;
                sys.IsDeletable = false;
                sys.IsEditable = true;

                sys.Title = string.IsNullOrWhiteSpace(sys.Title) ? "Beneficiary Registration" : sys.Title;
                sys.Description ??= "System registration form used for all new beneficiaries.";
                sys.Status = FormStatus.Published;
                sys.PublishedAt ??= DateTime.UtcNow;
                sys.UnpublishedAt = null;
                sys.IsActive = true;
                sys.UpdatedAt = DateTime.UtcNow;

                if (string.IsNullOrWhiteSpace(sys.PublicToken))
                    sys.PublicToken = NewPublicToken();

                await db.SaveChangesAsync(ct);
            }

            // 4) If nothing exists, create the fixed system row
            if (sys == null)
            {
                sys = new FormTemplate
                {
                    Id = SystemTemplateIds.RegistrationTemplateId,

                    Title = "Beneficiary Registration",
                    Description =
                        "System registration form used for all new beneficiaries. " +
                        "Beneficiaries confirm personal details, accept consent, then select programme/employer status information.",

                    Purpose = FormPurpose.Registration,

                    Status = FormStatus.Published,
                    PublishedAt = DateTime.UtcNow,

                    PublicToken = NewPublicToken(),

                    IsActive = true,

                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,

                    IsSystem = true,
                    IsDeletable = false,
                    IsEditable = true,

                    CreatedOnUtc = DateTime.UtcNow
                };

                db.FormTemplates.Add(sys);
                await db.SaveChangesAsync(ct);
            }

            // 5) Ensure FormPublish row exists and matches (so List + Send Center behave consistently)
            var pub = await db.FormPublishes.FirstOrDefaultAsync(x => x.FormTemplateId == sys.Id, ct);
            if (pub == null)
            {
                pub = new FormPublish
                {
                    FormTemplateId = sys.Id,
                    PublicToken = sys.PublicToken,

                    IsPublished = true,
                    IsOpen = true,
                    OpenFromUtc = DateTime.UtcNow,
                    CloseAtUtc = DateTime.MaxValue,
                    AllowMultipleSubmissions = true,

                    CreatedOnUtc = DateTime.UtcNow
                };
                db.FormPublishes.Add(pub);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(pub.PublicToken))
                    pub.PublicToken = sys.PublicToken;

                pub.IsPublished = true;
                pub.IsOpen = true;

                // Don’t force date windows if admin changes them later,
                // but protect against nonsense defaults:
                if (pub.OpenFromUtc == default) pub.OpenFromUtc = DateTime.UtcNow;
                if (pub.CloseAtUtc == default) pub.CloseAtUtc = DateTime.MaxValue;
            }

            await db.SaveChangesAsync(ct);
        }
    }
}