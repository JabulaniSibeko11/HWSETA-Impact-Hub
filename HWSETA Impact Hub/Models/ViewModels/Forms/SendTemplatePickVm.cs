using HWSETA_Impact_Hub.Domain.Entities;

namespace HWSETA_Impact_Hub.Models.ViewModels.Forms
{
    public sealed class SendTemplatePickVm
    {
        public Guid TemplateId { get; set; }
        public string Title { get; set; } = "";
        public FormPurpose Purpose { get; set; }
    }

    public sealed class SendCenterRowVm
    {
        public Guid BeneficiaryId { get; set; }
        public string FullName { get; set; } = "";
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? Province { get; set; }
        public string? Programme { get; set; }
        public string? TrainingProvider { get; set; }
        public string? Employer { get; set; }
        public BeneficiaryRegistrationStatus Status { get; set; }
        public DateTime? InvitedAt { get; set; }
    }

    public sealed class SendCenterVm
    {
        // dropdown for published templates
        public List<SendTemplatePickVm> Templates { get; set; } = new();

        // selected template to send
        public Guid SelectedTemplateId { get; set; }

        // filters
        public FormSendBulkVm Filters { get; set; } = new();

        // preview
        public int PreviewTotal { get; set; }
        public List<SendCenterRowVm> PreviewRows { get; set; } = new();
    }
}
