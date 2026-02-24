using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Models.ViewModels.Provider;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
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

            //Define Columns
            var cCode = Col("ProviderCode");
            var cName = Col("ProviderName");
            var cAcc = Col("AccreditationNo");
            var cStart = Col("AccreditationStartDate");
            var cEnd = Col("AccreditationEndDate");
            var cContactName = Col("ContactName");
            var cContactEmail = Col("ContactEmail");
            var cContactPhone = Col("ContactPhone");
            var cPhone = Col("Phone");

            var cAddr1 = Col("AddressLine1");
            var cSuburb = Col("Suburb");
            var cCity = Col("City");
            var cPostal = Col("PostalCode");
            var cProvince = Col("Province");
            var cIsActive = Col("IsActive");

            if (cName < 0 || cAcc < 0 || cProvince < 0 || cAddr1 < 0 || cCity < 0)
            {
                result.Errors.Add("Missing required headers.");
                return result;
            }

            //Lookup provinces
            var provinces = await _db.Provinces
              .ToDictionaryAsync(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase, ct);


            // Upsert key: AccreditationNo (most reliable)
            var existingByAcc = await _db.Providers
                .AsTracking()
                .ToDictionaryAsync(x => x.AccreditationNo, x => x, StringComparer.OrdinalIgnoreCase, ct);

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            for (int r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);

                //Required Columns
                var name = row.Cell(cName).GetString().Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    result.Errors.Add($"Row {r}: Provider Name is required.");
                    result.Skipped++;
                    continue;
                }

                var code = row.Cell(cCode).GetString().Trim();
                if (string.IsNullOrWhiteSpace(code))
                {
                    result.Errors.Add($"Row {r}:  Provider Code is required.");
                    result.Skipped++;
                    continue;
                }

                var accreditationNo = row.Cell(cAcc).GetString().Trim();
                if (string.IsNullOrWhiteSpace(accreditationNo))
                {
                    result.Errors.Add($"Row {r}:  Provider Code is required.");
                    result.Skipped++;
                    continue;
                }

                //Employer Contacts
                var contactName = row.Cell(cContactName).GetString().Trim();
                if (string.IsNullOrWhiteSpace(contactName))
                {
                    result.Errors.Add($"Row {r}:  Provider Contact Name is required.");
                    result.Skipped++;
                    continue;
                }

                var contactPhone = row.Cell(cContactPhone).GetString().Trim();
                if (string.IsNullOrWhiteSpace(contactPhone))
                {
                    result.Errors.Add($"Row {r}:  Provider Contact Phone is required.");
                    result.Skipped++;
                    continue;
                }

                var contactEmail = row.Cell(cContactEmail).GetString().Trim();
                if (string.IsNullOrWhiteSpace(contactEmail))
                {
                    result.Errors.Add($"Row {r}:  Provider Contact Email is required.");
                    result.Skipped++;
                    continue;
                }

                var provinceName = row.Cell(cProvince).GetString().Trim();
                var province = await _db.Provinces.FirstOrDefaultAsync(x => x.Name == provinceName, ct);

                if (province == null)
                {
                    result.Errors.Add($"Row {r}: Invalid Province.");
                    result.Skipped++;
                    continue;
                }

                bool isActive = true;
                if (cIsActive > 0)
                {
                    var actStr = row.Cell(cIsActive).GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(actStr) && TryParseBoolLoose(actStr, out var b))
                        isActive = b;
                }


                //Optional Columns
                DateTime? startDate = cStart > 0 ? GetNullableDate(row.Cell(cStart)) : null;
                DateTime? endDate = cEnd > 0 ? GetNullableDate(row.Cell(cEnd)) : null;
                var suburb = cSuburb > 0 ? row.Cell(cSuburb).GetString().Trim() : null;


                //Verify Address Columns
                var addr1 = row.Cell(cAddr1).GetString().Trim();
                if (string.IsNullOrWhiteSpace(addr1))
                {
                    result.Errors.Add($"Row {r}: Provider Street Address is required.");
                    result.Skipped++;
                    continue;
                }

                var city = row.Cell(cCity).GetString().Trim();
                if (string.IsNullOrWhiteSpace(city))
                {
                    result.Errors.Add($"Row {r}: Provider City is required.");
                    result.Skipped++;
                    continue;
                }

                var postalCode = row.Cell(cPostal).GetString().Trim();
                if (string.IsNullOrWhiteSpace(postalCode))
                {
                    result.Errors.Add($"Row {r}: Provider Postal Code is required.");
                    result.Skipped++;
                    continue;
                }


                if (existingByAcc.TryGetValue(accreditationNo, out var existing))
                {
                    // update
                    existing.ProviderName = name;
                    existing.ProviderCode = code;
                    existing.AccreditationStartDate = startDate;
                    existing.AccreditationEndDate = endDate;
                    existing.Province = province.Name;

                    existing.ContactName = contactName;
                    existing.ContactEmail = contactEmail;
                    existing.ContactPhone = contactPhone;
                    existing.Phone = contactPhone;
                    existing.IsActive = isActive;

                    // Update Address
                    existing.Address.AddressLine1 = addr1;
                    existing.Address.City = city;
                    existing.Address.Suburb = suburb;
                    existing.Address.PostalCode = postalCode;
                    existing.Address.ProvinceId = province.Id;

                    existing.UpdatedOnUtc = DateTime.UtcNow;
                    existing.UpdatedByUserId = _user.UserId;

                    result.Updated++;
                }
                else
                {
                    // insert

                    var address = new Address
                    {
                        AddressLine1 = addr1,
                        Suburb = suburb,
                        City = city,
                        PostalCode = postalCode,
                        ProvinceId = province.Id,
                        CreatedOnUtc = DateTime.UtcNow,
                        CreatedByUserId = _user.UserId
                    };


                    _db.Addresses.Add(address);

                    var provider = new Provider
                    {
                        ProviderName = name,
                        ProviderCode = code,
                        AccreditationNo = accreditationNo,
                        AccreditationStartDate = startDate,
                        AccreditationEndDate = endDate,
                        Province = province.Name,
                        Address = address,

                        ContactName = contactName,
                        ContactEmail = contactEmail,
                        ContactPhone = contactPhone,
                        Phone = contactPhone,
                        IsActive = isActive,

                        CreatedOnUtc = DateTime.UtcNow,
                        CreatedByUserId = _user.UserId
                    };


                    _db.Providers.Add(provider);
                    existingByAcc[accreditationNo] = provider;

                    result.Inserted++;
                }

                result.TotalRows++;
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return result;
        }

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

        DateTime? GetNullableDate(IXLCell cell)
        {
            if (cell == null || cell.IsEmpty())
                return null;

            if (cell.TryGetValue<DateTime>(out var dt))
                return dt;

            // In case it's stored as text
            if (DateTime.TryParse(cell.GetString(), out dt))
                return dt;

            return null;
        }
    }
}

