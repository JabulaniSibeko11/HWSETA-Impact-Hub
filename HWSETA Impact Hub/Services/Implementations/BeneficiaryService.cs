namespace HWSETA_Impact_Hub.Services.Implementations
{
    using ClosedXML.Excel;
    using global::HWSETA_Impact_Hub.Data;
    using global::HWSETA_Impact_Hub.Domain.Entities;
    using global::HWSETA_Impact_Hub.Infrastructure.Identity;
    using global::HWSETA_Impact_Hub.Models.ViewModels.Beneficiaries;
    using global::HWSETA_Impact_Hub.Services.Interface;

    using Microsoft.EntityFrameworkCore;

    namespace HWSETA_Impact_Hub.Services.Implementations
    {
        public sealed class BeneficiaryService : IBeneficiaryService
        {
            private readonly ApplicationDbContext _db;
            private readonly ICurrentUserService _user;

            public BeneficiaryService(ApplicationDbContext db, ICurrentUserService user)
            {
                _db = db;
                _user = user;
            }

            public Task<List<Beneficiary>> ListAsync(CancellationToken ct) =>
                _db.Beneficiaries.AsNoTracking()
                    .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
                    .ToListAsync(ct);

            public async Task<(bool ok, string? error)> CreateAsync(BeneficiaryCreateVm vm, CancellationToken ct)
            {
                var idVal = vm.IdentifierValue.Trim();

                var exists = await _db.Beneficiaries.AnyAsync(x =>
                    x.IdentifierType == vm.IdentifierType &&
                    x.IdentifierValue == idVal, ct);

                if (exists) return (false, "Beneficiary already exists (same ID/Passport).");

                var b = new Beneficiary
                {
                    IdentifierType = vm.IdentifierType,
                    IdentifierValue = idVal,
                    FirstName = vm.FirstName.Trim(),
                    LastName = vm.LastName.Trim(),
                    DateOfBirth = vm.DateOfBirth,
                    Gender = vm.Gender?.Trim(),
                    Email = vm.Email?.Trim(),
                    Phone = vm.Phone?.Trim(),
                    Province = vm.Province?.Trim(),
                    City = vm.City?.Trim(),
                    AddressLine1 = vm.AddressLine1?.Trim(),
                    PostalCode = vm.PostalCode?.Trim(),
                    IsActive = vm.IsActive,
                    CreatedOnUtc = DateTime.UtcNow,
                    CreatedByUserId = _user.UserId
                };

                _db.Beneficiaries.Add(b);
                await _db.SaveChangesAsync(ct);

                return (true, null);
            }

            public async Task<BeneficiaryImportResultVm> ImportFromExcelAsync(IFormFile file, CancellationToken ct)
            {
                var result = new BeneficiaryImportResultVm();

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

                // Headers:
                // IdentifierType | IdentifierValue | FirstName | LastName | DateOfBirth | Gender | Email | Phone | Province | City | AddressLine1 | PostalCode | IsActive
                var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

                var headers = ws.Row(1).Cells().ToDictionary(
                    c => (c.GetString() ?? "").Trim(),
                    c => c.Address.ColumnNumber,
                    StringComparer.OrdinalIgnoreCase);

                int Col(string name) => headers.TryGetValue(name, out var i) ? i : -1;

                var cType = Col("IdentifierType");
                var cVal = Col("IdentifierValue");
                var cFn = Col("FirstName");
                var cLn = Col("LastName");
                var cDob = Col("DateOfBirth");
                var cGen = Col("Gender");
                var cEmail = Col("Email");
                var cPhone = Col("Phone");
                var cProv = Col("Province");
                var cCity = Col("City");
                var cAddr = Col("AddressLine1");
                var cPost = Col("PostalCode");
                var cAct = Col("IsActive");

                if (cType < 0 || cVal < 0 || cFn < 0 || cLn < 0)
                {
                    result.Errors.Add("Missing required headers. Required: IdentifierType, IdentifierValue, FirstName, LastName. Others optional.");
                    return result;
                }

                // Cache existing for upsert by (type,val)
                var existing = await _db.Beneficiaries.AsTracking()
                    .ToListAsync(ct);

                var byKey = existing.ToDictionary(
                    x => $"{(int)x.IdentifierType}|{x.IdentifierValue}".ToLowerInvariant(),
                    x => x);

                using var tx = await _db.Database.BeginTransactionAsync(ct);

                for (int r = 2; r <= lastRow; r++)
                {
                    var row = ws.Row(r);

                    var typeStr = row.Cell(cType).GetString()?.Trim() ?? "";
                    var idVal = row.Cell(cVal).GetString()?.Trim() ?? "";
                    var fn = row.Cell(cFn).GetString()?.Trim() ?? "";
                    var ln = row.Cell(cLn).GetString()?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(typeStr) && string.IsNullOrWhiteSpace(idVal))
                    {
                        result.Skipped++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(typeStr) || string.IsNullOrWhiteSpace(idVal) ||
                        string.IsNullOrWhiteSpace(fn) || string.IsNullOrWhiteSpace(ln))
                    {
                        result.Errors.Add($"Row {r}: IdentifierType, IdentifierValue, FirstName, LastName are required.");
                        result.Skipped++;
                        continue;
                    }

                    // Parse IdentifierType (accept: "SaId", "Passport", "1", "2")
                    IdentifierType idType;
                    if (int.TryParse(typeStr, out var tInt) && Enum.IsDefined(typeof(IdentifierType), tInt))
                        idType = (IdentifierType)tInt;
                    else if (Enum.TryParse<IdentifierType>(typeStr, true, out var tEnum))
                        idType = tEnum;
                    else
                    {
                        result.Errors.Add($"Row {r}: Invalid IdentifierType '{typeStr}'. Use SaId or Passport.");
                        result.Skipped++;
                        continue;
                    }

                    DateTime? dob = null;
                    if (cDob > 0)
                    {
                        var dobStr = row.Cell(cDob).GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(dobStr) && DateTime.TryParse(dobStr, out var dt))
                            dob = dt.Date;
                    }

                    var gender = cGen > 0 ? row.Cell(cGen).GetString()?.Trim() : null;
                    var email = cEmail > 0 ? row.Cell(cEmail).GetString()?.Trim() : null;
                    var phone = cPhone > 0 ? row.Cell(cPhone).GetString()?.Trim() : null;
                    var prov = cProv > 0 ? row.Cell(cProv).GetString()?.Trim() : null;
                    var city = cCity > 0 ? row.Cell(cCity).GetString()?.Trim() : null;
                    var addr = cAddr > 0 ? row.Cell(cAddr).GetString()?.Trim() : null;
                    var post = cPost > 0 ? row.Cell(cPost).GetString()?.Trim() : null;

                    bool isActive = true;
                    if (cAct > 0)
                    {
                        var actStr = row.Cell(cAct).GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(actStr) && bool.TryParse(actStr, out var b))
                            isActive = b;
                    }

                    var key = $"{(int)idType}|{idVal}".ToLowerInvariant();

                    if (byKey.TryGetValue(key, out var bExisting))
                    {
                        // update
                        bExisting.FirstName = fn;
                        bExisting.LastName = ln;
                        bExisting.DateOfBirth = dob;
                        bExisting.Gender = gender;
                        bExisting.Email = email;
                        bExisting.Phone = phone;
                        bExisting.Province = prov;
                        bExisting.City = city;
                        bExisting.AddressLine1 = addr;
                        bExisting.PostalCode = post;
                        bExisting.IsActive = isActive;

                        bExisting.UpdatedOnUtc = DateTime.UtcNow;
                        bExisting.UpdatedByUserId = _user.UserId;

                        result.Updated++;
                    }
                    else
                    {
                        var bNew = new Beneficiary
                        {
                            IdentifierType = idType,
                            IdentifierValue = idVal,
                            FirstName = fn,
                            LastName = ln,
                            DateOfBirth = dob,
                            Gender = gender,
                            Email = email,
                            Phone = phone,
                            Province = prov,
                            City = city,
                            AddressLine1 = addr,
                            PostalCode = post,
                            IsActive = isActive,
                            CreatedOnUtc = DateTime.UtcNow,
                            CreatedByUserId = _user.UserId
                        };

                        _db.Beneficiaries.Add(bNew);
                        byKey[key] = bNew;

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
}