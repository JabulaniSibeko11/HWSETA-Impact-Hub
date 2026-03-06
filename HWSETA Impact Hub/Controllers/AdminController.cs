using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HWSETA_Impact_Hub.Controllers
{
    [Authorize(Policy = "AdminManage")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var vm = new DashboardVm();

            // ── QUERY 1: Beneficiary KPIs ────────────────────────────
            var beneficiaries = await _db.Beneficiaries
                .AsNoTracking()
                .Where(b => b.IsActive)
                .Select(b => new { b.RegistrationStatus })
                .ToListAsync(ct);

            vm.TotalBeneficiaries = beneficiaries.Count;
            vm.ActiveBeneficiaries = beneficiaries.Count(b =>
                b.RegistrationStatus != BeneficiaryRegistrationStatus.Completed);

            // ── QUERY 2: Enrollment KPIs ─────────────────────────────
            var enrollments = await _db.Enrollments
                .AsNoTracking()
                .Select(e => new { e.CurrentStatus, e.IsActive, e.CreatedOnUtc })
                .ToListAsync(ct);

            vm.TotalEnrollments = enrollments.Count;
            vm.CompletedEnrollments = enrollments.Count(e => e.CurrentStatus == EnrollmentStatus.Completed);
            vm.DroppedOutEnrollments = enrollments.Count(e => e.CurrentStatus == EnrollmentStatus.DroppedOut);
            vm.ActiveEnrollments = enrollments.Count(e =>
                e.IsActive &&
                e.CurrentStatus != EnrollmentStatus.Completed &&
                e.CurrentStatus != EnrollmentStatus.DroppedOut);

            vm.CompletionRatePct = vm.TotalEnrollments > 0
                ? Math.Round((decimal)vm.CompletedEnrollments * 100m / vm.TotalEnrollments, 1)
                : 0;

            // ── QUERY 3: Cohort count ────────────────────────────────
            vm.TotalCohorts = await _db.Cohorts.AsNoTracking().CountAsync(ct);

            // ── QUERY 4: New feedback count ──────────────────────────
            vm.NewFeedbackCount = await _db.BeneficiaryFeedbacks
                .AsNoTracking()
                .CountAsync(f => f.Status == FeedbackStatus.New && f.IsActive, ct);

            // ── CHART 1: Enrollment status doughnut ──────────────────
            var enrGroups = enrollments
                .GroupBy(e => e.CurrentStatus)
                .ToDictionary(g => g.Key, g => g.Count());

            vm.EnrollmentStatusLabelsJson = JsonSerializer.Serialize(
                new[] { "Enrolled", "In Training", "Completed", "Dropped Out" });
            vm.EnrollmentStatusDataJson = JsonSerializer.Serialize(new[]
            {
                enrGroups.GetValueOrDefault(EnrollmentStatus.Enrolled,   0),
                enrGroups.GetValueOrDefault(EnrollmentStatus.InTraining, 0),
                enrGroups.GetValueOrDefault(EnrollmentStatus.Completed,  0),
                enrGroups.GetValueOrDefault(EnrollmentStatus.DroppedOut, 0)
            });

            // ── CHART 2: Registration pipeline doughnut ──────────────
            var regGroups = beneficiaries
                .GroupBy(b => b.RegistrationStatus)
                .ToDictionary(g => g.Key, g => g.Count());

            vm.RegistrationLabelsJson = JsonSerializer.Serialize(
                new[] { "Added", "Invite Sent", "Password Set", "Location", "Submitted", "Completed" });
            vm.RegistrationDataJson = JsonSerializer.Serialize(new[]
            {
                regGroups.GetValueOrDefault(BeneficiaryRegistrationStatus.AddedByAdmin,         0),
                regGroups.GetValueOrDefault(BeneficiaryRegistrationStatus.InviteSent,           0),
                regGroups.GetValueOrDefault(BeneficiaryRegistrationStatus.PasswordSet,          0),
                regGroups.GetValueOrDefault(BeneficiaryRegistrationStatus.LocationCaptured,     0),
                regGroups.GetValueOrDefault(BeneficiaryRegistrationStatus.RegistrationSubmitted,0),
                regGroups.GetValueOrDefault(BeneficiaryRegistrationStatus.Completed,            0)
            });

            // ── CHART 3: Monthly enrolment trend (last 12 months) ────
            var now = DateTime.UtcNow;
            var months = Enumerable.Range(0, 12)
                .Select(i => new DateTime(now.AddMonths(-11 + i).Year, now.AddMonths(-11 + i).Month, 1))
                .ToList();

            vm.MonthLabelsJson = JsonSerializer.Serialize(months.Select(m => m.ToString("MMM yy")).ToArray());
            vm.MonthEnrolledJson = JsonSerializer.Serialize(months.Select(m =>
                enrollments.Count(e => e.CreatedOnUtc.Year == m.Year && e.CreatedOnUtc.Month == m.Month)).ToArray());
            vm.MonthCompletedJson = JsonSerializer.Serialize(months.Select(m =>
                enrollments.Count(e => e.CurrentStatus == EnrollmentStatus.Completed &&
                                       e.CreatedOnUtc.Year == m.Year && e.CreatedOnUtc.Month == m.Month)).ToArray());

            // ── CHART 4: Top 5 programmes by headcount ───────────────
            var progData = await _db.Enrollments
                .AsNoTracking()
                .Include(e => e.Cohort).ThenInclude(c => c.Programme)
                .GroupBy(e => e.Cohort.Programme.ProgrammeName)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync(ct);

            vm.ProgrammeLabelsJson = JsonSerializer.Serialize(progData.Select(p => p.Name).ToArray());
            vm.ProgrammeDataJson = JsonSerializer.Serialize(progData.Select(p => p.Count).ToArray());

            // ── CHART 5: Beneficiaries by province ───────────────────
            var provData = await _db.Beneficiaries
                .AsNoTracking()
                .Where(b => b.IsActive && b.Province != null)
                .GroupBy(b => b.Province)
                .Select(g => new { Province = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(9)
                .ToListAsync(ct);

            vm.ProvinceLabelsJson = JsonSerializer.Serialize(provData.Select(p => p.Province).ToArray());
            vm.ProvinceDataJson = JsonSerializer.Serialize(provData.Select(p => p.Count).ToArray());

            return View(vm);
        }
    }
}