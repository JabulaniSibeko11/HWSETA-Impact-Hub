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
                .Include(x => x.Programme)
                .Include(x => x.Provider)
                .Include(x => x.Employer)
                .OrderByDescending(x => x.StartDate)
                .ToListAsync(ct);

        public Task<Enrollment?> GetAsync(Guid id, CancellationToken ct) =>
      _db.Enrollments.AsNoTracking()
          .Include(x => x.Beneficiary)
          .Include(x => x.Programme)
          .Include(x => x.Provider)
          .Include(x => x.Employer)
          .FirstOrDefaultAsync(x => x.Id == id, ct);
        public Task<List<EnrollmentStatusHistory>> GetHistoryAsync(Guid enrollmentId, CancellationToken ct) =>
       _db.EnrollmentStatusHistories.AsNoTracking()
           .Where(x => x.EnrollmentId == enrollmentId)
           .OrderByDescending(x => x.StatusDate)
           .ThenByDescending(x => x.Id)
           .ToListAsync(ct);


        public async Task<(bool ok, string? error, Guid? enrollmentId)> CreateAsync(EnrollmentCreateVm vm, CancellationToken ct)
        {
            var benExists = await _db.Beneficiaries.AnyAsync(x => x.Id == vm.BeneficiaryId, ct);
            var progExists = await _db.Programmes.AnyAsync(x => x.Id == vm.ProgrammeId, ct);
            var provExists = await _db.Providers.AnyAsync(x => x.Id == vm.ProviderId, ct);

            if (!benExists) return (false, "Beneficiary not found.", null);
            if (!progExists) return (false, "Programme not found.", null);
            if (!provExists) return (false, "Provider not found.", null);

            if (vm.EmployerId.HasValue)
            {
                var empExists = await _db.Employers.AnyAsync(x => x.Id == vm.EmployerId.Value, ct);
                if (!empExists) return (false, "Employer not found.", null);
            }

            var dup = await _db.Enrollments.AnyAsync(x =>
                x.BeneficiaryId == vm.BeneficiaryId &&
                x.ProgrammeId == vm.ProgrammeId &&
                x.ProviderId == vm.ProviderId &&
                x.StartDate == vm.StartDate.Date, ct);

            if (dup) return (false, "This beneficiary is already enrolled in this Programme/Provider for that start date.", null);

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            var enrollment = new Enrollment
            {
                BeneficiaryId = vm.BeneficiaryId,
                ProgrammeId = vm.ProgrammeId,
                ProviderId = vm.ProviderId,
                EmployerId = vm.EmployerId,
                StartDate = vm.StartDate.Date,
                EndDate = vm.EndDate?.Date,
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
                StatusDate = vm.StartDate.Date,
                Reason = "Initial enrollment",
                Comment = vm.Notes?.Trim(),
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

            if ((vm.Status == EnrollmentStatus.Completed || vm.Status == EnrollmentStatus.DroppedOut) && enr.EndDate == null)
                enr.EndDate = vm.StatusDate.Date;

            var hist = new EnrollmentStatusHistory
            {
                EnrollmentId = enr.Id,
                Status = vm.Status,
                StatusDate = vm.StatusDate.Date,
                Reason = vm.Reason?.Trim(),
                Comment = vm.Comment?.Trim(),
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