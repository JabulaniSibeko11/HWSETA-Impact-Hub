using ClosedXML.Excel;
using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Models.ViewModels.Programme;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class ProgrammeService : IProgrammeService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _user;

        public ProgrammeService(ApplicationDbContext db, ICurrentUserService user)
        {
            _db = db;
            _user = user;
        }

        public Task<List<Programme>> ListAsync(CancellationToken ct) =>
            _db.Programmes.AsNoTracking().OrderBy(x => x.ProgrammeName).ToListAsync(ct);

        public async Task<(bool ok, string? error)> CreateAsync(ProgrammeCreateVm vm, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(vm.ProgrammeName))
                return (false, "ProgrammeName is required.");

            if (string.IsNullOrWhiteSpace(vm.QualificationType))
                return (false, "QualificationType is required.");

            var name = vm.ProgrammeName.Trim();

            // Optional unique code check
            string? code = string.IsNullOrWhiteSpace(vm.ProgrammeCode) ? null : vm.ProgrammeCode.Trim();
            if (!string.IsNullOrWhiteSpace(code))
            {
                if (await _db.Programmes.AnyAsync(x => x.ProgrammeCode == code, ct))
                    return (false, "ProgrammeCode already exists.");
            }

            // QualificationType lookup (by Name) -> REQUIRED
            var qName = vm.QualificationType.Trim();

            var qualificationTypeId = await _db.QualificationTypes
                .Where(x => x.IsActive && x.Name == qName)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);

            if (qualificationTypeId == Guid.Empty)
                return (false, $"QualificationType '{qName}' not found in lookup.");

            var p = new Programme
            {
                ProgrammeName = name,
                ProgrammeCode = code,
                QualificationTypeId = qualificationTypeId,   // Guid (required)
                IsActive = vm.IsActive,
                CreatedOnUtc = DateTime.UtcNow,
                CreatedByUserId = _user.UserId
            };

            _db.Programmes.Add(p);
            await _db.SaveChangesAsync(ct);

            return (true, null);
        }

        public async Task<ProgrammeImportResultVm> ImportFromExcelAsync(IFormFile file, CancellationToken ct)
        {
            var result = new ProgrammeImportResultVm();

            if (file == null || file.Length == 0)
            {
                result.Errors.Add("No file uploaded.");
                return result;
            }

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add("Only .xlsx files are supported.");
                return result;
            }

            using var stream = file.OpenReadStream();
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.FirstOrDefault();

            if (ws == null)
            {
                result.Errors.Add("Excel file has no worksheet.");
                return result;
            }

            // Expected headers:
            // ProgrammeCode | ProgrammeName | QualificationType | IsActive
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            var headers = ws.Row(1).CellsUsed().ToDictionary(
                c => (c.GetString() ?? "").Trim(),
                c => c.Address.ColumnNumber,
                StringComparer.OrdinalIgnoreCase);

            int Col(string name) => headers.TryGetValue(name, out var i) ? i : -1;

            var cCode = Col("ProgrammeCode");
            var cName = Col("ProgrammeName");
            var cQual = Col("QualificationType");
            var cAct = Col("IsActive");

            if (cName < 0)
            {
                result.Errors.Add("Missing required header: ProgrammeName. Optional: ProgrammeCode, IsActive. Required: QualificationType");
                return result;
            }

            if (cQual < 0)
            {
                result.Errors.Add("Missing required header: QualificationType.");
                return result;
            }

            // Lookups: QualificationTypes (Name -> Id)
            var qualTypes = await _db.QualificationTypes
                .Where(x => x.IsActive)
                .Select(x => new { x.Id, x.Name })
                .ToListAsync(ct);

            var qualByName = qualTypes
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.Name!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            // Upsert by ProgrammeCode if present
            var existingByCode = await _db.Programmes
                .Where(x => x.ProgrammeCode != null)
                .AsTracking()
                .ToDictionaryAsync(x => x.ProgrammeCode!, x => x, StringComparer.OrdinalIgnoreCase, ct);

            // bool parsing (true/false, 1/0, yes/no)
            static bool TryParseBoolLoose(string? s, out bool value)
            {
                value = false;
                if (string.IsNullOrWhiteSpace(s)) return false;

                s = s.Trim();
                if (bool.TryParse(s, out value)) return true;

                if (s.Equals("1") || s.Equals("yes", StringComparison.OrdinalIgnoreCase) || s.Equals("y", StringComparison.OrdinalIgnoreCase))
                { value = true; return true; }

                if (s.Equals("0") || s.Equals("no", StringComparison.OrdinalIgnoreCase) || s.Equals("n", StringComparison.OrdinalIgnoreCase))
                { value = false; return true; }

                return false;
            }

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            for (int r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);

                var name = row.Cell(cName).GetString()?.Trim() ?? "";
                var code = cCode > 0 ? row.Cell(cCode).GetString()?.Trim() : null;

                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(code))
                {
                    result.Skipped++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    result.Errors.Add($"Row {r}: ProgrammeName is required.");
                    result.Skipped++;
                    continue;
                }

                // QualificationType REQUIRED per row
                var qualName = row.Cell(cQual).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(qualName))
                {
                    result.Errors.Add($"Row {r}: QualificationType is required.");
                    result.Skipped++;
                    continue;
                }

                if (!qualByName.TryGetValue(qualName, out var qualId))
                {
                    result.Errors.Add($"Row {r}: Unknown QualificationType '{qualName}'.");
                    result.Skipped++;
                    continue;
                }

                bool isActive = true;
                if (cAct > 0)
                {
                    var actStr = row.Cell(cAct).GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(actStr) && TryParseBoolLoose(actStr, out var b))
                        isActive = b;
                }

                if (!string.IsNullOrWhiteSpace(code) && existingByCode.TryGetValue(code, out var found))
                {
                    // update
                    found.ProgrammeName = name;
                    found.QualificationTypeId = qualId;   // Guid (required)
                    found.IsActive = isActive;
                    found.UpdatedOnUtc = DateTime.UtcNow;
                    found.UpdatedByUserId = _user.UserId;

                    result.Updated++;
                }
                else
                {
                    var entity = new Programme
                    {
                        ProgrammeCode = string.IsNullOrWhiteSpace(code) ? null : code,
                        ProgrammeName = name,
                        QualificationTypeId = qualId,      // Guid (required)
                        IsActive = isActive,
                        CreatedOnUtc = DateTime.UtcNow,
                        CreatedByUserId = _user.UserId
                    };

                    _db.Programmes.Add(entity);

                    if (!string.IsNullOrWhiteSpace(code))
                        existingByCode[code] = entity;

                    result.Inserted++;
                }

                result.TotalRows++;
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return result;
        }
    }
}