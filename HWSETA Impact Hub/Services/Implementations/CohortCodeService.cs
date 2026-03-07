using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class CohortCodeService : ICohortCodeService
    {
        private readonly ApplicationDbContext _db;

        public CohortCodeService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<string> GenerateNextAsync(CancellationToken ct)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"COH-{year}-";

            var lastCode = await _db.Cohorts
                .AsNoTracking()
                .Where(x => x.CohortCode != null && x.CohortCode.StartsWith(prefix))
                .OrderByDescending(x => x.CohortCode)
                .Select(x => x.CohortCode)
                .FirstOrDefaultAsync(ct);

            var nextNumber = 1;

            if (!string.IsNullOrWhiteSpace(lastCode))
            {
                var tail = lastCode.Substring(prefix.Length);
                if (int.TryParse(tail, out var parsed))
                    nextNumber = parsed + 1;
            }

            return $"{prefix}{nextNumber:D4}";
        }
    }
}