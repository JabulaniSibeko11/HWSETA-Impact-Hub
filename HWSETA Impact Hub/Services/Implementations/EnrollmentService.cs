using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Models.ViewModels.Enrollment;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class EnrollmentService : IEnrollmentService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _user;

        public EnrollmentService(ApplicationDbContext db, ICurrentUserService user)
        {
            _db = db;
            _user = user;
        }

        public Task<List<Enrollment>> ListAsync(CancellationToken ct) =>
            _db.Enrollments.AsNoTracking()
                .Include(x => x.Beneficiary)
                .Include(x => x.Cohort)
                    .ThenInclude(c => c.Programme)
                        .ThenInclude(p => p.QualificationType)
                .Include(x => x.Cohort)
                    .ThenInclude(c => c.Provider)
                .Include(x => x.Cohort)
                    .ThenInclude(c => c.Employer)
                .Include(x => x.Cohort)
                    .ThenInclude(c => c.FundingType)
                .OrderByDescending(x => x.StartDate)
                .ToListAsync(ct);

        public Task<Enrollment?> GetAsync(Guid id, CancellationToken ct) =>
            _db.Enrollments.AsNoTracking()
                .Include(x => x.Beneficiary)
                .Include(x => x.Cohort)
                    .ThenInclude(c => c.Programme)
                        .ThenInclude(p => p.QualificationType)
                .Include(x => x.Cohort)
                    .ThenInclude(c => c.Provider)
                .Include(x => x.Cohort)
                    .ThenInclude(c => c.Employer)
                .Include(x => x.Cohort)
                    .ThenInclude(c => c.FundingType)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

        public Task<List<EnrollmentStatusHistory>> GetHistoryAsync(Guid enrollmentId, CancellationToken ct) =>
            _db.EnrollmentStatusHistories.AsNoTracking()
                .Where(x => x.EnrollmentId == enrollmentId)
                .OrderByDescending(x => x.StatusDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);

        public async Task<(bool ok, string? error, Guid? enrollmentId)> CreateAsync(EnrollmentCreateVm vm, CancellationToken ct)
        {
            // Validate Beneficiary
            var beneficiaryExists = await _db.Beneficiaries.AnyAsync(x => x.Id == vm.BeneficiaryId, ct);
            if (!beneficiaryExists) return (false, "Beneficiary not found.", null);

            // Load Cohort (need StartDate)
            var cohort = await _db.Cohorts.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == vm.CohortId, ct);

            if (cohort == null) return (false, "Cohort not found.", null);

            // Duplicate check: Beneficiary can only be enrolled once per Cohort
            var dup = await _db.Enrollments.AnyAsync(x =>
                x.BeneficiaryId == vm.BeneficiaryId &&
                x.CohortId == vm.CohortId, ct);

            if (dup) return (false, "This beneficiary is already enrolled in this cohort.", null);

            var start = (vm.StartDate?.Date ?? cohort.StartDate.Date);

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            var enrollment = new Enrollment
            {
                BeneficiaryId = vm.BeneficiaryId,
                CohortId = vm.CohortId,
                StartDate = start,
                CurrentStatus = EnrollmentStatus.Enrolled,
                Notes = vm.Notes?.Trim(),
                IsActive = true,
                CreatedOnUtc = DateTime.UtcNow,
                CreatedByUserId = _user.UserId
            };

            _db.Enrollments.Add(enrollment);
            await _db.SaveChangesAsync(ct);

            var history = new EnrollmentStatusHistory
            {
                EnrollmentId = enrollment.Id,
                Status = EnrollmentStatus.Enrolled,
                StatusDate = start,
                Reason = "Initial enrollment",
                Comment = vm.Notes?.Trim(),
                ChangedByUserId = _user.UserId,
                CreatedOnUtc = DateTime.UtcNow,
                CreatedByUserId = _user.UserId
            };

            _db.EnrollmentStatusHistories.Add(history);
            await _db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);

            return (true, null, enrollment.Id);
        }

        public async Task<(bool ok, string? error)> UpdateStatusAsync(EnrollmentStatusUpdateVm vm, CancellationToken ct)
        {
            var enr = await _db.Enrollments.FirstOrDefaultAsync(x => x.Id == vm.EnrollmentId, ct);
            if (enr == null) return (false, "Enrollment not found.");

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            enr.CurrentStatus = vm.Status;
            enr.UpdatedOnUtc = DateTime.UtcNow;
            enr.UpdatedByUserId = _user.UserId;

            // Set actual end date for terminal statuses
            if (vm.Status == EnrollmentStatus.Completed || vm.Status == EnrollmentStatus.DroppedOut)
            {
                if (enr.ActualEndDate == null)
                    enr.ActualEndDate = vm.StatusDate.Date;
            }

            var hist = new EnrollmentStatusHistory
            {
                EnrollmentId = enr.Id,
                Status = vm.Status,
                StatusDate = vm.StatusDate.Date,
                Reason = vm.Reason?.Trim(),
                Comment = vm.Comment?.Trim(),
                ChangedByUserId = _user.UserId,
                CreatedOnUtc = DateTime.UtcNow,
                CreatedByUserId = _user.UserId
            };

            _db.EnrollmentStatusHistories.Add(hist);
            await _db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);

            return (true, null);
        }
    }
}
