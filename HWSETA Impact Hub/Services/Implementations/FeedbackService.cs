using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Feedback;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HWSETA_Impact_Hub.Infrastructure.Identity;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class FeedbackService : IFeedbackService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public FeedbackService(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        // ── Index ────────────────────────────────────────────────────
        public async Task<FeedbackIndexVm> GetIndexAsync(
            FeedbackStatus? status,
            FeedbackCategory? category,
            string? search,
            CancellationToken ct)
        {
            var q = _db.BeneficiaryFeedbacks
                .AsNoTracking()
                .Include(x => x.Beneficiary)
                .Where(x => x.IsActive);

            if (status.HasValue) q = q.Where(x => x.Status == status.Value);
            if (category.HasValue) q = q.Where(x => x.Category == category.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(x =>
                    x.Beneficiary.FirstName.ToLower().Contains(s) ||
                    x.Beneficiary.LastName.ToLower().Contains(s) ||
                    x.Beneficiary.IdentifierValue.ToLower().Contains(s) ||
                    (x.Subject != null && x.Subject.ToLower().Contains(s)) ||
                    x.Message.ToLower().Contains(s));
            }

            var rows = await q
                .OrderByDescending(x => x.CreatedOnUtc)
                .Select(x => new FeedbackListRowVm
                {
                    Id = x.Id,
                    BeneficiaryName = x.Beneficiary.FirstName + " " + x.Beneficiary.LastName,
                    BeneficiaryId_ = x.Beneficiary.IdentifierValue,
                    Category = x.Category,
                    Status = x.Status,
                    Rating = x.Rating,
                    Subject = x.Subject,
                    Message = x.Message.Length > 120 ? x.Message.Substring(0, 120) + "…" : x.Message,
                    HasReply = x.AdminReply != null,
                    SubmittedByAdmin = x.SubmittedByAdmin,
                    CreatedOnUtc = x.CreatedOnUtc
                })
                .ToListAsync(ct);

            // Totals (always unfiltered except IsActive)
            var totals = await _db.BeneficiaryFeedbacks
                .AsNoTracking()
                .Where(x => x.IsActive)
                .GroupBy(x => x.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            return new FeedbackIndexVm
            {
                Rows = rows,
                FilterStatus = status,
                FilterCategory = category,
                FilterSearch = search,
                TotalNew = totals.FirstOrDefault(t => t.Status == FeedbackStatus.New)?.Count ?? 0,
                TotalUnderReview = totals.FirstOrDefault(t => t.Status == FeedbackStatus.UnderReview)?.Count ?? 0,
                TotalResolved = totals.FirstOrDefault(t => t.Status == FeedbackStatus.Resolved)?.Count ?? 0,
                TotalClosed = totals.FirstOrDefault(t => t.Status == FeedbackStatus.Closed)?.Count ?? 0,
            };
        }

        // ── Details ──────────────────────────────────────────────────
        public async Task<FeedbackDetailsVm?> GetDetailsAsync(Guid id, CancellationToken ct)
        {
            var fb = await _db.BeneficiaryFeedbacks
                .AsNoTracking()
                .Include(x => x.Beneficiary)
                .Include(x => x.Enrollment)
                    .ThenInclude(e => e!.Cohort)
                        .ThenInclude(c => c.Programme)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);

            if (fb == null) return null;

            // Resolve replier email
            string? replierEmail = null;
            if (!string.IsNullOrWhiteSpace(fb.RepliedByUserId))
            {
                var replier = await _users.FindByIdAsync(fb.RepliedByUserId);
                replierEmail = replier?.Email;
            }

            return new FeedbackDetailsVm
            {
                Id = fb.Id,
                BeneficiaryId = fb.BeneficiaryId,
                BeneficiaryName = $"{fb.Beneficiary.FirstName} {fb.Beneficiary.LastName}",
                BeneficiaryIdentifier = fb.Beneficiary.IdentifierValue,
                BeneficiaryEmail = fb.Beneficiary.Email,
                BeneficiaryMobile = fb.Beneficiary.MobileNumber,
                EnrollmentId = fb.EnrollmentId,
                Programme = fb.Enrollment?.Cohort?.Programme?.ProgrammeName,
                Category = fb.Category,
                Status = fb.Status,
                Rating = fb.Rating,
                Subject = fb.Subject,
                Message = fb.Message,
                SubmittedByAdmin = fb.SubmittedByAdmin,
                CreatedOnUtc = fb.CreatedOnUtc,
                AdminReply = fb.AdminReply,
                RepliedAt = fb.RepliedAt,
                RepliedByEmail = replierEmail,
                NewStatus = fb.Status,
            };
        }

        // ── Create ───────────────────────────────────────────────────
        public async Task<(bool ok, string? error, Guid? id)> CreateAsync(
            FeedbackCreateVm vm, string submittedByUserId, CancellationToken ct)
        {
            var beneficiary = await _db.Beneficiaries
                .AnyAsync(x => x.Id == vm.BeneficiaryId && x.IsActive, ct);

            if (!beneficiary) return (false, "Beneficiary not found.", null);

            if (vm.EnrollmentId.HasValue)
            {
                var enrollmentExists = await _db.Enrollments
                    .AnyAsync(x => x.Id == vm.EnrollmentId && x.BeneficiaryId == vm.BeneficiaryId, ct);

                if (!enrollmentExists) return (false, "Enrollment not found for this beneficiary.", null);
            }

            var fb = new BeneficiaryFeedback
            {
                BeneficiaryId = vm.BeneficiaryId,
                EnrollmentId = vm.EnrollmentId,
                Category = vm.Category,
                Status = FeedbackStatus.New,
                Rating = vm.Rating,
                Subject = string.IsNullOrWhiteSpace(vm.Subject) ? null : vm.Subject.Trim(),
                Message = vm.Message.Trim(),
                SubmittedByAdmin = vm.SubmittedByAdmin,
                SubmittedByUserId = submittedByUserId,
                CreatedOnUtc = DateTime.UtcNow,
                CreatedByUserId = submittedByUserId,
                IsActive = true
            };

            _db.BeneficiaryFeedbacks.Add(fb);
            await _db.SaveChangesAsync(ct);

            return (true, null, fb.Id);
        }

        // ── Reply ────────────────────────────────────────────────────
        public async Task<(bool ok, string? error)> ReplyAsync(
            Guid id, string reply, FeedbackStatus newStatus,
            string repliedByUserId, CancellationToken ct)
        {
            var fb = await _db.BeneficiaryFeedbacks
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);

            if (fb == null) return (false, "Feedback not found.");

            fb.AdminReply = reply.Trim();
            fb.RepliedAt = DateTime.UtcNow;
            fb.RepliedByUserId = repliedByUserId;
            fb.Status = newStatus;
            fb.UpdatedOnUtc = DateTime.UtcNow;
            fb.UpdatedByUserId = repliedByUserId;

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        // ── Update status only ───────────────────────────────────────
        public async Task<(bool ok, string? error)> UpdateStatusAsync(
            Guid id, FeedbackStatus status, CancellationToken ct)
        {
            var fb = await _db.BeneficiaryFeedbacks
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);

            if (fb == null) return (false, "Feedback not found.");

            fb.Status = status;
            fb.UpdatedOnUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }
    }
}