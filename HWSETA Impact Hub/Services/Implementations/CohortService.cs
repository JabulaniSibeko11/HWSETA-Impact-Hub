using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class CohortService : ICohortService
    {
        private readonly ApplicationDbContext _db;

        public CohortService(ApplicationDbContext db)
        {
            _db = db;
        }

        public Task<List<Cohort>> ListAsync(CancellationToken ct) =>
            _db.Cohorts
               .Include(x => x.Programme)
                    .ThenInclude(p => p.QualificationType)
               .Include(x => x.Provider)
               .Include(x => x.Employer)
               .Include(x => x.FundingType)
               // ❌ Removed: .Include(x => x.Province) (Cohort has no Province)
               .AsNoTracking()
               .OrderByDescending(x => x.StartDate)
               .ToListAsync(ct);

        public async Task<(bool ok, string? error)> CreateAsync(Guid currentUserId, Cohort cohort, CancellationToken ct)
        {
            if (cohort == null) return (false, "Invalid cohort payload.");

            if (currentUserId == Guid.Empty)
                return (false, "Current user is not available.");

            var code = (cohort.CohortCode ?? "").Trim();
            if (string.IsNullOrWhiteSpace(code))
                return (false, "CohortCode is required.");

            if (await _db.Cohorts.AnyAsync(x => x.CohortCode == code, ct))
                return (false, "CohortCode already exists.");

            if (cohort.IntakeYear < 2000 || cohort.IntakeYear > 2100)
                return (false, "IntakeYear is invalid.");

            if (cohort.PlannedEndDate.Date < cohort.StartDate.Date)
                return (false, "Planned End Date cannot be earlier than Start Date.");

            cohort.CohortCode = code;
            cohort.StartDate = cohort.StartDate.Date;
            cohort.PlannedEndDate = cohort.PlannedEndDate.Date;

            cohort.CreatedOnUtc = DateTime.UtcNow;

            // ✅ if BaseEntity.CreatedByUserId is string:
            cohort.CreatedByUserId = currentUserId;

            _db.Cohorts.Add(cohort);
            await _db.SaveChangesAsync(ct);

            return (true, null);
        }
    }
}