namespace HWSETA_Impact_Hub.Models.ViewModels.Notifications
{
    public enum NotificationFeedFilter
    {
        All = 0,
        StatusChanges = 1,
        Submissions = 2,
        Invites = 3,
        Feedback = 4,
        Alerts = 5
    }

    public sealed class NotificationFeedItemVm
    {
        public DateTime OccurredOnUtc { get; set; }

        public NotificationFeedFilter Type { get; set; }

        public string Title { get; set; } = "";
        public string Description { get; set; } = "";

        public string? BeneficiaryName { get; set; }
        public string? BeneficiaryIdentifier { get; set; }
        public string? ProgrammeName { get; set; }
        public string? CohortCode { get; set; }

        public string BadgeText { get; set; } = "";
        public string BadgeClass { get; set; } = "";

        public string IconClass { get; set; } = "bi-bell";
        public string IconWrapClass { get; set; } = "green";

        public string? LinkText { get; set; }
        public string? LinkUrl { get; set; }

        public bool IsAlert { get; set; }
    }

    public sealed class NotificationFeedVm
    {
        public NotificationFeedFilter Filter { get; set; } = NotificationFeedFilter.All;
        public string? Search { get; set; }

        public int TotalItems { get; set; }
        public int StatusChangeCount { get; set; }
        public int SubmissionCountToday { get; set; }
        public int InviteFailureCount { get; set; }
        public int NewFeedbackCount { get; set; }

        public List<NotificationFeedItemVm> Items { get; set; } = new();
    }
}
