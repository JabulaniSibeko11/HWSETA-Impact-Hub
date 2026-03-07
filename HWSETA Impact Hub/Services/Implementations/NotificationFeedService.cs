using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Notifications;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class NotificationFeedService : INotificationFeedService
    {
        private readonly ApplicationDbContext _db;

        public NotificationFeedService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<NotificationFeedVm> GetFeedAsync(
            NotificationFeedFilter filter,
            string? search,
            CancellationToken ct)
        {
            var vm = new NotificationFeedVm
            {
                Filter = filter,
                Search = search
            };

            var todayUtc = DateTime.UtcNow.Date;

            vm.StatusChangeCount = await _db.EnrollmentStatusHistories
                .AsNoTracking()
                .CountAsync(ct);

            vm.SubmissionCountToday = await _db.FormSubmissions
                .AsNoTracking()
                .CountAsync(x => x.SubmittedOnUtc >= todayUtc, ct);

            vm.InviteFailureCount = await _db.BeneficiaryFormInvites
                .AsNoTracking()
                .CountAsync(x => x.DeliveryStatus == InviteDeliveryStatus.Failed && x.IsActive, ct);

            vm.NewFeedbackCount = await _db.BeneficiaryFeedbacks
                .AsNoTracking()
                .CountAsync(x => x.IsActive && x.Status == FeedbackStatus.New, ct);

            var items = new List<NotificationFeedItemVm>();

            if (filter == NotificationFeedFilter.All || filter == NotificationFeedFilter.StatusChanges)
            {
                var statusItems = await _db.EnrollmentStatusHistories
                    .AsNoTracking()
                    .Include(x => x.Enrollment)
                        .ThenInclude(e => e.Beneficiary)
                    .Include(x => x.Enrollment)
                        .ThenInclude(e => e.Cohort)
                            .ThenInclude(c => c.Programme)
                    .OrderByDescending(x => x.StatusDate)
                    .Take(80)
                    .Select(x => new NotificationFeedItemVm
                    {
                        OccurredOnUtc = x.StatusDate,
                        Type = NotificationFeedFilter.StatusChanges,
                        Title = "Progress status changed",
                        Description = BuildStatusDescription(x.Status, x.Reason, x.Comment),
                        BeneficiaryName = x.Enrollment.Beneficiary.FirstName + " " + x.Enrollment.Beneficiary.LastName,
                        BeneficiaryIdentifier = x.Enrollment.Beneficiary.IdentifierValue,
                        ProgrammeName = x.Enrollment.Cohort.Programme.ProgrammeName,
                        CohortCode = x.Enrollment.Cohort.CohortCode,
                        BadgeText = x.Status.ToString(),
                        BadgeClass = GetStatusBadgeClass(x.Status),
                        IconClass = GetStatusIcon(x.Status),
                        IconWrapClass = GetStatusIconWrapClass(x.Status),
                        LinkText = "View enrolment",
                        LinkUrl = "/Enrollments/Details/" + x.EnrollmentId,
                        IsAlert = x.Status == EnrollmentStatus.DroppedOut
                    })
                    .ToListAsync(ct);

                items.AddRange(statusItems);
            }

            if (filter == NotificationFeedFilter.All || filter == NotificationFeedFilter.Submissions)
            {
                var submissionItems = await _db.FormSubmissions
                    .AsNoTracking()
                    .Include(x => x.FormPublish)
                        .ThenInclude(p => p.FormTemplate)
                    .OrderByDescending(x => x.SubmittedOnUtc)
                    .Take(80)
                    .Select(x => new
                    {
                        x.Id,
                        x.SubmittedOnUtc,
                        x.BeneficiaryId,
                        FormTitle = x.FormPublish.FormTemplate.Title
                    })
                    .ToListAsync(ct);

                if (submissionItems.Count > 0)
                {
                    var beneficiaryIds = submissionItems
                        .Where(x => x.BeneficiaryId.HasValue)
                        .Select(x => x.BeneficiaryId!.Value)
                        .Distinct()
                        .ToList();

                    var beneficiaries = await _db.Beneficiaries
                        .AsNoTracking()
                        .Where(x => beneficiaryIds.Contains(x.Id))
                        .Select(x => new
                        {
                            x.Id,
                            FullName = x.FirstName + " " + x.LastName,
                            x.IdentifierValue
                        })
                        .ToDictionaryAsync(x => x.Id, ct);

                    items.AddRange(submissionItems.Select(x =>
                    {
                        beneficiaries.TryGetValue(x.BeneficiaryId ?? Guid.Empty, out var ben);

                        return new NotificationFeedItemVm
                        {
                            OccurredOnUtc = x.SubmittedOnUtc,
                            Type = NotificationFeedFilter.Submissions,
                            Title = "Monitoring form submitted",
                            Description = $"A beneficiary submitted “{x.FormTitle}”.",
                            BeneficiaryName = ben?.FullName,
                            BeneficiaryIdentifier = ben?.IdentifierValue,
                            BadgeText = "Submitted",
                            BadgeClass = "nf-badge-success",
                            IconClass = "bi-ui-checks-grid",
                            IconWrapClass = "blue",
                            LinkText = "View submission",
                            LinkUrl = "/FormSubmissions/Details/" + x.Id,
                            IsAlert = false
                        };
                    }));
                }
            }

            if (filter == NotificationFeedFilter.All ||
                filter == NotificationFeedFilter.Invites ||
                filter == NotificationFeedFilter.Alerts)
            {
                var inviteItems = await _db.BeneficiaryFormInvites
                    .AsNoTracking()
                    .Include(x => x.Beneficiary)
                    .Include(x => x.FormPublish)
                        .ThenInclude(p => p.FormTemplate)
                    .OrderByDescending(x => x.LastAttemptAtUtc ?? x.SentAtUtc ?? x.CompletedAtUtc ?? x.CreatedOnUtc)
                    .Take(80)
                    .Select(x => new NotificationFeedItemVm
                    {
                        OccurredOnUtc = x.CompletedAtUtc
                                        ?? x.LastAttemptAtUtc
                                        ?? x.SentAtUtc
                                        ?? x.CreatedOnUtc,
                        Type = x.DeliveryStatus == InviteDeliveryStatus.Failed
                            ? NotificationFeedFilter.Alerts
                            : NotificationFeedFilter.Invites,
                        Title = x.CompletedAtUtc != null
                            ? "Invite completed"
                            : x.DeliveryStatus == InviteDeliveryStatus.Failed
                                ? "Invite delivery failed"
                                : x.DeliveryStatus == InviteDeliveryStatus.Sent
                                    ? "Invite sent"
                                    : "Invite pending",
                        Description = BuildInviteDescription(
                            x.FormPublish.FormTemplate.Title,
                            x.Channel,
                            x.DeliveryStatus,
                            x.LastError),
                        BeneficiaryName = x.Beneficiary.FirstName + " " + x.Beneficiary.LastName,
                        BeneficiaryIdentifier = x.Beneficiary.IdentifierValue,
                        BadgeText = x.CompletedAtUtc != null ? "Completed" : x.DeliveryStatus.ToString(),
                        BadgeClass = GetInviteBadgeClass(x.CompletedAtUtc, x.DeliveryStatus),
                        IconClass = GetInviteIcon(x.CompletedAtUtc, x.DeliveryStatus),
                        IconWrapClass = GetInviteIconWrapClass(x.CompletedAtUtc, x.DeliveryStatus),
                        LinkText = "Open send centre",
                        LinkUrl = "/FormTemplates/SendCenter",
                        IsAlert = x.DeliveryStatus == InviteDeliveryStatus.Failed
                    })
                    .ToListAsync(ct);

                items.AddRange(inviteItems);
            }

            if (filter == NotificationFeedFilter.All ||
                filter == NotificationFeedFilter.Feedback ||
                filter == NotificationFeedFilter.Alerts)
            {
                var feedbackItems = await _db.BeneficiaryFeedbacks
                    .AsNoTracking()
                    .Include(x => x.Beneficiary)
                    .OrderByDescending(x => x.CreatedOnUtc)
                    .Take(80)
                    .Select(x => new NotificationFeedItemVm
                    {
                        OccurredOnUtc = x.CreatedOnUtc,
                        Type = x.Status == FeedbackStatus.New
                            ? NotificationFeedFilter.Alerts
                            : NotificationFeedFilter.Feedback,
                        Title = x.Status == FeedbackStatus.New
                            ? "New beneficiary feedback"
                            : "Feedback activity",
                        Description = BuildFeedbackDescription(x.Category, x.Subject, x.Message),
                        BeneficiaryName = x.Beneficiary.FirstName + " " + x.Beneficiary.LastName,
                        BeneficiaryIdentifier = x.Beneficiary.IdentifierValue,
                        BadgeText = x.Status.ToString(),
                        BadgeClass = GetFeedbackBadgeClass(x.Status),
                        IconClass = GetFeedbackIcon(x.Status),
                        IconWrapClass = GetFeedbackIconWrapClass(x.Status),
                        LinkText = "View feedback",
                        LinkUrl = "/Feedback/Details/" + x.Id,
                        IsAlert = x.Status == FeedbackStatus.New
                    })
                    .ToListAsync(ct);

                items.AddRange(feedbackItems);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();

                items = items
                    .Where(x =>
                        (x.Title?.ToLowerInvariant().Contains(term) ?? false) ||
                        (x.Description?.ToLowerInvariant().Contains(term) ?? false) ||
                        (x.BeneficiaryName?.ToLowerInvariant().Contains(term) ?? false) ||
                        (x.BeneficiaryIdentifier?.ToLowerInvariant().Contains(term) ?? false) ||
                        (x.ProgrammeName?.ToLowerInvariant().Contains(term) ?? false) ||
                        (x.CohortCode?.ToLowerInvariant().Contains(term) ?? false) ||
                        (x.BadgeText?.ToLowerInvariant().Contains(term) ?? false))
                    .ToList();
            }

            vm.Items = items
                .OrderByDescending(x => x.OccurredOnUtc)
                .Take(120)
                .ToList();

            vm.TotalItems = vm.Items.Count;

            return vm;
        }

        private static string BuildStatusDescription(
            EnrollmentStatus status,
            string? reason,
            string? comment)
        {
            var text = status switch
            {
                EnrollmentStatus.Enrolled => "Beneficiary was marked as enrolled.",
                EnrollmentStatus.InTraining => "Beneficiary is currently in training.",
                EnrollmentStatus.DroppedOut => "Beneficiary was marked as dropped out.",
                EnrollmentStatus.Completed => "Beneficiary completed the programme.",
                _ => "Beneficiary progress was updated."
            };

            if (!string.IsNullOrWhiteSpace(reason))
                text += $" Reason: {reason.Trim()}.";

            if (!string.IsNullOrWhiteSpace(comment))
                text += $" Comment: {comment.Trim()}.";

            return text;
        }

        private static string BuildInviteDescription(
            string formTitle,
            InviteChannel channel,
            InviteDeliveryStatus status,
            string? lastError)
        {
            if (status == InviteDeliveryStatus.Failed)
            {
                return string.IsNullOrWhiteSpace(lastError)
                    ? $"Delivery failed for “{formTitle}” via {channel}."
                    : $"Delivery failed for “{formTitle}” via {channel}. Error: {lastError}";
            }

            if (status == InviteDeliveryStatus.Sent)
                return $"Invite for “{formTitle}” was sent via {channel}.";

            return $"Invite for “{formTitle}” is pending via {channel}.";
        }

        private static string BuildFeedbackDescription(
            FeedbackCategory category,
            string? subject,
            string message)
        {
            var summary = !string.IsNullOrWhiteSpace(subject) ? subject.Trim() : category.ToString();
            var body = message.Length > 120 ? message[..120] + "…" : message;
            return $"{summary}: {body}";
        }

        private static string GetStatusBadgeClass(EnrollmentStatus status) => status switch
        {
            EnrollmentStatus.Completed => "nf-badge-success",
            EnrollmentStatus.InTraining => "nf-badge-info",
            EnrollmentStatus.DroppedOut => "nf-badge-danger",
            _ => "nf-badge-warning"
        };

        private static string GetStatusIcon(EnrollmentStatus status) => status switch
        {
            EnrollmentStatus.Completed => "bi-patch-check-fill",
            EnrollmentStatus.InTraining => "bi-graph-up-arrow",
            EnrollmentStatus.DroppedOut => "bi-exclamation-triangle-fill",
            _ => "bi-person-check-fill"
        };

        private static string GetStatusIconWrapClass(EnrollmentStatus status) => status switch
        {
            EnrollmentStatus.Completed => "green",
            EnrollmentStatus.InTraining => "blue",
            EnrollmentStatus.DroppedOut => "red",
            _ => "gold"
        };

        private static string GetInviteBadgeClass(DateTime? completedAtUtc, InviteDeliveryStatus status)
        {
            if (completedAtUtc != null) return "nf-badge-success";

            return status switch
            {
                InviteDeliveryStatus.Sent => "nf-badge-info",
                InviteDeliveryStatus.Failed => "nf-badge-danger",
                _ => "nf-badge-warning"
            };
        }

        private static string GetInviteIcon(DateTime? completedAtUtc, InviteDeliveryStatus status)
        {
            if (completedAtUtc != null) return "bi-check2-circle";

            return status switch
            {
                InviteDeliveryStatus.Sent => "bi-send-check",
                InviteDeliveryStatus.Failed => "bi-envelope-x-fill",
                _ => "bi-hourglass-split"
            };
        }

        private static string GetInviteIconWrapClass(DateTime? completedAtUtc, InviteDeliveryStatus status)
        {
            if (completedAtUtc != null) return "green";

            return status switch
            {
                InviteDeliveryStatus.Sent => "blue",
                InviteDeliveryStatus.Failed => "red",
                _ => "gold"
            };
        }

        private static string GetFeedbackBadgeClass(FeedbackStatus status) => status switch
        {
            FeedbackStatus.New => "nf-badge-danger",
            FeedbackStatus.UnderReview => "nf-badge-warning",
            FeedbackStatus.Resolved => "nf-badge-success",
            FeedbackStatus.Closed => "nf-badge-muted",
            _ => "nf-badge-warning"
        };

        private static string GetFeedbackIcon(FeedbackStatus status) => status switch
        {
            FeedbackStatus.New => "bi-chat-left-dots-fill",
            FeedbackStatus.UnderReview => "bi-hourglass-split",
            FeedbackStatus.Resolved => "bi-check-circle-fill",
            FeedbackStatus.Closed => "bi-archive-fill",
            _ => "bi-chat-left-text-fill"
        };

        private static string GetFeedbackIconWrapClass(FeedbackStatus status) => status switch
        {
            FeedbackStatus.New => "red",
            FeedbackStatus.UnderReview => "gold",
            FeedbackStatus.Resolved => "green",
            FeedbackStatus.Closed => "muted",
            _ => "gold"
        };

        public async Task<TopbarNotificationVm> GetTopbarSummaryAsync(CancellationToken ct)
        {
            var vm = new TopbarNotificationVm();

            vm.NewFeedbackCount = await _db.BeneficiaryFeedbacks
                .AsNoTracking()
                .CountAsync(x => x.IsActive && x.Status == FeedbackStatus.New, ct);

            vm.InviteFailureCount = await _db.BeneficiaryFormInvites
                .AsNoTracking()
                .CountAsync(x => x.IsActive && x.DeliveryStatus == InviteDeliveryStatus.Failed, ct);

            var statusRows = await _db.EnrollmentStatusHistories
                .AsNoTracking()
                .Select(x => new
                {
                    x.EnrollmentId,
                    x.Status,
                    x.StatusDate,
                    x.Id
                })
                .ToListAsync(ct);

            vm.DroppedOutCount = statusRows
                .GroupBy(x => x.EnrollmentId)
                .Select(g => g
                    .OrderByDescending(x => x.StatusDate)
                    .ThenByDescending(x => x.Id)
                    .First())
                .Count(x => x.Status == EnrollmentStatus.DroppedOut);

            vm.UnresolvedCount =
                vm.NewFeedbackCount +
                vm.InviteFailureCount +
                vm.DroppedOutCount;

            return vm;
        }
    }
}
