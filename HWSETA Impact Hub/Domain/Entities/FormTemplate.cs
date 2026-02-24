using HWSETA_Impact_Hub.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public enum FormPurpose
    {
        Registration = 1,   // compulsory one
        Tracking = 2,       // beneficiary changes over time
        Challenges = 3,     // challenges during training
        Outcomes = 4        // post-training outcomes
    }

    public sealed class FormTemplate : BaseEntity
    {
        public string Title { get; set; } = "";
        public string? Description { get; set; }

        public FormStatus Status { get; set; } = FormStatus.Draft;

        // NEW: classify the template
        public FormPurpose Purpose { get; set; } = FormPurpose.Tracking;

        // Versioning
        public int Version { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        // NEW: simple auditing
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public List<FormSection> Sections { get; set; } = new();

        // Convenience computed flag (NOT mapped)
        public bool IsPublished => Status == FormStatus.Published;

        public string PublicToken { get; set; } = "";   // used in /f/{token}
        public DateTime? PublishedAt { get; set; }
        public DateTime? UnpublishedAt { get; set; }

        public DateTime? OpenFromUtc { get; set; }
        public DateTime? OpenToUtc { get; set; }
    }

    public sealed class FormSection : BaseEntity
    {
        public Guid FormTemplateId { get; set; }
        public FormTemplate FormTemplate { get; set; } = null!;

        public string Title { get; set; } = "";
        public string? Description { get; set; }

        public int SortOrder { get; set; } = 1;

        public List<FormField> Fields { get; set; } = new();
    }
}
