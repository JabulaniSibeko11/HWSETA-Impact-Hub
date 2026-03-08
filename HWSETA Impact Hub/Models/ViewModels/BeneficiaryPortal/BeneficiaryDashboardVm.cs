namespace HWSETA_Impact_Hub.Models.ViewModels.BeneficiaryPortal
{
    public sealed class BeneficiaryDashboardVm
    {
        public Guid BeneficiaryId { get; set; }

        public string BeneficiaryName { get; set; } = "";
        public string RegistrationStatus { get; set; } = "";
        public bool IsActive { get; set; }

        public int PendingFormsCount { get; set; }
        public int UnreadMessagesCount { get; set; }
        public int ActiveEnrolmentsCount { get; set; }
        public int PendingActionsCount { get; set; }

        public List<BeneficiaryDashboardFormVm> RecentForms { get; set; } = new();
        public List<BeneficiaryDashboardMessageVm> RecentMessages { get; set; } = new();
        public List<BeneficiaryDashboardEnrollmentVm> ActiveEnrollments { get; set; } = new();
        public List<BeneficiaryDashboardActivityVm> RecentActivity { get; set; } = new();
    }

    public sealed class BeneficiaryDashboardFormVm
    {
        public Guid InviteId { get; set; }
        public string FormTitle { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime? SentOnUtc { get; set; }
        public DateTime? ExpiryOnUtc { get; set; }
    }

    public sealed class BeneficiaryDashboardMessageVm
    {
        public Guid ThreadId { get; set; }
        public string Subject { get; set; } = "";
        public string LastSender { get; set; } = "";
        public string LastMessagePreview { get; set; } = "";
        public DateTime LastMessageOnUtc { get; set; }
        public bool IsUnread { get; set; }
    }

    public sealed class BeneficiaryDashboardEnrollmentVm
    {
        public Guid EnrollmentId { get; set; }
        public string ProgrammeName { get; set; } = "";
        public string CohortCode { get; set; } = "";
        public string ProviderName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime StartDate { get; set; }
    }

    public sealed class BeneficiaryDashboardActivityVm
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime OccurredOnUtc { get; set; }
    }
}
