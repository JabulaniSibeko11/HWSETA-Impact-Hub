using ClosedXML.Excel;
using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Models.ViewModels.Programme;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System;

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
            if (!string.IsNullOrWhiteSpace(vm.ProgrammeCode))
            {
                var code = vm.ProgrammeCode.Trim();
                if (await _db.Programmes.AnyAsync(x => x.ProgrammeCode == code, ct))
                    return (false, "ProgrammeCode already exists.");
            }

            var p = new Programme
            {
                ProgrammeName = vm.ProgrammeName.Trim(),
                ProgrammeCode = string.IsNullOrWhiteSpace(vm.ProgrammeCode) ? null : vm.ProgrammeCode.Trim(),
                NqfLevel = vm.NqfLevel?.Trim(),
                QualificationType = vm.QualificationType?.Trim(),
                DurationMonths = vm.DurationMonths,
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
            // ProgrammeCode | ProgrammeName | NqfLevel | QualificationType | DurationMonths | IsActive
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            var headers = ws.Row(1).Cells().ToDictionary(
                c => (c.GetString() ?? "").Trim(),
                c => c.Address.ColumnNumber,
                StringComparer.OrdinalIgnoreCase);

            int Col(string name) => headers.TryGetValue(name, out var i) ? i : -1;

            var cCode = Col("ProgrammeCode");
            var cName = Col("ProgrammeName");
            var cNqf = Col("NqfLevel");
            var cQual = Col("QualificationType");
            var cDur = Col("DurationMonths");
            var cAct = Col("IsActive");

            if (cName < 0)
            {
                result.Errors.Add("Missing required header: ProgrammeName. Optional: ProgrammeCode, NqfLevel, QualificationType, DurationMonths, IsActive");
                return result;
            }

            // Upsert by ProgrammeCode if present, else insert
            var existingByCode = await _db.Programmes
                .Where(x => x.ProgrammeCode != null)
                .AsTracking()
                .ToDictionaryAsync(x => x.ProgrammeCode!, x => x, StringComparer.OrdinalIgnoreCase, ct);

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

                var nqf = cNqf > 0 ? row.Cell(cNqf).GetString()?.Trim() : null;
                var qual = cQual > 0 ? row.Cell(cQual).GetString()?.Trim() : null;

                int? dur = null;
                if (cDur > 0)
                {
                    var durStr = row.Cell(cDur).GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(durStr) && int.TryParse(durStr, out var dm))
                        dur = dm;
                }

                bool isActive = true;
                if (cAct > 0)
                {
                    var actStr = row.Cell(cAct).GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(actStr) && bool.TryParse(actStr, out var b))
                        isActive = b;
                }

                Programme? entity = null;

                if (!string.IsNullOrWhiteSpace(code) && existingByCode.TryGetValue(code, out var found))
                {
                    entity = found;
                    entity.ProgrammeName = name;
                    entity.NqfLevel = nqf;
                    entity.QualificationType = qual;
                    entity.DurationMonths = dur;
                    entity.IsActive = isActive;
                    entity.UpdatedOnUtc = DateTime.UtcNow;
                    entity.UpdatedByUserId = _user.UserId;

                    result.Updated++;
                }
                else
                {
                    entity = new Programme
                    {
                        ProgrammeCode = string.IsNullOrWhiteSpace(code) ? null : code,
                        ProgrammeName = name,
                        NqfLevel = nqf,
                        QualificationType = qual,
                        DurationMonths = dur,
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

