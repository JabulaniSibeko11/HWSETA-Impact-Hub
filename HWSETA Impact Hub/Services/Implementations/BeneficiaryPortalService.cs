using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.BeneficiaryPortal;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class BeneficiaryPortalService : IBeneficiaryPortalService
    {
        private readonly ApplicationDbContext _db;

        public BeneficiaryPortalService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<BeneficiaryDashboardVm?> GetDashboardAsync(string currentUserId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
                return null;

            var beneficiary = await _db.Beneficiaries
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == currentUserId, ct);

            if (beneficiary == null)
                return null;

            var invites = await _db.BeneficiaryFormInvites
                .AsNoTracking()
                .Include(x => x.FormPublish)
                    .ThenInclude(x => x.FormTemplate)
                .Where(x => x.BeneficiaryId == beneficiary.Id)
                .OrderByDescending(x => x.SentOnUtc)
                .ToListAsync(ct);

            var threads = await _db.ConversationThreads
                .AsNoTracking()
                .Include(x => x.Messages)
                .Where(x => x.BeneficiaryId == beneficiary.Id)
                .OrderByDescending(x => x.LastMessageOnUtc)
                .ToListAsync(ct);

            var enrollments = await _db.Enrollments
                .AsNoTracking()
                .Include(x => x.Cohort)
                    .ThenInclude(c => c.Programme)
                .Include(x => x.Cohort)
                    .ThenInclude(c => c.Provider)
                .Where(x => x.BeneficiaryId == beneficiary.Id)
                .OrderByDescending(x => x.StartDate)
                .ToListAsync(ct);

            var pendingForms = invites.Count(x =>
                !string.Equals(x.Status, "Submitted", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(x.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
                (!x.ExpiryOnUtc.HasValue || x.ExpiryOnUtc.Value >= DateTime.UtcNow));

            var unreadMessages = threads.Count(x => x.HasUnreadBeneficiaryMessage);

            var activeEnrollmentCount = enrollments.Count(x =>
                x.CurrentStatus == EnrollmentStatus.Enrolled);

            var vm = new BeneficiaryDashboardVm
            {
                BeneficiaryId = beneficiary.Id,
                BeneficiaryName = $"{beneficiary.FirstName ?? ""} {beneficiary.LastName ?? ""}".Trim(),
                RegistrationStatus = beneficiary.RegistrationStatus.ToString(),
                IsActive = beneficiary.IsActive,

                PendingFormsCount = pendingForms,
                UnreadMessagesCount = unreadMessages,
                ActiveEnrolmentsCount = activeEnrollmentCount,
                PendingActionsCount = pendingForms + unreadMessages,

                RecentForms = invites
                    .Take(5)
                    .Select(x => new BeneficiaryDashboardFormVm
                    {
                        InviteId = x.Id,
                        FormTitle = x.FormPublish?.FormTemplate?.Title ?? "Form",
                        Status = x.Status ?? "Sent",
                        SentOnUtc = x.SentOnUtc,
                        ExpiryOnUtc = x.ExpiryOnUtc
                    })
                    .ToList(),

                RecentMessages = threads
                    .Take(5)
                    .Select(x =>
                    {
                        var last = x.Messages
                            .OrderByDescending(m => m.SentOnUtc)
                            .FirstOrDefault();

                        return new BeneficiaryDashboardMessageVm
                        {
                            ThreadId = x.Id,
                            Subject = x.Subject,
                            LastSender = last?.SenderDisplayName ?? "",
                            LastMessagePreview = last?.MessageText ?? "",
                            LastMessageOnUtc = x.LastMessageOnUtc,
                            IsUnread = x.HasUnreadBeneficiaryMessage
                        };
                    })
                    .ToList(),

                ActiveEnrollments = enrollments
                    .Take(5)
                    .Select(x => new BeneficiaryDashboardEnrollmentVm
                    {
                        EnrollmentId = x.Id,
                        ProgrammeName = x.Cohort?.Programme?.ProgrammeName ?? "",
                        CohortCode = x.Cohort?.CohortCode ?? "",
                        ProviderName = x.Cohort?.Provider?.ProviderName ?? "",
                        Status = x.CurrentStatus.ToString(),
                        StartDate = x.StartDate
                    })
                    .ToList(),

                RecentActivity = BuildActivity(invites, threads, enrollments)
                    .OrderByDescending(x => x.OccurredOnUtc)
                    .Take(8)
                    .ToList()
            };

            return vm;
        }

        private static List<BeneficiaryDashboardActivityVm> BuildActivity(
            List<BeneficiaryFormInvite> invites,
            List<ConversationThread> threads,
            List<Enrollment> enrollments)
        {
            var items = new List<BeneficiaryDashboardActivityVm>();

            items.AddRange(invites.Select(x => new BeneficiaryDashboardActivityVm
            {
                Title = "Form Assigned",
                Description = x.FormPublish?.FormTemplate?.Title ?? "A form was assigned to you.",
                OccurredOnUtc = x.SentOnUtc ?? x.CreatedOnUtc
            }));

            items.AddRange(threads.Select(x => new BeneficiaryDashboardActivityVm
            {
                Title = "Conversation Updated",
                Description = x.Subject,
                OccurredOnUtc = x.LastMessageOnUtc
            }));

            items.AddRange(enrollments.Select(x => new BeneficiaryDashboardActivityVm
            {
                Title = "Enrolment Active",
                Description = $"{x.Cohort?.Programme?.ProgrammeName ?? "Programme"} - {x.CurrentStatus}",
                OccurredOnUtc = x.CreatedOnUtc
            }));

            return items;
        }
    }
}