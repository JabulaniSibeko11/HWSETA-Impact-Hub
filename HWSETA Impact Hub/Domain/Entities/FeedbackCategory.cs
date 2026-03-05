using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public enum FeedbackCategory
    {
        General = 1,
        ProgrammeQuality = 2,
        Support = 3,
        Facilities = 4,
        Complaint = 5,
        Compliment = 6
    }

    public enum FeedbackStatus
    {
        New = 1,
        UnderReview = 2,
        Resolved = 3,
        Closed = 4
    }

    public sealed class BeneficiaryFeedback : BaseEntity
    {
        // Who submitted
        public Guid BeneficiaryId { get; set; }
        public Beneficiary Beneficiary { get; set; } = null!;

        // Optional: link to a specific enrollment
        public Guid? EnrollmentId { get; set; }
        public Enrollment? Enrollment { get; set; }

        public FeedbackCategory Category { get; set; } = FeedbackCategory.General;
        public FeedbackStatus Status { get; set; } = FeedbackStatus.New;

        // 1–5 star rating (nullable — not all feedback needs a rating)
        public int? Rating { get; set; }

        // Subject line (optional summary)
        public string? Subject { get; set; }

        // Main feedback body
        public string Message { get; set; } = "";

        // Admin response
        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }
        public string? RepliedByUserId { get; set; }

        // Submitted by (admin on behalf of beneficiary, or beneficiary themselves)
        public bool SubmittedByAdmin { get; set; } = false;
        public string? SubmittedByUserId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
