namespace HWSETA_Impact_Hub.Models.ViewModels.Forms
{
    public sealed class FormSubmissionRowVm
    {
        public Guid SubmissionId { get; set; }
        public DateTime SubmittedOnUtc { get; set; }

        public string? SubmittedByUserId { get; set; }
        public Guid? BeneficiaryId { get; set; }

        public string? IpAddress { get; set; }
    }

    public sealed class FormSubmissionsListVm
    {
        public Guid TemplateId { get; set; }
        public string Title { get; set; } = "";
        public string Token { get; set; } = "";

        public int Total { get; set; }
        public List<FormSubmissionRowVm> Rows { get; set; } = new();
    }

    public sealed class FormSubmissionAnswerVm
    {
        public string SectionTitle { get; set; } = "";
        public string QuestionLabel { get; set; } = "";
        public string? Value { get; set; }
    }

    public sealed class FormSubmissionDetailsVm
    {
        public Guid SubmissionId { get; set; }
        public string FormTitle { get; set; } = "";
        public DateTime SubmittedOnUtc { get; set; }

        public List<FormSubmissionAnswerVm> Answers { get; set; } = new();
    }
}
