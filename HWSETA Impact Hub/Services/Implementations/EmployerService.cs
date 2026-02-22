using ClosedXML.Excel;
using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Models.ViewModels.Employers;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class EmployerService : IEmployerService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _user;

        public EmployerService(ApplicationDbContext db, ICurrentUserService user)
        {
            _db = db;
            _user = user;
        }

        public Task<List<Employer>> ListAsync(CancellationToken ct) =>
            _db.Employers.AsNoTracking().OrderBy(x => x.EmployerName).ToListAsync(ct);

        public async Task<(bool ok, string? error)> CreateAsync(EmployerCreateVm vm, CancellationToken ct)
        {
            // Prevent duplicates by EmployerCode if provided
            if (!string.IsNullOrWhiteSpace(vm.EmployerCode))
            {
                var existsCode = await _db.Employers.AnyAsync(x => x.EmployerCode == vm.EmployerCode, ct);
                if (existsCode) return (false, "EmployerCode already exists.");
            }

            //Province Lookup
            var provinceName = vm.ProvinceId != Guid.Empty ? (await _db.Provinces.Where(x => x.Id == vm.ProvinceId).Select(x => x.Name).FirstOrDefaultAsync(ct)) : null;

            //Save Address
            // Create Address first (required)
            var addr = new Address
            {
                AddressLine1 = vm.AddressLine1.Trim(),
                City = vm.City.Trim(),
                PostalCode = vm.PostalCode.Trim(),
                ProvinceId = vm.ProvinceId,
                CreatedOnUtc = DateTime.UtcNow,
                CreatedByUserId = _user.UserId
            };
            _db.Addresses.Add(addr);

            var e = new Employer
            {
                EmployerName = vm.EmployerName.Trim(),
                EmployerCode = string.IsNullOrWhiteSpace(vm.EmployerCode) ? null : vm.EmployerCode.Trim(),
                TradingName = vm.EmployerName.Trim(),
                Sector = vm.Sector.Trim(),
                RegistrationTypeId = vm.RegistrationTypeId,
                RegistrationNumber = vm.RegistrationNumber.Trim(),
                SetaLevyNumber = vm.SetaLevyNumber.Trim(),
                AddressId = addr.Id,
                Province = provinceName,
                ContactName = vm.ContactName?.Trim(),
                ContactEmail = vm.ContactEmail?.Trim(),
                ContactPhone = vm.ContactPhone.Trim(),
                Phone = vm.Phone?.Trim(),

                IsActive = vm.IsActive,
                CreatedOnUtc = DateTime.UtcNow,
                CreatedByUserId = _user.UserId
            };

            _db.Employers.Add(e);
            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        public async Task<EmployerImportResultVm> ImportFromExcelAsync(IFormFile file, CancellationToken ct)
        {
            var result = new EmployerImportResultVm();

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

            // Load workbook
            using var stream = file.OpenReadStream();
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                result.Errors.Add("Excel file has no worksheet.");
                return result;
            }

            // Expected columns (Row 1 headers):
            // EmployerCode | EmployerName | Sector | Province | ContactName | ContactEmail | Phone
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            // Map headers → col index
            var headerRow = 1;
            var headers = ws.Row(headerRow).Cells().ToDictionary(
                c => (c.GetString() ?? "").Trim(),
                c => c.Address.ColumnNumber,
                StringComparer.OrdinalIgnoreCase);

            int Col(string name) => headers.TryGetValue(name, out var i) ? i : -1;

            var cCode = Col("EmployerCode");
            var cName = Col("EmployerName");
            var cSector = Col("Sector");
            var cProv = Col("Province");
            var cCName = Col("ContactName");
            var cCEmail = Col("ContactEmail");
            var cPhone = Col("Phone");

            if (cName < 0 || cSector < 0 || cProv < 0)
            {
                result.Errors.Add("Missing required headers. Required: EmployerName, Sector, Province. Optional: EmployerCode, ContactName, ContactEmail, Phone.");
                return result;
            }

            // Cache existing employers by EmployerCode (best key)
            var existingByCode = await _db.Employers
                .Where(x => x.EmployerCode != null)
                .ToDictionaryAsync(x => x.EmployerCode!, x => x, StringComparer.OrdinalIgnoreCase, ct);

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            for (int r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                var name = row.Cell(cName).GetString()?.Trim() ?? "";
                var sector = row.Cell(cSector).GetString()?.Trim() ?? "";
                var prov = row.Cell(cProv).GetString()?.Trim() ?? "";
                var code = cCode > 0 ? (row.Cell(cCode).GetString()?.Trim()) : null;

                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(code))
                {
                    result.Skipped++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(sector) || string.IsNullOrWhiteSpace(prov))
                {
                    result.Errors.Add($"Row {r}: EmployerName, Sector, Province are required.");
                    result.Skipped++;
                    continue;
                }

                var contactName = cCName > 0 ? row.Cell(cCName).GetString()?.Trim() : null;
                var contactEmail = cCEmail > 0 ? row.Cell(cCEmail).GetString()?.Trim() : null;
                var phone = cPhone > 0 ? row.Cell(cPhone).GetString()?.Trim() : null;

                Employer? entity = null;

                // Upsert logic:
                // 1) If EmployerCode exists: update that employer
                // 2) Else insert new employer
                if (!string.IsNullOrWhiteSpace(code) && existingByCode.TryGetValue(code, out var found))
                {
                    entity = found;
                    entity.EmployerName = name;
                    entity.Sector = sector;
                    entity.Province = prov;
                    entity.ContactName = contactName;
                    entity.ContactEmail = contactEmail;
                    entity.Phone = phone;
                    entity.UpdatedOnUtc = DateTime.UtcNow;
                    entity.UpdatedByUserId = _user.UserId;

                    result.Updated++;
                }
                else
                {
                    entity = new Employer
                    {
                        EmployerCode = string.IsNullOrWhiteSpace(code) ? null : code,
                        EmployerName = name,
                        Sector = sector,
                        Province = prov,
                        ContactName = contactName,
                        ContactEmail = contactEmail,
                        Phone = phone,
                        CreatedOnUtc = DateTime.UtcNow,
                        CreatedByUserId = _user.UserId
                    };

                    _db.Employers.Add(entity);

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

