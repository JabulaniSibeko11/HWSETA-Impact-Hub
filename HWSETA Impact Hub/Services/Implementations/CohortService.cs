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
               .AsNoTracking()
               .OrderByDescending(x => x.StartDate)
               .ToListAsync(ct);

        public async Task<(bool ok, string? error)> CreateAsync(Guid currentUserId, Cohort cohort, CancellationToken ct)
        {
            if (cohort == null) return (false, "Invalid cohort payload.");
            if (currentUserId == Guid.Empty) return (false, "Current user is not available.");

            if (cohort.IntakeYear < 2000 || cohort.IntakeYear > 2100)
                return (false, "IntakeYear is invalid.");

            if (cohort.PlannedEndDate.Date < cohort.StartDate.Date)
                return (false, "Planned End Date cannot be earlier than Start Date.");

            if (cohort.ProgrammeId == Guid.Empty)
                return (false, "Programme is required.");

            if (cohort.ProviderId == Guid.Empty)
                return (false, "Training Provider is required.");

            if (cohort.EmployerId == Guid.Empty)
                return (false, "Employer is required.");

            // Validate Programme and derive QualificationTypeId
            var programme = await _db.Programmes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == cohort.ProgrammeId && p.IsActive, ct);

            if (programme is null)
                return (false, "Selected Programme is invalid or inactive.");

            if (programme.QualificationTypeId == Guid.Empty)
                return (false, "Selected Programme is missing a Qualification Type. Fix the programme setup first.");

            if (cohort.QualificationTypeId == Guid.Empty)
            {
                cohort.QualificationTypeId = programme.QualificationTypeId;
            }
            else if (cohort.QualificationTypeId != programme.QualificationTypeId)
            {
                return (false, "Qualification Type does not match the selected Programme.");
            }

            var qtExists = await _db.QualificationTypes
                .AsNoTracking()
                .AnyAsync(x => x.Id == cohort.QualificationTypeId && x.IsActive, ct);

            if (!qtExists)
                return (false, "Derived Qualification Type is invalid or missing from seed data.");

            var providerExists = await _db.Providers
                .AsNoTracking()
                .AnyAsync(x => x.Id == cohort.ProviderId && x.IsActive, ct);

            if (!providerExists)
                return (false, "Selected Provider is invalid or inactive.");

            var employerExists = await _db.Employers
                .AsNoTracking()
                .AnyAsync(x => x.Id == cohort.EmployerId && x.IsActive, ct);

            if (!employerExists)
                return (false, "Selected Employer is invalid or inactive.");

            // Normalise dates
            cohort.StartDate = cohort.StartDate.Date;
            cohort.PlannedEndDate = cohort.PlannedEndDate.Date;

            // Auto-generate cohort code
            // Ignore any manually supplied value
            const int maxAttempts = 5;
            string generatedCode = "";

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                generatedCode = await GenerateNextCohortCodeAsync(cohort.IntakeYear, ct);

                var exists = await _db.Cohorts
                    .AsNoTracking()
                    .AnyAsync(x => x.CohortCode == generatedCode, ct);

                if (!exists)
                    break;

                if (attempt == maxAttempts)
                    return (false, "Failed to generate a unique cohort code. Please try again.");
            }

            cohort.CohortCode = generatedCode;
            cohort.CreatedOnUtc = DateTime.UtcNow;
            cohort.CreatedByUserId = currentUserId;

            _db.Cohorts.Add(cohort);
            await _db.SaveChangesAsync(ct);

            return (true, null);
        }

        private async Task<string> GenerateNextCohortCodeAsync(int intakeYear, CancellationToken ct)
        {
            var prefix = $"COH-{intakeYear}-";

            var existingCodes = await _db.Cohorts
                .AsNoTracking()
                .Where(x => x.CohortCode != null && x.CohortCode.StartsWith(prefix))
                .Select(x => x.CohortCode!)
                .ToListAsync(ct);

            var maxNumber = 0;

            foreach (var code in existingCodes)
            {
                var tail = code[prefix.Length..];

                if (int.TryParse(tail, out var n) && n > maxNumber)
                    maxNumber = n;
            }

            var nextNumber = maxNumber + 1;
            return $"{prefix}{nextNumber:D4}";
        }
        public Task<string> PreviewNextCodeAsync(int intakeYear, CancellationToken ct) =>
    GenerateNextCohortCodeAsync(intakeYear, ct);
    }
}