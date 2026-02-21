using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public sealed class FormField : BaseEntity
    {
        public Guid FormSectionId { get; set; }
        public FormSection FormSection { get; set; } = null!;

        public string Label { get; set; } = "";
        public string? HelpText { get; set; }

        public FormFieldType FieldType { get; set; }

        public bool IsRequired { get; set; }
        public int SortOrder { get; set; } = 1;

        // Validation rules (stored as generic config)
        public int? MinInt { get; set; }
        public int? MaxInt { get; set; }
        public decimal? MinDecimal { get; set; }
        public decimal? MaxDecimal { get; set; }
        public int? MaxLength { get; set; }
        public string? RegexPattern { get; set; }

        // For advanced field types, store extra config as JSON
        // example: rating scale, file types, max file size, matrix columns, etc.
        public string? SettingsJson { get; set; }

        public bool IsActive { get; set; } = true;

        public List<FormFieldOption> Options { get; set; } = new();

        // Visibility rules: show/hide this field based on another field answer
        public List<FormFieldCondition> Conditions { get; set; } = new();
    }

    public sealed class FormFieldOption : BaseEntity
    {
        public Guid FormFieldId { get; set; }
        public FormField FormField { get; set; } = null!;

        public string Value { get; set; } = ""; // store stable value
        public string Text { get; set; } = "";  // what user sees
        public int SortOrder { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }

    public sealed class FormFieldCondition : BaseEntity
    {
        // Field being controlled
        public Guid TargetFieldId { get; set; }
        public FormField TargetField { get; set; } = null!;

        // Field being checked
        public Guid SourceFieldId { get; set; }
        public FormField SourceField { get; set; } = null!;

        public ConditionOperator Operator { get; set; }

        // Compare value stored as string
        public string? CompareValue { get; set; }

        // If true => show field when condition matches, else hide
        public bool ShowWhenMatched { get; set; } = true;

        public int SortOrder { get; set; } = 1;
    }
}
