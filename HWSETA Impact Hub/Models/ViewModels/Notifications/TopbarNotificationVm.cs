namespace HWSETA_Impact_Hub.Models.ViewModels.Notifications
{
    public sealed class TopbarNotificationVm
    {
        public int UnresolvedCount { get; set; }

        public int NewFeedbackCount { get; set; }
        public int InviteFailureCount { get; set; }
        public int DroppedOutCount { get; set; }

        public bool HasItems => UnresolvedCount > 0;
        public string BadgeText => UnresolvedCount > 99 ? "99+" : UnresolvedCount.ToString();
    }
}
