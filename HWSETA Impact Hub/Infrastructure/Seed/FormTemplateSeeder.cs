using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Infrastructure.Seed
{
    public static class FormTemplateSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
        {
            // One compulsory system registration template
            var exists = await db.FormTemplates.AnyAsync(x =>
                x.Purpose == FormPurpose.Registration && x.IsActive, ct);

            if (exists) return;

            var t = new FormTemplate
            {
                Title = "Beneficiary Registration",
                Description =
                    "System registration form used for all new beneficiaries. " +
                    "Beneficiaries confirm personal details, accept consent, then select programme/employer status information.",
                Purpose = FormPurpose.Registration,

                // Always published (system form)
                Status = FormStatus.Published,
                PublishedAt = DateTime.UtcNow,

                // required by your model for public form token, even if not used by dynamic renderer
                PublicToken = Guid.NewGuid().ToString("N"),

                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.FormTemplates.Add(t);
            await db.SaveChangesAsync(ct);
        }
    }
}