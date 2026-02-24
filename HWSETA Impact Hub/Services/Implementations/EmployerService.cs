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

            //Define Columns
            var cCode = Col("EmployerCode");
            var cName = Col("EmployerName");
            var cTrading = Col("TradingName");
            var cRegType = Col("RegistrationType");
            var cRegNo = Col("RegistrationNumber");
            var cLevy = Col("SetaLevyNumber");
            var cSector = Col("Sector");
            var cProv = Col("Province");
            var cCName = Col("ContactName");
            var cCEmail = Col("ContactEmail");
            var cCPhone = Col("ContactPhone");
            var cAddr1 = Col("AddressLine1");
            var cSuburb = Col("Suburb");
            var cCity = Col("City");
            var cPostal = Col("PostalCode");
            var cAct = Col("IsActive");

            if (cName < 0 || cSector < 0 || cProv < 0)
            {
                result.Errors.Add("Missing required headers. Required: EmployerName, Sector, Province. Optional: EmployerCode, ContactName, ContactEmail, Phone.");
                return result;
            }


            // Lookup Registration Types
            var registrationLookup = await _db.EmployerRegistrationTypes
                .ToDictionaryAsync(x => x.Name.Trim(), x => x.Name,
                    StringComparer.OrdinalIgnoreCase, ct);



            // Cache existing employers by EmployerCode (best key)
            var existingByCode = await _db.Employers
                .Where(x => x.EmployerCode != null)
                .ToDictionaryAsync(x => x.EmployerCode!, x => x, StringComparer.OrdinalIgnoreCase, ct);



            using var tx = await _db.Database.BeginTransactionAsync(ct);

            for (int r = 2; r <= lastRow; r++)
            {
                var row = ws.Row(r);

                //Required Columns
                var name = row.Cell(cName).GetString().Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    result.Errors.Add($"Row {r}: EmployerName is required.");
                    result.Skipped++;
                    continue;
                }

                var code = row.Cell(cCode).GetString().Trim();
                if (string.IsNullOrWhiteSpace(code))
                {
                    result.Errors.Add($"Row {r}: Employer Code is required.");
                    result.Skipped++;
                    continue;
                }

                var registrationTypeName = row.Cell(cRegType).GetString().Trim();
                var registrationType = await _db.EmployerRegistrationTypes
                    .FirstOrDefaultAsync(x => x.Name == registrationTypeName, ct);

                if (registrationType == null)
                {
                    result.Errors.Add($"Row {r}: Invalid RegistrationType.");
                    result.Skipped++;
                    continue;
                }

                var registrationNumber = row.Cell(cRegNo).GetString().Trim();
                if (string.IsNullOrWhiteSpace(registrationNumber))
                {
                    result.Errors.Add($"Row {r}: Employer Registration Number is required.");
                    result.Skipped++;
                    continue;
                }

                var setaLevyNum = row.Cell(cLevy).GetString().Trim();
                if (string.IsNullOrWhiteSpace(setaLevyNum))
                {
                    result.Errors.Add($"Row {r}: Employer Seta Levey Number is required.");
                    result.Skipped++;
                    continue;
                }

                var provinceName = row.Cell(cProv).GetString().Trim();
                var province = await _db.Provinces.FirstOrDefaultAsync(x => x.Name == provinceName, ct);

                if (province == null)
                {
                    result.Errors.Add($"Row {r}: Invalid Province.");
                    result.Skipped++;
                    continue;
                }

                var sector = row.Cell(cSector).GetString().Trim();
                if (string.IsNullOrWhiteSpace(sector))
                {
                    result.Errors.Add($"Row {r}: Employer Sector is required.");
                    result.Skipped++;
                    continue;
                }

                //Employer Contacts
                var contactName = row.Cell(cCName).GetString().Trim();
                if (string.IsNullOrWhiteSpace(contactName))
                {
                    result.Errors.Add($"Row {r}: Employer Contact Name is required.");
                    result.Skipped++;
                    continue;
                }

                var contactPhone = row.Cell(cCPhone).GetString().Trim();
                if (string.IsNullOrWhiteSpace(contactPhone))
                {
                    result.Errors.Add($"Row {r}: Employer Contact Phone is required.");
                    result.Skipped++;
                    continue;
                }

                var contactEmail = row.Cell(cCEmail).GetString().Trim();
                if (string.IsNullOrWhiteSpace(contactEmail))
                {
                    result.Errors.Add($"Row {r}: Employer Contact Email is required.");
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



                //Optinal Columns
                var tradingName = cTrading > 0 ? row.Cell(cTrading).GetString().Trim() : null;
                var suburb = cSuburb > 0 ? row.Cell(cSuburb).GetString().Trim() : null;

                //Verify Address Columns
                var addr1 = row.Cell(cAddr1).GetString().Trim();
                if (string.IsNullOrWhiteSpace(addr1))
                {
                    result.Errors.Add($"Row {r}: Employer Street Address is required.");
                    result.Skipped++;
                    continue;
                }

                var city = row.Cell(cCity).GetString().Trim();
                if (string.IsNullOrWhiteSpace(city))
                {
                    result.Errors.Add($"Row {r}: Employer City is required.");
                    result.Skipped++;
                    continue;
                }

                var postalCode = row.Cell(cPostal).GetString().Trim();
                if (string.IsNullOrWhiteSpace(postalCode))
                {
                    result.Errors.Add($"Row {r}: Employer Postal Code is required.");
                    result.Skipped++;
                    continue;
                }

                Employer? entity = null;

                // Upsert logic:
                // 1) If EmployerCode exists: update that employer
                // 2) Else insert new employer
                if (!string.IsNullOrWhiteSpace(code) && existingByCode.TryGetValue(code, out var found))
                {
                    entity = found;
                    // 🔁 UPDATE
                    entity.EmployerName = name;
                    entity.TradingName = tradingName;
                    entity.RegistrationTypeId = registrationType.Id;
                    entity.RegistrationNumber = registrationNumber;
                    entity.SetaLevyNumber = setaLevyNum;
                    entity.Sector = sector;
                    entity.Province = province.Name;
                    entity.ContactName = contactName;
                    entity.ContactEmail = contactEmail;
                    entity.ContactPhone = contactPhone;
                    entity.Phone = contactPhone;
                    entity.UpdatedOnUtc = DateTime.UtcNow;
                    entity.UpdatedByUserId = _user.UserId;
                    entity.IsActive = isActive;

                    // Update Address
                    if (entity.Address != null)
                    {
                        entity.Address.AddressLine1 = addr1;
                        entity.Address.Suburb = suburb;
                        entity.Address.City = city;
                        entity.Address.PostalCode = postalCode;
                        entity.Address.ProvinceId = province.Id;
                    }


                    result.Updated++;
                }
                else
                {
                    // Create Address
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

                    var employer = new Employer
                    {
                        EmployerCode = code,
                        EmployerName = name,
                        TradingName = tradingName,
                        RegistrationTypeId = registrationType.Id,
                        RegistrationNumber = registrationNumber,
                        SetaLevyNumber = setaLevyNum,
                        Sector = sector,
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

                    _db.Employers.Add(employer);

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

    }
}

