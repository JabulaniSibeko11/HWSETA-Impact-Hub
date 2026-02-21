using HWSETA_Impact_Hub.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public sealed class FormTemplate : BaseEntity
    {
        public string Title { get; set; } = "";
        public string? Description { get; set; }

        public FormStatus Status { get; set; } = FormStatus.Draft;

        // Versioning: if you edit a published form, you can clone to a new version later
        public int Version { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public List<FormSection> Sections { get; set; } = new();
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
