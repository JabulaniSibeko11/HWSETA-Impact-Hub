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

            public async Task<(bool ok, string? error, Guid? beneficiaryId)> CreateAsync(BeneficiaryCreateVm vm, CancellationToken ct)
            {
                var idVal = vm.IdentifierValue.Trim();

                var exists = await _db.Beneficiaries.AnyAsync(x =>
                    x.IdentifierType == vm.IdentifierType &&
                    x.IdentifierValue == idVal, ct);

                if (exists)
                    return (false, "Beneficiary already exists (same ID/Passport).", null);

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

                var b = new Beneficiary
                {
                    IdentifierType = vm.IdentifierType,
                    IdentifierValue = idVal,
                    FirstName = vm.FirstName.Trim(),
                    MiddleName = string.IsNullOrWhiteSpace(vm.MiddleName) ? null : vm.MiddleName.Trim(),
                    LastName = vm.LastName.Trim(),
                    DateOfBirth = vm.DateOfBirth,

                    GenderId = vm.GenderId,
                    RaceId = vm.RaceId,
                    CitizenshipStatusId = vm.CitizenshipStatusId,
                    DisabilityStatusId = vm.DisabilityStatusId,
                    DisabilityTypeId = vm.DisabilityTypeId,
                    EducationLevelId = vm.EducationLevelId,
                    EmploymentStatusId = vm.EmploymentStatusId,

                    Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim(),
                    MobileNumber = vm.MobileNumber.Trim(),
                    AltNumber = string.IsNullOrWhiteSpace(vm.AltNumber) ? null : vm.AltNumber.Trim(),

                    // Consent moved to beneficiary registration:
                    ConsentGiven = false,
                    ConsentDate = DateTime.MinValue,
                    RegistrationStatus = BeneficiaryRegistrationStatus.AddedByAdmin,

                    IsActive = vm.IsActive,
                    Address = addr,

                    CreatedOnUtc = DateTime.UtcNow,
                    CreatedByUserId = _user.UserId
                };

                _db.Beneficiaries.Add(b);
                await _db.SaveChangesAsync(ct);

                return (true, null, b.Id);
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
                using var wb = new ClosedXML.Excel.XLWorkbook(stream);
                var ws = wb.Worksheets.FirstOrDefault();

                if (ws == null)
                {
                    result.Errors.Add("Excel file has no worksheet.");
                    return result;
                }

                // Expected headers (Citizenship/Disability/Education derived if not in Excel):
                // IdentifierType | IdentifierValue | FirstName | LastName | DateOfBirth | Gender | Email | MobileNumber | Province | City | AddressLine1 | PostalCode | IsActive
                var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

                var headers = ws.Row(1).CellsUsed().ToDictionary(
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
                var cMob = Col("MobileNumber");
                var cProv = Col("Province");
                var cCity = Col("City");
                var cAddr = Col("AddressLine1");
                var cPost = Col("PostalCode");
                var cAct = Col("IsActive");

                if (cType < 0 || cVal < 0 || cFn < 0 || cLn < 0)
                {
                    result.Errors.Add("Missing required headers. Required: IdentifierType, IdentifierValue, FirstName, LastName.");
                    return result;
                }

                if (cDob < 0)
                {
                    result.Errors.Add("Missing required header: DateOfBirth.");
                    return result;
                }

                if (cGen < 0)
                {
                    result.Errors.Add("Missing required header: Gender.");
                    return result;
                }

                if (cProv < 0 || cCity < 0 || cAddr < 0 || cPost < 0)
                {
                    result.Errors.Add("Missing required address headers. Required: Province, City, AddressLine1, PostalCode.");
                    return result;
                }

                // Lookups
                var genders = await _db.Genders
                    .Where(x => x.IsActive)
                    .Select(x => new { x.Id, x.Name })
                    .ToListAsync(ct);

                var provinces = await _db.Provinces
                    .Where(x => x.IsActive)
                    .Select(x => new { x.Id, x.Name })
                    .ToListAsync(ct);

                var citizenshipStatuses = await _db.CitizenshipStatuses
                    .Where(x => x.IsActive)
                    .Select(x => new { x.Id, x.Name })
                    .ToListAsync(ct);

                var disabilityStatuses = await _db.DisabilityStatuses
                    .Where(x => x.IsActive)
                    .Select(x => new { x.Id, x.Name })
                    .ToListAsync(ct);

                var educationLevels = await _db.EducationLevels
                    .Where(x => x.IsActive)
                    .Select(x => new { x.Id, x.Name })
                    .ToListAsync(ct);

                var genderByName = genders
                    .GroupBy(x => (x.Name ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

                var provinceByName = provinces
                    .GroupBy(x => (x.Name ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

                var citizenshipByName = citizenshipStatuses
                    .GroupBy(x => (x.Name ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

                var disabilityByName = disabilityStatuses
                    .GroupBy(x => (x.Name ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

                var educationByName = educationLevels
                    .GroupBy(x => (x.Name ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

                // ✅ EXACT names from your LookupSeeder
                const string SA_CIT_NAME = "South African Citizen";
                const string FOREIGN_CIT_NAME = "Foreign National";
                const string DIS_NO_NAME = "No";
                const string EDU_DEFAULT_NAME = "Matric";

                if (!citizenshipByName.TryGetValue(SA_CIT_NAME, out var saCitizenId))
                {
                    result.Errors.Add($"Missing CitizenshipStatus lookup: '{SA_CIT_NAME}'. Run LookupSeeder.SeedAsync().");
                    return result;
                }

                if (!citizenshipByName.TryGetValue(FOREIGN_CIT_NAME, out var foreignCitizenId))
                {
                    result.Errors.Add($"Missing CitizenshipStatus lookup: '{FOREIGN_CIT_NAME}'. Run LookupSeeder.SeedAsync().");
                    return result;
                }

                if (!disabilityByName.TryGetValue(DIS_NO_NAME, out var disabilityNoId))
                {
                    result.Errors.Add($"Missing DisabilityStatus lookup: '{DIS_NO_NAME}'. Run LookupSeeder.SeedAsync().");
                    return result;
                }

                if (!educationByName.TryGetValue(EDU_DEFAULT_NAME, out var defaultEduId))
                {
                    result.Errors.Add($"Missing EducationLevel lookup: '{EDU_DEFAULT_NAME}'. Run LookupSeeder.SeedAsync().");
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

                static DateTime? TryReadDate(ClosedXML.Excel.IXLCell cell)
                {
                    if (cell == null) return null;

                    if (cell.DataType == ClosedXML.Excel.XLDataType.DateTime)
                        return cell.GetDateTime().Date;

                    var s = (cell.GetString() ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(s)) return null;

                    if (DateTime.TryParse(s, out var dt))
                        return dt.Date;

                    return null;
                }

                using var tx = await _db.Database.BeginTransactionAsync(ct);

                for (int r = 2; r <= lastRow; r++)
                {
                    var row = ws.Row(r);

                    var typeStr = row.Cell(cType).GetString()?.Trim() ?? "";
                    var idVal = row.Cell(cVal).GetString()?.Trim() ?? "";
                    var fn = row.Cell(cFn).GetString()?.Trim() ?? "";
                    var ln = row.Cell(cLn).GetString()?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(typeStr) && string.IsNullOrWhiteSpace(idVal) &&
                        string.IsNullOrWhiteSpace(fn) && string.IsNullOrWhiteSpace(ln))
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

                    // ✅ Derived required lookups
                    var citizenshipId = (idType == IdentifierType.SaId) ? saCitizenId : foreignCitizenId;
                    var disabilityStatusId = disabilityNoId;
                    var educationLevelId = defaultEduId;

                    var parsedDob = TryReadDate(row.Cell(cDob));
                    if (!parsedDob.HasValue)
                    {
                        result.Errors.Add($"Row {r}: DateOfBirth is required and must be a valid date.");
                        result.Skipped++;
                        continue;
                    }
                    var dob = parsedDob.Value;

                    var genderName = row.Cell(cGen).GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(genderName))
                    {
                        result.Errors.Add($"Row {r}: Gender is required.");
                        result.Skipped++;
                        continue;
                    }

                    if (!genderByName.TryGetValue(genderName, out var genderId))
                    {
                        result.Errors.Add($"Row {r}: Unknown Gender '{genderName}'.");
                        result.Skipped++;
                        continue;
                    }

                    var email = (cEmail > 0) ? row.Cell(cEmail).GetString()?.Trim() : null;
                    var mobile = (cMob > 0) ? row.Cell(cMob).GetString()?.Trim() : null;

                    var provName = row.Cell(cProv).GetString()?.Trim();
                    var city = row.Cell(cCity).GetString()?.Trim();
                    var addr1 = row.Cell(cAddr).GetString()?.Trim();
                    var postal = row.Cell(cPost).GetString()?.Trim();

                    if (string.IsNullOrWhiteSpace(provName) || string.IsNullOrWhiteSpace(city) ||
                        string.IsNullOrWhiteSpace(addr1) || string.IsNullOrWhiteSpace(postal))
                    {
                        result.Errors.Add($"Row {r}: Province, City, AddressLine1, PostalCode are required.");
                        result.Skipped++;
                        continue;
                    }

                    if (!provinceByName.TryGetValue(provName!, out var provinceId))
                    {
                        result.Errors.Add($"Row {r}: Unknown Province '{provName}'.");
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

                    var existing = await _db.Beneficiaries
                        .Include(x => x.Address)
                        .FirstOrDefaultAsync(x =>
                            x.IdentifierType == idType &&
                            x.IdentifierValue == idVal, ct);

                    if (existing != null)
                    {
                        existing.FirstName = fn;
                        existing.LastName = ln;
                        existing.DateOfBirth = dob;

                        existing.GenderId = genderId;
                        existing.CitizenshipStatusId = citizenshipId;
                        existing.DisabilityStatusId = disabilityStatusId;
                        existing.EducationLevelId = educationLevelId; // ✅ NEW

                        if (!string.IsNullOrWhiteSpace(email)) existing.Email = email;
                        if (!string.IsNullOrWhiteSpace(mobile)) existing.MobileNumber = mobile;

                        existing.IsActive = isActive;
                        existing.UpdatedOnUtc = DateTime.UtcNow;
                        existing.UpdatedByUserId = _user.UserId;

                        if (existing.Address == null)
                        {
                            existing.Address = new Address
                            {
                                AddressLine1 = addr1!,
                                City = city!,
                                PostalCode = postal!,
                                ProvinceId = provinceId,
                                CreatedOnUtc = DateTime.UtcNow,
                                CreatedByUserId = _user.UserId
                            };
                        }
                        else
                        {
                            existing.Address.AddressLine1 = addr1!;
                            existing.Address.City = city!;
                            existing.Address.PostalCode = postal!;
                            existing.Address.ProvinceId = provinceId;

                            existing.Address.UpdatedOnUtc = DateTime.UtcNow;
                            existing.Address.UpdatedByUserId = _user.UserId;
                        }

                        result.Updated++;
                    }
                    else
                    {
                        var addrEntity = new Address
                        {
                            AddressLine1 = addr1!,
                            City = city!,
                            PostalCode = postal!,
                            ProvinceId = provinceId,
                            CreatedOnUtc = DateTime.UtcNow,
                            CreatedByUserId = _user.UserId
                        };

                        var bNew = new Beneficiary
                        {
                            IdentifierType = idType,
                            IdentifierValue = idVal,
                            FirstName = fn,
                            LastName = ln,
                            DateOfBirth = dob,

                            GenderId = genderId,
                            CitizenshipStatusId = citizenshipId,
                            DisabilityStatusId = disabilityStatusId,
                            EducationLevelId = educationLevelId, // ✅ NEW

                            Email = string.IsNullOrWhiteSpace(email) ? null : email,
                            MobileNumber = string.IsNullOrWhiteSpace(mobile) ? "" : mobile,

                            IsActive = isActive,
                            Address = addrEntity,

                            CreatedOnUtc = DateTime.UtcNow,
                            CreatedByUserId = _user.UserId
                        };

                        _db.Beneficiaries.Add(bNew);
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