using ClosedXML.Excel;
using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Reporting;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class ReportingService : IReportingService
    {
        private readonly ApplicationDbContext _db;

        public ReportingService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<CohortDeliveryReportVm> BuildCohortDeliveryAsync(CohortDeliveryReportFiltersVm filters, CancellationToken ct)
        {
            // Dropdowns
            filters.Programmes = await _db.Programmes.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.ProgrammeName)
                .Select(x => new SelectListItem(x.ProgrammeName, x.Id.ToString()))
                .ToListAsync(ct);

            filters.Providers = await _db.Providers.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.ProviderName)
                .Select(x => new SelectListItem(x.ProviderName, x.Id.ToString()))
                .ToListAsync(ct);

            filters.FundingTypes = await _db.FundingTypes.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToListAsync(ct);

            filters.Cohorts = await _db.Cohorts.AsNoTracking()
                .OrderByDescending(x => x.StartDate)
                .Select(x => new SelectListItem($"{x.CohortCode} ({x.IntakeYear})", x.Id.ToString()))
                .ToListAsync(ct);

            // Base query for cohorts (filters)
            var cohortsQ = _db.Cohorts.AsNoTracking()
                .Include(x => x.Programme).ThenInclude(p => p.QualificationType)
                .Include(x => x.Provider)
                .Include(x => x.Employer)
                .Include(x => x.FundingType)
                .AsQueryable();

            if (filters.CohortId.HasValue) cohortsQ = cohortsQ.Where(x => x.Id == filters.CohortId.Value);
            if (filters.ProgrammeId.HasValue) cohortsQ = cohortsQ.Where(x => x.ProgrammeId == filters.ProgrammeId.Value);
            if (filters.ProviderId.HasValue) cohortsQ = cohortsQ.Where(x => x.ProviderId == filters.ProviderId.Value);
            if (filters.FundingTypeId.HasValue) cohortsQ = cohortsQ.Where(x => x.FundingTypeId == filters.FundingTypeId.Value);
            if (filters.IntakeYear.HasValue) cohortsQ = cohortsQ.Where(x => x.IntakeYear == filters.IntakeYear.Value);

            if (filters.StartFrom.HasValue) cohortsQ = cohortsQ.Where(x => x.StartDate >= filters.StartFrom.Value.Date);
            if (filters.StartTo.HasValue) cohortsQ = cohortsQ.Where(x => x.StartDate <= filters.StartTo.Value.Date);

            // Pull cohort IDs first
            var cohortIds = await cohortsQ.Select(x => x.Id).ToListAsync(ct);

            // Enrollment query limited to selected cohorts
            var enrQ = _db.Enrollments.AsNoTracking()
                .Where(x => cohortIds.Contains(x.CohortId));

            // KPIs (enrollments)
            var totalEnrollments = await enrQ.CountAsync(ct);
            var completed = await enrQ.CountAsync(x => x.CurrentStatus == EnrollmentStatus.Completed, ct);
            var dropped = await enrQ.CountAsync(x => x.CurrentStatus == EnrollmentStatus.DroppedOut, ct);
            var active = await enrQ.CountAsync(x =>
                x.IsActive && x.CurrentStatus != EnrollmentStatus.Completed && x.CurrentStatus != EnrollmentStatus.DroppedOut, ct);

            // Cohort count
            var totalCohorts = cohortIds.Count;

            decimal SafePct(int num, int den) => den <= 0 ? 0 : Math.Round((decimal)num * 100m / (decimal)den, 2);

            // Rows per cohort (aggregate enrollments by cohort)
            var enrAgg = await enrQ
                .GroupBy(x => x.CohortId)
                .Select(g => new
                {
                    CohortId = g.Key,
                    Total = g.Count(),
                    Completed = g.Count(x => x.CurrentStatus == EnrollmentStatus.Completed),
                    Dropped = g.Count(x => x.CurrentStatus == EnrollmentStatus.DroppedOut),
                    Active = g.Count(x => x.IsActive && x.CurrentStatus != EnrollmentStatus.Completed && x.CurrentStatus != EnrollmentStatus.DroppedOut)
                })
                .ToListAsync(ct);

            var enrAggByCohort = enrAgg.ToDictionary(x => x.CohortId, x => x);

            var cohorts = await cohortsQ
                .OrderByDescending(x => x.StartDate)
                .ToListAsync(ct);

            var rows = cohorts.Select(c =>
            {
                enrAggByCohort.TryGetValue(c.Id, out var agg);

                var tot = agg?.Total ?? 0;
                var comp = agg?.Completed ?? 0;
                var drop = agg?.Dropped ?? 0;
                var act = agg?.Active ?? 0;

                return new CohortDeliveryRowVm
                {
                    CohortId = c.Id,
                    CohortCode = c.CohortCode,
                    IntakeYear = c.IntakeYear,
                    StartDate = c.StartDate,
                    PlannedEndDate = c.PlannedEndDate,

                    ProgrammeName = c.Programme?.ProgrammeName ?? "",
                    QualificationType = c.Programme?.QualificationType?.Name ?? "",
                    ProviderName = c.Provider?.ProviderName ?? "",
                    FundingType = c.FundingType?.Name ?? "",
                    EmployerCode = c.Employer?.EmployerCode,

                    Total = tot,
                    Active = act,
                    Completed = comp,
                    DroppedOut = drop,
                    CompletionRatePct = SafePct(comp, tot),
                    DropoutRatePct = SafePct(drop, tot)
                };
            }).ToList();

            var vm = new CohortDeliveryReportVm
            {
                Filters = filters,
                Kpis = new CohortDeliveryKpisVm
                {
                    TotalCohorts = totalCohorts,
                    TotalEnrollments = totalEnrollments,
                    ActiveEnrollments = active,
                    CompletedEnrollments = completed,
                    DroppedOutEnrollments = dropped,
                    CompletionRatePct = SafePct(completed, totalEnrollments),
                    DropoutRatePct = SafePct(dropped, totalEnrollments)
                },
                Rows = rows
            };

            return vm;
        }

        public async Task<string> ExportCohortDeliveryCsvAsync(CohortDeliveryReportFiltersVm filters, CancellationToken ct)
        {
            var report = await BuildCohortDeliveryAsync(filters, ct);

            static string Esc(string? s)
            {
                s ??= "";
                if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
                    return "\"" + s.Replace("\"", "\"\"") + "\"";
                return s;
            }

            var sb = new StringBuilder();
            sb.AppendLine("CohortCode,IntakeYear,StartDate,PlannedEndDate,Programme,QualificationType,Provider,FundingType,Employer,Total,Active,Completed,DroppedOut,CompletionRatePct,DropoutRatePct");

            foreach (var r in report.Rows)
            {
                sb.AppendLine(string.Join(",",
                    Esc(r.CohortCode),
                    r.IntakeYear.ToString(CultureInfo.InvariantCulture),
                    r.StartDate.ToString("yyyy-MM-dd"),
                    r.PlannedEndDate.ToString("yyyy-MM-dd"),
                    Esc(r.ProgrammeName),
                    Esc(r.QualificationType),
                    Esc(r.ProviderName),
                    Esc(r.FundingType),
                    Esc(r.EmployerCode),
                    r.Total.ToString(CultureInfo.InvariantCulture),
                    r.Active.ToString(CultureInfo.InvariantCulture),
                    r.Completed.ToString(CultureInfo.InvariantCulture),
                    r.DroppedOut.ToString(CultureInfo.InvariantCulture),
                    r.CompletionRatePct.ToString(CultureInfo.InvariantCulture),
                    r.DropoutRatePct.ToString(CultureInfo.InvariantCulture)
                ));
            }

            return sb.ToString();
        }

        public async Task<byte[]> ExportCohortDeliveryExcelAsync(CohortDeliveryReportFiltersVm filters, CancellationToken ct)
        {
            var report = await BuildCohortDeliveryAsync(filters, ct);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Cohort Delivery");

            // KPI header
            ws.Cell(1, 1).Value = "Total Cohorts";
            ws.Cell(1, 2).Value = report.Kpis.TotalCohorts;
            ws.Cell(2, 1).Value = "Total Enrollments";
            ws.Cell(2, 2).Value = report.Kpis.TotalEnrollments;
            ws.Cell(3, 1).Value = "Active";
            ws.Cell(3, 2).Value = report.Kpis.ActiveEnrollments;
            ws.Cell(4, 1).Value = "Completed";
            ws.Cell(4, 2).Value = report.Kpis.CompletedEnrollments;
            ws.Cell(5, 1).Value = "Dropped Out";
            ws.Cell(5, 2).Value = report.Kpis.DroppedOutEnrollments;
            ws.Cell(6, 1).Value = "Completion Rate %";
            ws.Cell(6, 2).Value = report.Kpis.CompletionRatePct;
            ws.Cell(7, 1).Value = "Dropout Rate %";
            ws.Cell(7, 2).Value = report.Kpis.DropoutRatePct;

            ws.Range(1, 1, 7, 2).Style.Font.Bold = true;

            // Table header
            var startRow = 9;
            var headers = new[]
            {
                "CohortCode","IntakeYear","StartDate","PlannedEndDate",
                "Programme","QualificationType","Provider","FundingType","Employer",
                "Total","Active","Completed","DroppedOut","CompletionRatePct","DropoutRatePct"
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(startRow, i + 1).Value = headers[i];

            ws.Range(startRow, 1, startRow, headers.Length).Style.Font.Bold = true;

            // Data
            var r = startRow + 1;
            foreach (var row in report.Rows)
            {
                ws.Cell(r, 1).Value = row.CohortCode;
                ws.Cell(r, 2).Value = row.IntakeYear;
                ws.Cell(r, 3).Value = row.StartDate;
                ws.Cell(r, 4).Value = row.PlannedEndDate;
                ws.Cell(r, 5).Value = row.ProgrammeName;
                ws.Cell(r, 6).Value = row.QualificationType;
                ws.Cell(r, 7).Value = row.ProviderName;
                ws.Cell(r, 8).Value = row.FundingType;
                ws.Cell(r, 9).Value = row.EmployerCode ?? "";
                ws.Cell(r, 10).Value = row.Total;
                ws.Cell(r, 11).Value = row.Active;
                ws.Cell(r, 12).Value = row.Completed;
                ws.Cell(r, 13).Value = row.DroppedOut;
                ws.Cell(r, 14).Value = row.CompletionRatePct;
                ws.Cell(r, 15).Value = row.DropoutRatePct;
                r++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
