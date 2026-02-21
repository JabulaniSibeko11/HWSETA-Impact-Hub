using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Forms
{
    public sealed class FormPublishVm
    {
        [Required]
        public Guid TemplateId { get; set; }

        public string Title { get; set; } = "";

        public bool IsPublished { get; set; }
        public string? Token { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime OpenFromUtc { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CloseAtUtc { get; set; }

        public int? MaxSubmissions { get; set; }
        public bool AllowMultipleSubmissions { get; set; } = true;

        // convenience
        public string? PublicUrl { get; set; }
    }

    // -------- Public render --------
    public sealed class PublicFormVm
    {
        public string Token { get; set; } = "";
        public Guid TemplateId { get; set; }

        public string Title { get; set; } = "";
        public string? Description { get; set; }

        public bool IsOpen { get; set; }
        public string? ClosedReason { get; set; }

        public List<PublicSectionVm> Sections { get; set; } = new();

        // Captured at submit time
        public string? PrefillEmail { get; set; }
        public string? PrefillPhone { get; set; }
    }

    public sealed class PublicSectionVm
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public List<PublicQuestionVm> Questions { get; set; } = new();
    }

    public sealed class PublicQuestionVm
    {
        public Guid FieldId { get; set; }
        public string Label { get; set; } = "";
        public string? HelpText { get; set; }

        public int FieldType { get; set; }           // store enum int to avoid extra refs in view
        public bool IsRequired { get; set; }
        public int SortOrder { get; set; }

        public int? MaxLength { get; set; }
        public int? MinInt { get; set; }
        public int? MaxInt { get; set; }
        public decimal? MinDecimal { get; set; }
        public decimal? MaxDecimal { get; set; }
        public string? RegexPattern { get; set; }

        public List<PublicOptionVm> Options { get; set; } = new();
    }

    public sealed class PublicOptionVm
    {
        public string Value { get; set; } = "";
        public string Text { get; set; } = "";
    }

    // -------- Public submit payload --------
    public sealed class PublicFormSubmitVm
    {
        [Required]
        public string Token { get; set; } = "";

        // Dynamic answers: FieldId -> string value (single value)
        public Dictionary<Guid, string?> Answers { get; set; } = new();

        // Checkboxes: FieldId -> list of selected values
        public Dictionary<Guid, List<string>> MultiAnswers { get; set; } = new();

        // optional capture (if you want)
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

}
