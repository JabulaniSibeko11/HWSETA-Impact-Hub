using HWSETA_Impact_Hub.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Forms
{
    public sealed class FormSendBulkVm
    {
        public Guid TemplateId { get; set; }

        public BeneficiaryRegistrationStatus? Status { get; set; }

        // ✅ Use GUID lookups (not strings)
        public Guid? ProvinceId { get; set; }
        public Guid? QualificationTypeId { get; set; }   // optional
        public Guid? ProgrammeId { get; set; }
        public Guid? ProviderId { get; set; }
        public Guid? EmployerId { get; set; }

        public string? Search { get; set; }

        public bool OnlyNotInvitedYet { get; set; }
        public bool OnlyMissingPasswordOrUser { get; set; }

        public bool SendEmail { get; set; } = true;
        public bool SendSms { get; set; } = true;

        // ✅ Dropdown lists
        public List<SelectListItem> Provinces { get; set; } = new();
        public List<SelectListItem> QualificationTypes { get; set; } = new();
        public List<SelectListItem> Programmes { get; set; } = new();
        public List<SelectListItem> Providers { get; set; } = new();
        public List<SelectListItem> Employers { get; set; } = new();

       
        public string? Programme { get; set; }
        public string? Provider { get; set; }
        public string? Employer { get; set; }
        public string? Province { get; set; }
      

        // For reporting
        public int? PreviewCount { get; set; }
    }
}
