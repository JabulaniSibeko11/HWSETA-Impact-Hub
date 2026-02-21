using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public sealed class FormPublish : BaseEntity
    {
        public Guid FormTemplateId { get; set; }
        public FormTemplate FormTemplate { get; set; } = null!;

        // public link token
        public string PublicToken { get; set; } = Guid.NewGuid().ToString("N");

        public DateTime OpenFromUtc { get; set; } = DateTime.UtcNow;
        public DateTime? OpenToUtc { get; set; }

        public bool IsOpen { get; set; } = true;

        // Optional targeting (future)
        public Guid? CohortId { get; set; }
        public Guid? BeneficiaryId { get; set; }
    }

    public sealed class FormSubmission : BaseEntity
    {
        public Guid FormPublishId { get; set; }
        public FormPublish FormPublish { get; set; } = null!;

        // if logged in (beneficiary or admin), store identity
        public string? SubmittedByUserId { get; set; }

        // optional link to beneficiary
        public Guid? BeneficiaryId { get; set; }

        public DateTime SubmittedOnUtc { get; set; } = DateTime.UtcNow;

        // GPS (we’ll hook later)
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public List<FormAnswer> Answers { get; set; } = new();
    }

    public sealed class FormAnswer : BaseEntity
    {
        public Guid FormSubmissionId { get; set; }
        public FormSubmission FormSubmission { get; set; } = null!;

        public Guid FormFieldId { get; set; }
        public FormField FormField { get; set; } = null!;

        // store raw answer as string; for checkboxes/multi select store JSON array
        public string? Value { get; set; }
        public string? ValueJson { get; set; }
    }
}
