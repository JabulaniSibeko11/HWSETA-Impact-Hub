using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Reporting
{
    public sealed class CohortDeliveryReportFiltersVm
    {
        public Guid? CohortId { get; set; }
        public Guid? ProgrammeId { get; set; }
        public Guid? ProviderId { get; set; }
        public Guid? FundingTypeId { get; set; }

        [Range(2000, 2100)]
        public int? IntakeYear { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartFrom { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartTo { get; set; }

        public List<SelectListItem> Cohorts { get; set; } = new();
        public List<SelectListItem> Programmes { get; set; } = new();
        public List<SelectListItem> Providers { get; set; } = new();
        public List<SelectListItem> FundingTypes { get; set; } = new();
    }

    public sealed class CohortDeliveryKpisVm
    {
        public int TotalCohorts { get; set; }
        public int TotalEnrollments { get; set; }

        public int ActiveEnrollments { get; set; }
        public int CompletedEnrollments { get; set; }
        public int DroppedOutEnrollments { get; set; }

        public decimal CompletionRatePct { get; set; }
        public decimal DropoutRatePct { get; set; }
    }

    public sealed class CohortDeliveryRowVm
    {
        public Guid CohortId { get; set; }
        public string CohortCode { get; set; } = "";

        public int IntakeYear { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime PlannedEndDate { get; set; }

        public string ProgrammeName { get; set; } = "";
        public string QualificationType { get; set; } = "";
        public string ProviderName { get; set; } = "";
        public string FundingType { get; set; } = "";
        public string? EmployerCode { get; set; }

        public int Total { get; set; }
        public int Active { get; set; }
        public int Completed { get; set; }
        public int DroppedOut { get; set; }
        public decimal CompletionRatePct { get; set; }
        public decimal DropoutRatePct { get; set; }
    }

    public sealed class CohortDeliveryReportVm
    {
        public CohortDeliveryReportFiltersVm Filters { get; set; } = new();
        public CohortDeliveryKpisVm Kpis { get; set; } = new();
        public List<CohortDeliveryRowVm> Rows { get; set; } = new();
    }
}
