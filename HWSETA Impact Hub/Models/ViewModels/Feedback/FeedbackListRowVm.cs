using HWSETA_Impact_Hub.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Feedback
{
    // ── List row ─────────────────────────────────────────────────────
    public sealed class FeedbackListRowVm
    {
        public Guid Id { get; set; }
        public string BeneficiaryName { get; set; } = "";
        public string BeneficiaryId_ { get; set; } = "";  // display ID/passport
        public FeedbackCategory Category { get; set; }
        public FeedbackStatus Status { get; set; }
        public int? Rating { get; set; }
        public string? Subject { get; set; }
        public string Message { get; set; } = "";
        public bool HasReply { get; set; }
        public bool SubmittedByAdmin { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }

    // ── Create (admin submits on behalf of / records beneficiary feedback) ──
    public sealed class FeedbackCreateVm
    {
        [Required]
        [Display(Name = "Beneficiary")]
        public Guid BeneficiaryId { get; set; }

        [Display(Name = "Enrollment (optional)")]
        public Guid? EnrollmentId { get; set; }

        [Required]
        [Display(Name = "Category")]
        public FeedbackCategory Category { get; set; } = FeedbackCategory.General;

        [Display(Name = "Star Rating (1–5)")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int? Rating { get; set; }

        [MaxLength(300)]
        [Display(Name = "Subject")]
        public string? Subject { get; set; }

        [Required, MaxLength(4000)]
        [Display(Name = "Feedback / Comment")]
        public string Message { get; set; } = "";

        [Display(Name = "Submitted by Admin")]
        public bool SubmittedByAdmin { get; set; } = true;

        // Dropdowns
        public List<SelectListItem> Beneficiaries { get; set; } = new();
        public List<SelectListItem> Enrollments { get; set; } = new();
    }

    // ── Details + reply ──────────────────────────────────────────────
    public sealed class FeedbackDetailsVm
    {
        public Guid Id { get; set; }
        public Guid BeneficiaryId { get; set; }
        public string BeneficiaryName { get; set; } = "";
        public string BeneficiaryIdentifier { get; set; } = "";
        public string? BeneficiaryEmail { get; set; }
        public string? BeneficiaryMobile { get; set; }

        public Guid? EnrollmentId { get; set; }
        public string? Programme { get; set; }

        public FeedbackCategory Category { get; set; }
        public FeedbackStatus Status { get; set; }
        public int? Rating { get; set; }
        public string? Subject { get; set; }
        public string Message { get; set; } = "";
        public bool SubmittedByAdmin { get; set; }
        public DateTime CreatedOnUtc { get; set; }

        // Admin reply section
        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }
        public string? RepliedByEmail { get; set; }

        // Form fields for updating
        [MaxLength(4000)]
        [Display(Name = "Reply")]
        public string? NewReply { get; set; }

        [Display(Name = "Status")]
        public FeedbackStatus NewStatus { get; set; }
    }

    // ── Index (list + filters) ────────────────────────────────────────
    public sealed class FeedbackIndexVm
    {
        public List<FeedbackListRowVm> Rows { get; set; } = new();

        // Filter state
        public FeedbackStatus? FilterStatus { get; set; }
        public FeedbackCategory? FilterCategory { get; set; }
        public string? FilterSearch { get; set; }

        // Totals for header badges
        public int TotalNew { get; set; }
        public int TotalUnderReview { get; set; }
        public int TotalResolved { get; set; }
        public int TotalClosed { get; set; }
    }
}
