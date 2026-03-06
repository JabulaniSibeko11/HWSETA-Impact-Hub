namespace HWSETA_Impact_Hub.Models.ViewModels.Dashboard
{
    public sealed class DashboardVm
    {
        // ── KPI header cards ─────────────────────────────────────────
        public int TotalBeneficiaries { get; set; }
        public int ActiveBeneficiaries { get; set; }
        public int TotalEnrollments { get; set; }
        public int CompletedEnrollments { get; set; }
        public int DroppedOutEnrollments { get; set; }
        public int ActiveEnrollments { get; set; }
        public int NewFeedbackCount { get; set; }
        public int TotalCohorts { get; set; }
        public decimal CompletionRatePct { get; set; }

        // ── Chart 1: Enrollment Status Doughnut ──────────────────────
        // Labels: ["Enrolled","In Training","Completed","Dropped Out"]
        public string EnrollmentStatusLabelsJson { get; set; } = "[]";
        public string EnrollmentStatusDataJson { get; set; } = "[]";

        // ── Chart 2: Registration Pipeline Doughnut ──────────────────
        // Labels: ["Added","Invite Sent","Password Set","Location","Submitted","Completed"]
        public string RegistrationLabelsJson { get; set; } = "[]";
        public string RegistrationDataJson { get; set; } = "[]";

        // ── Chart 3: Monthly Enrolment Trend Line ────────────────────
        // Last 12 calendar months
        public string MonthLabelsJson { get; set; } = "[]";
        public string MonthEnrolledJson { get; set; } = "[]";
        public string MonthCompletedJson { get; set; } = "[]";

        // ── Chart 4: Top Programmes Bar ──────────────────────────────
        public string ProgrammeLabelsJson { get; set; } = "[]";
        public string ProgrammeDataJson { get; set; } = "[]";

        // ── Chart 5: Beneficiaries by Province Bar ───────────────────
        public string ProvinceLabelsJson { get; set; } = "[]";
        public string ProvinceDataJson { get; set; } = "[]";
    }
}