using HWSETA_Impact_Hub.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Forms
{
    public sealed class FormSendBulkVm
    {
        [Required]
        public Guid TemplateId { get; set; }

        public bool SendEmail { get; set; } = true;
        public bool SendSms { get; set; } = true;

        // Filters
        public bool OnlyNotInvitedYet { get; set; }
        public bool OnlyMissingPasswordOrUser { get; set; }

        public BeneficiaryRegistrationStatus? Status { get; set; }
        public string? Programme { get; set; }
        public string? Provider { get; set; }
        public string? Employer { get; set; }
        public string? Province { get; set; }
        public string? Search { get; set; } // name, email, id, phone

        // For reporting
        public int? PreviewCount { get; set; }
    }
}
