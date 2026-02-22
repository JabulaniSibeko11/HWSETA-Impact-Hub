using ClosedXML.Excel;
using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Models.ViewModels.Provider;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class ProviderService : IProviderService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _user;

        public ProviderService(ApplicationDbContext db, ICurrentUserService user)
        {
            _db = db;
            _user = user;
        }

        public Task<List<Provider>> ListAsync(CancellationToken ct) =>
            _db.Providers.AsNoTracking().OrderBy(x => x.ProviderName).ToListAsync(ct);

        public async Task<(bool ok, string? error)> CreateAsync(ProviderCreateVm vm, CancellationToken ct)
        {
            // Uniqueness checks
            var acc = vm.AccreditationNo.Trim();
            if (await _db.Providers.AnyAsync(x => x.AccreditationNo == acc, ct))
                return (false, "AccreditationNo already exists.");

            if (!string.IsNullOrWhiteSpace(vm.ProviderCode))
            {
                var code = vm.ProviderCode.Trim();
                if (await _db.Providers.AnyAsync(x => x.ProviderCode == code, ct))
                    return (false, "ProviderCode already exists.");
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

            var p = new Provider
            {
                ProviderName = vm.ProviderName.Trim(),
                ProviderCode = string.IsNullOrWhiteSpace(vm.ProviderCode) ? null : vm.ProviderCode.Trim(),
                AccreditationNo = acc,
                AccreditationStartDate = vm.AccreditationStartDate,
                AccreditationEndDate = vm.AccreditationEndDate,
                AddressId = addr.Id,
                Province = provinceName,
                ContactName = vm.ContactName?.Trim(),
                ContactEmail = vm.ContactEmail?.Trim(),
                ContactPhone = vm.ContactPhone?.Trim(),
                Phone = vm.Phone?.Trim(),

                IsActive = vm.IsActive,
                CreatedOnUtc = DateTime.UtcNow,
                CreatedByUserId = _user.UserId
            };

            _db.Providers.Add(p);
            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        public async Task<ProviderImportResultVm> ImportFromExcelAsync(IFormFile file, CancellationToken ct)
        {
            var result = new ProviderImportResultVm();

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
            // ProviderCode | ProviderName | AccreditationNo | Province | ContactName | ContactEmail | Phone
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            var headers = ws.Row(1).Cells().ToDictionary(
                c => (c.GetString() ?? "").Trim(),
                c => c.Address.ColumnNumber,
                StringComparer.OrdinalIgnoreCase);

            int Col(string name) => headers.TryGetValue(name, out var i) ? i : -1;

            var cCode = Col("ProviderCode");
            var cName = Col("ProviderName");
            var cAcc = Col("AccreditationNo");
            var cProv = Col("Province");
            var cCName = Col("ContactName");
            var cCEmail = Col("ContactEmail");
            var cPhone = Col("Phone");

            if (cName < 0 || cAcc < 0 || cProv < 0)
            {
                result.Errors.Add("Missing required headers. Required: ProviderName, AccreditationNo, Province. Optional: ProviderCode, ContactName, ContactEmail, Phone.");
                return result;
            }

            // Upsert key: AccreditationNo (most reliable)
            var existingByAcc = await _db.Providers
                .AsTracking()
                .ToDictionaryAsync(x => x.AccreditationNo, x => x, StringComparer.OrdinalIgnoreCase, ct);

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            for (int r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);

                var name = row.Cell(cName).GetString()?.Trim() ?? "";
                var acc = row.Cell(cAcc).GetString()?.Trim() ?? "";
                var prov = row.Cell(cProv).GetString()?.Trim() ?? "";
                var code = cCode > 0 ? row.Cell(cCode).GetString()?.Trim() : null;

                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(acc))
                {
                    result.Skipped++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(acc) || string.IsNullOrWhiteSpace(prov))
                {
                    result.Errors.Add($"Row {r}: ProviderName, AccreditationNo, Province are required.");
                    result.Skipped++;
                    continue;
                }

                var contactName = cCName > 0 ? row.Cell(cCName).GetString()?.Trim() : null;
                var contactEmail = cCEmail > 0 ? row.Cell(cCEmail).GetString()?.Trim() : null;
                var phone = cPhone > 0 ? row.Cell(cPhone).GetString()?.Trim() : null;

                if (existingByAcc.TryGetValue(acc, out var existing))
                {
                    // update
                    existing.ProviderName = name;
                    existing.ProviderCode = string.IsNullOrWhiteSpace(code) ? existing.ProviderCode : code;
                    existing.Province = prov;
                    existing.ContactName = contactName;
                    existing.ContactEmail = contactEmail;
                    existing.Phone = phone;

                    existing.UpdatedOnUtc = DateTime.UtcNow;
                    existing.UpdatedByUserId = _user.UserId;

                    result.Updated++;
                }
                else
                {
                    // insert
                    var p = new Provider
                    {
                        ProviderName = name,
                        ProviderCode = string.IsNullOrWhiteSpace(code) ? null : code,
                        AccreditationNo = acc,
                        Province = prov,
                        ContactName = contactName,
                        ContactEmail = contactEmail,
                        Phone = phone,
                        CreatedOnUtc = DateTime.UtcNow,
                        CreatedByUserId = _user.UserId
                    };

                    _db.Providers.Add(p);
                    existingByAcc[acc] = p;

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

