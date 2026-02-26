using HWSETA_Impact_Hub.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Forms
{
    public sealed class FormTemplateListRowVm
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public FormStatus Status { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOnUtc { get; set; }

        public bool IsPublished { get; set; } = false;

        public string PublicToken { get; set; }

        public FormPurpose Purpose { get; set; }
    }

    public sealed class FormTemplateCreateVm
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        [MaxLength(2000)]
        public string? Description { get; set; }

        public FormPurpose Purpose { get; set; } = FormPurpose.Tracking;
    }

    public sealed class FormTemplateEditVm
    {
        [Required]
        public Guid Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        [MaxLength(2000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // ---------------- Builder ----------------
    public sealed class FormTemplateBuilderVm
    {
        public Guid TemplateId { get; set; }
        public string Title { get; set; } = "";
        public FormStatus Status { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; }

        public List<FormSectionVm> Sections { get; set; } = new();
    }

    public sealed class FormSectionVm
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public List<FormFieldVm> Fields { get; set; } = new();
    }

    public sealed class FormFieldVm
    {
        public Guid Id { get; set; }
        public Guid FormSectionId { get; set; }
        public string Label { get; set; } = "";
        public string? HelpText { get; set; }
        public FormFieldType FieldType { get; set; }
        public bool IsRequired { get; set; }
        public int SortOrder { get; set; }

        public int? MinInt { get; set; }
        public int? MaxInt { get; set; }
        public decimal? MinDecimal { get; set; }
        public decimal? MaxDecimal { get; set; }
        public int? MaxLength { get; set; }
        public string? RegexPattern { get; set; }
        public string? SettingsJson { get; set; }

        public List<FormFieldOptionVm> Options { get; set; } = new();
    }

    public sealed class FormFieldOptionVm
    {
        public Guid Id { get; set; }
        public Guid FormFieldId { get; set; }
        public string Value { get; set; } = "";
        public string Text { get; set; } = "";
        public int SortOrder { get; set; }
    }

    // ---------------- Commands ----------------
    public sealed class FormSectionCreateVm
    {
        [Required] public Guid FormTemplateId { get; set; }
        [Required, MaxLength(200)] public string Title { get; set; } = "";
        [MaxLength(2000)] public string? Description { get; set; }
    }

    public sealed class FormSectionEditVm
    {
        [Required] public Guid Id { get; set; }
        [Required, MaxLength(200)] public string Title { get; set; } = "";
        [MaxLength(2000)] public string? Description { get; set; }
    }

    public  class FormFieldCreateVm
    {
        [Required] public Guid FormSectionId { get; set; }
        [Required, MaxLength(300)] public string Label { get; set; } = "";
        [MaxLength(2000)] public string? HelpText { get; set; }
        [Required] public FormFieldType FieldType { get; set; }
        public bool IsRequired { get; set; }

        public int? MinInt { get; set; }
        public int? MaxInt { get; set; }
        public decimal? MinDecimal { get; set; }
        public decimal? MaxDecimal { get; set; }
        public int? MaxLength { get; set; }
        public string? RegexPattern { get; set; }
        public string? SettingsJson { get; set; }
    }

    public sealed class FormFieldEditVm : FormFieldCreateVm
    {
        [Required] public Guid Id { get; set; }
    }

    public  class FormFieldOptionCreateVm
    {
        [Required] public Guid FormFieldId { get; set; }
        [MaxLength(200)] public string? Value { get; set; }
        [Required, MaxLength(300)] public string Text { get; set; } = "";
    }

    public sealed class FormFieldOptionEditVm : FormFieldOptionCreateVm
    {
        [Required] public Guid Id { get; set; }
    }


}
