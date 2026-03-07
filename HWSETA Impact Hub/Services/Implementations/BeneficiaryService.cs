namespace HWSETA_Impact_Hub.Services.Implementations
{
    using ClosedXML.Excel;
    using global::HWSETA_Impact_Hub.Data;
    using global::HWSETA_Impact_Hub.Domain.Entities;
    using global::HWSETA_Impact_Hub.Infrastructure.Encryption;
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
            private readonly IAesEncryptionService _enc;

            public BeneficiaryService(
                ApplicationDbContext db,
                ICurrentUserService user,
                IAesEncryptionService enc)
            {
                _db = db;
                _user = user;
                _enc = enc;
            }

            public async Task<List<BeneficiaryListVm>> ListAsync(CancellationToken ct)
            {
                var list = await _db.Beneficiaries
                    .AsNoTracking()
                    .Select(x => new BeneficiaryListVm
                    {
                        Id = x.Id,
                        IdentifierType = x.IdentifierType.ToString(),
                        IdentifierValue = x.IdentifierValue ?? "",
                        FullName = ((x.FirstName ?? "") + " " + (x.LastName ?? "")).Trim(),
                        Email = x.Email ?? "",
                        MobileNumber = x.MobileNumber ?? "",
                        Province = x.Address != null && x.Address.Province != null
                            ? (x.Address.Province.Name ?? "")
                            : "",
                        City = x.Address != null
                            ? (x.Address.City ?? "")
                            : "",
                        RegistrationStatus = x.RegistrationStatus.ToString(),
                        IsActive = x.IsActive
                    })
                    .OrderBy(x => x.FullName)
                    .ToListAsync(ct);

                return list;
            }

            public async Task<(bool ok, string? error, Guid? beneficiaryId)> CreateAsync(BeneficiaryCreateVm vm, CancellationToken ct)
            {
                var idVal = vm.IdentifierValue.Trim();
                var idHash = _enc.BlindIndex(idVal) ?? "";
                var emailVal = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim();

                // Duplicate check uses the blind index (IdentifierValue is encrypted)
                var exists = await _db.Beneficiaries.AnyAsync(x =>
                    x.IdentifierType == vm.IdentifierType &&
                    x.IdentifierValueHash == idHash, ct);

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
                    IdentifierValue = idVal,        // encrypted by EF Value Converter
                    IdentifierValueHash = idHash,       // blind index for lookups/unique constraint
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

                    Email = emailVal,             // encrypted by EF Value Converter
                    EmailHash = _enc.BlindIndex(emailVal),
                    MobileNumber = vm.MobileNumber.Trim(),
                    AltNumber = string.IsNullOrWhiteSpace(vm.AltNumber) ? null : vm.AltNumber.Trim(),

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

                // -----------------------------
                // Load lookups
                // -----------------------------
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

                var races = await _db.Races
                    .Where(x => x.IsActive)
                    .Select(x => new { x.Id, x.Name })
                    .ToListAsync(ct);

                var employmentStatuses = await _db.EmploymentStatuses
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

                var raceByName = races
                    .GroupBy(x => (x.Name ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

                var employmentByName = employmentStatuses
                    .GroupBy(x => (x.Name ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

                // -----------------------------
                // Default lookup names
                // Replace these if your seeder uses different names
                // -----------------------------
                const string SA_CIT_NAME = "South African Citizen";
                const string FOREIGN_CIT_NAME = "Foreign National";
                const string DIS_NO_NAME = "No";
                const string EDU_DEFAULT_NAME = "Matric";
                const string RACE_DEFAULT_NAME = "African";
                const string EMPLOYMENT_DEFAULT_NAME = "Unemployed";

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

                // Prefer exact seeded names; fallback to first active row if names differ
                Guid defaultRaceId;
                if (!raceByName.TryGetValue(RACE_DEFAULT_NAME, out defaultRaceId))
                {
                    defaultRaceId = races.Select(x => x.Id).FirstOrDefault();
                    if (defaultRaceId == Guid.Empty)
                    {
                        result.Errors.Add("Missing Race lookups. Run LookupSeeder.SeedAsync().");
                        return result;
                    }
                }

                Guid defaultEmploymentStatusId;
                if (!employmentByName.TryGetValue(EMPLOYMENT_DEFAULT_NAME, out defaultEmploymentStatusId))
                {
                    defaultEmploymentStatusId = employmentStatuses.Select(x => x.Id).FirstOrDefault();
                    if (defaultEmploymentStatusId == Guid.Empty)
                    {
                        result.Errors.Add("Missing EmploymentStatus lookups. Run LookupSeeder.SeedAsync().");
                        return result;
                    }
                }

                static bool TryParseBoolLoose(string? s, out bool value)
                {
                    value = false;
                    if (string.IsNullOrWhiteSpace(s)) return false;

                    s = s.Trim();
                    if (bool.TryParse(s, out value)) return true;

                    if (s.Equals("1") || s.Equals("yes", StringComparison.OrdinalIgnoreCase) || s.Equals("y", StringComparison.OrdinalIgnoreCase))
                    {
                        value = true;
                        return true;
                    }

                    if (s.Equals("0") || s.Equals("no", StringComparison.OrdinalIgnoreCase) || s.Equals("n", StringComparison.OrdinalIgnoreCase))
                    {
                        value = false;
                        return true;
                    }

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

                try
                {
                    for (int r = 2; r <= lastRow; r++)
                    {
                        var row = ws.Row(r);

                        var typeStr = row.Cell(cType).GetString()?.Trim() ?? "";
                        var idVal = row.Cell(cVal).GetString()?.Trim() ?? "";
                        var fn = row.Cell(cFn).GetString()?.Trim() ?? "";
                        var ln = row.Cell(cLn).GetString()?.Trim() ?? "";

                        if (string.IsNullOrWhiteSpace(typeStr) &&
                            string.IsNullOrWhiteSpace(idVal) &&
                            string.IsNullOrWhiteSpace(fn) &&
                            string.IsNullOrWhiteSpace(ln))
                        {
                            result.Skipped++;
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(typeStr) ||
                            string.IsNullOrWhiteSpace(idVal) ||
                            string.IsNullOrWhiteSpace(fn) ||
                            string.IsNullOrWhiteSpace(ln))
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

                        var email = cEmail > 0 ? row.Cell(cEmail).GetString()?.Trim() : null;
                        var mobile = cMob > 0 ? row.Cell(cMob).GetString()?.Trim() : null;

                        var provName = row.Cell(cProv).GetString()?.Trim();
                        var city = row.Cell(cCity).GetString()?.Trim();
                        var addr1 = row.Cell(cAddr).GetString()?.Trim();
                        var postal = row.Cell(cPost).GetString()?.Trim();

                        if (string.IsNullOrWhiteSpace(provName) ||
                            string.IsNullOrWhiteSpace(city) ||
                            string.IsNullOrWhiteSpace(addr1) ||
                            string.IsNullOrWhiteSpace(postal))
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

                        // Derived defaults for required FK lookups
                        var citizenshipId = idType == IdentifierType.SaId ? saCitizenId : foreignCitizenId;
                        var disabilityStatusId = disabilityNoId;
                        var educationLevelId = defaultEduId;
                        var raceId = defaultRaceId;
                        var employmentStatusId = defaultEmploymentStatusId;

                        var idHash = _enc.BlindIndex(idVal) ?? "";

                        var existing = await _db.Beneficiaries
                            .Include(x => x.Address)
                            .FirstOrDefaultAsync(x =>
                                x.IdentifierType == idType &&
                                x.IdentifierValueHash == idHash, ct);

                        if (existing != null)
                        {
                            existing.FirstName = fn;
                            existing.LastName = ln;
                            existing.DateOfBirth = dob;

                            existing.GenderId = genderId;
                            existing.RaceId = raceId;
                            existing.CitizenshipStatusId = citizenshipId;
                            existing.DisabilityStatusId = disabilityStatusId;
                            existing.EducationLevelId = educationLevelId;
                            existing.EmploymentStatusId = employmentStatusId;

                            if (!string.IsNullOrWhiteSpace(email))
                                existing.Email = email;

                            if (!string.IsNullOrWhiteSpace(mobile))
                                existing.MobileNumber = mobile;

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
                                IdentifierValueHash = idHash,
                                FirstName = fn,
                                LastName = ln,
                                DateOfBirth = dob,

                                GenderId = genderId,
                                RaceId = raceId,
                                CitizenshipStatusId = citizenshipId,
                                DisabilityStatusId = disabilityStatusId,
                                EducationLevelId = educationLevelId,
                                EmploymentStatusId = employmentStatusId,

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
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(ct);
                    result.Errors.Add(ex.InnerException?.Message ?? ex.Message);
                }

                return result;
            }

            private async Task PopulateEditDropdownsAsync(BeneficiaryEditVm vm, CancellationToken ct)
            {
                vm.Genders = await _db.Genders
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name ?? ""
                    })
                    .ToListAsync(ct);

                vm.Races = await _db.Races
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name ?? ""
                    })
                    .ToListAsync(ct);

                vm.CitizenshipStatuses = await _db.CitizenshipStatuses
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name ?? ""
                    })
                    .ToListAsync(ct);

                vm.DisabilityStatuses = await _db.DisabilityStatuses
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name ?? ""
                    })
                    .ToListAsync(ct);

                vm.DisabilityTypes = await _db.DisabilityTypes
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name ?? ""
                    })
                    .ToListAsync(ct);

                vm.EducationLevels = await _db.EducationLevels
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name ?? ""
                    })
                    .ToListAsync(ct);

                vm.EmploymentStatuses = await _db.EmploymentStatuses
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name ?? ""
                    })
                    .ToListAsync(ct);

                vm.Provinces = await _db.Provinces
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name ?? ""
                    })
                    .ToListAsync(ct);
            }

            public async Task<BeneficiaryDetailsVm?> GetDetailsAsync(Guid id, CancellationToken ct)
            {
                var x = await _db.Beneficiaries
                    .AsNoTracking()
                    .Include(b => b.Gender)
                    .Include(b => b.Race)
                    .Include(b => b.CitizenshipStatus)
                    .Include(b => b.DisabilityStatus)
                    .Include(b => b.DisabilityType)
                    .Include(b => b.EducationLevel)
                    .Include(b => b.EmploymentStatus)
                    .Include(b => b.Address)
                        .ThenInclude(a => a.Province)
                    .FirstOrDefaultAsync(b => b.Id == id, ct);

                if (x == null) return null;

                var first = x.FirstName ?? "";
                var middle = x.MiddleName ?? "";
                var last = x.LastName ?? "";

                return new BeneficiaryDetailsVm
                {
                    Id = x.Id,
                    IdentifierType = x.IdentifierType.ToString(),
                    IdentifierValue = x.IdentifierValue ?? "",
                    FirstName = first,
                    MiddleName = middle,
                    LastName = last,
                    FullName = string.Join(" ", new[] { first, middle, last }.Where(s => !string.IsNullOrWhiteSpace(s))),
                    DateOfBirth = x.DateOfBirth,

                    Gender = x.Gender?.Name ?? "",
                    Race = x.Race?.Name ?? "",
                    CitizenshipStatus = x.CitizenshipStatus?.Name ?? "",
                    DisabilityStatus = x.DisabilityStatus?.Name ?? "",
                    DisabilityType = x.DisabilityType?.Name ?? "",
                    EducationLevel = x.EducationLevel?.Name ?? "",
                    EmploymentStatus = x.EmploymentStatus?.Name ?? "",

                    Email = x.Email ?? "",
                    MobileNumber = x.MobileNumber ?? "",
                    AltNumber = x.AltNumber ?? "",
                    Phone = x.Phone ?? "",

                    Province = x.Address?.Province?.Name ?? x.Province ?? "",
                    City = x.Address?.City ?? x.City ?? "",
                    AddressLine1 = x.Address?.AddressLine1 ?? x.AddressLine1 ?? "",
                    PostalCode = x.Address?.PostalCode ?? x.PostalCode ?? "",

                    ConsentGiven = x.ConsentGiven,
                    ConsentDate = x.ConsentDate == DateTime.MinValue ? null : x.ConsentDate,

                    RegistrationStatus = x.RegistrationStatus.ToString(),
                    InvitedAt = x.InvitedAt,
                    PasswordSetAt = x.PasswordSetAt,
                    LocationCapturedAt = x.LocationCapturedAt,
                    RegistrationSubmittedAt = x.RegistrationSubmittedAt,

                    Latitude = x.Latitude,
                    Longitude = x.Longitude,

                    ProofOfCompletionPath = x.ProofOfCompletionPath ?? "",
                    ProofUploadedAt = x.ProofUploadedAt,

                    Programme = x.Programme ?? "",
                    TrainingProvider = x.TrainingProvider ?? "",
                    Employer = x.Employer ?? "",

                    IsActive = x.IsActive,

                    CreatedOnUtc = x.CreatedOnUtc,
                    CreatedByUserId = x.CreatedByUserId ?? "",
                    UpdatedOnUtc = x.UpdatedOnUtc,
                    UpdatedByUserId = x.UpdatedByUserId ?? ""
                };
            }

            public async Task<BeneficiaryEditVm?> GetEditAsync(Guid id, CancellationToken ct)
            {
                var x = await _db.Beneficiaries
                    .AsNoTracking()
                    .Include(b => b.Address)
                    .FirstOrDefaultAsync(b => b.Id == id, ct);

                if (x == null) return null;

                var vm = new BeneficiaryEditVm
                {
                    Id = x.Id,
                    IdentifierType = x.IdentifierType,
                    IdentifierValue = x.IdentifierValue ?? "",
                    FirstName = x.FirstName ?? "",
                    MiddleName = x.MiddleName,
                    LastName = x.LastName ?? "",
                    DateOfBirth = x.DateOfBirth,

                    GenderId = x.GenderId,
                    RaceId = x.RaceId,
                    CitizenshipStatusId = x.CitizenshipStatusId,
                    DisabilityStatusId = x.DisabilityStatusId,
                    DisabilityTypeId = x.DisabilityTypeId,
                    EducationLevelId = x.EducationLevelId,
                    EmploymentStatusId = x.EmploymentStatusId,

                    Email = x.Email,
                    MobileNumber = x.MobileNumber ?? "",
                    AltNumber = x.AltNumber,
                    Phone = x.Phone,

                    ProvinceId = x.Address?.ProvinceId ?? Guid.Empty,
                    City = x.Address?.City ?? x.City ?? "",
                    AddressLine1 = x.Address?.AddressLine1 ?? x.AddressLine1 ?? "",
                    PostalCode = x.Address?.PostalCode ?? x.PostalCode ?? "",

                    IsActive = x.IsActive,
                    Programme = x.Programme,
                    TrainingProvider = x.TrainingProvider,
                    Employer = x.Employer
                };

                await PopulateEditDropdownsAsync(vm, ct);
                return vm;
            }

            public async Task<(bool ok, string? error)> UpdateAsync(BeneficiaryEditVm vm, CancellationToken ct)
            {
                var entity = await _db.Beneficiaries
                    .Include(x => x.Address)
                    .FirstOrDefaultAsync(x => x.Id == vm.Id, ct);

                if (entity == null)
                    return (false, "Beneficiary not found.");

                var idVal = (vm.IdentifierValue ?? "").Trim();
                if (string.IsNullOrWhiteSpace(idVal))
                    return (false, "Identifier Value is required.");

                var idHash = _enc.BlindIndex(idVal) ?? "";

                var duplicateExists = await _db.Beneficiaries
                    .AnyAsync(x =>
                        x.Id != vm.Id &&
                        x.IdentifierType == vm.IdentifierType &&
                        x.IdentifierValueHash == idHash, ct);

                if (duplicateExists)
                    return (false, "Another beneficiary already exists with the same ID/Passport.");

                entity.IdentifierType = vm.IdentifierType;
                entity.IdentifierValue = idVal;
                entity.IdentifierValueHash = idHash;

                entity.FirstName = (vm.FirstName ?? "").Trim();
                entity.MiddleName = string.IsNullOrWhiteSpace(vm.MiddleName) ? null : vm.MiddleName.Trim();
                entity.LastName = (vm.LastName ?? "").Trim();
                entity.DateOfBirth = vm.DateOfBirth;

                entity.GenderId = vm.GenderId;
                entity.RaceId = vm.RaceId;
                entity.CitizenshipStatusId = vm.CitizenshipStatusId;
                entity.DisabilityStatusId = vm.DisabilityStatusId;
                entity.DisabilityTypeId = vm.DisabilityTypeId;
                entity.EducationLevelId = vm.EducationLevelId;
                entity.EmploymentStatusId = vm.EmploymentStatusId;

                entity.Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim();
                entity.EmailHash = _enc.BlindIndex(entity.Email);
                entity.MobileNumber = string.IsNullOrWhiteSpace(vm.MobileNumber) ? "" : vm.MobileNumber.Trim();
                entity.AltNumber = string.IsNullOrWhiteSpace(vm.AltNumber) ? null : vm.AltNumber.Trim();
                entity.Phone = string.IsNullOrWhiteSpace(vm.Phone) ? null : vm.Phone.Trim();

                entity.IsActive = vm.IsActive;
                entity.Programme = string.IsNullOrWhiteSpace(vm.Programme) ? null : vm.Programme.Trim();
                entity.TrainingProvider = string.IsNullOrWhiteSpace(vm.TrainingProvider) ? null : vm.TrainingProvider.Trim();
                entity.Employer = string.IsNullOrWhiteSpace(vm.Employer) ? null : vm.Employer.Trim();

                if (entity.Address == null)
                {
                    entity.Address = new Address
                    {
                        AddressLine1 = (vm.AddressLine1 ?? "").Trim(),
                        City = (vm.City ?? "").Trim(),
                        PostalCode = (vm.PostalCode ?? "").Trim(),
                        ProvinceId = vm.ProvinceId,
                        CreatedOnUtc = DateTime.UtcNow,
                        CreatedByUserId = _user.UserId
                    };
                }
                else
                {
                    entity.Address.AddressLine1 = (vm.AddressLine1 ?? "").Trim();
                    entity.Address.City = (vm.City ?? "").Trim();
                    entity.Address.PostalCode = (vm.PostalCode ?? "").Trim();
                    entity.Address.ProvinceId = vm.ProvinceId;
                    entity.Address.UpdatedOnUtc = DateTime.UtcNow;
                    entity.Address.UpdatedByUserId = _user.UserId;
                }

                entity.Province = null;
                entity.City = null;
                entity.AddressLine1 = null;
                entity.PostalCode = null;

                entity.UpdatedOnUtc = DateTime.UtcNow;
                entity.UpdatedByUserId = _user.UserId;

                await _db.SaveChangesAsync(ct);
                return (true, null);
            }

            public async Task<(bool ok, string? error)> SetActiveAsync(Guid id, bool isActive, CancellationToken ct)
            {
                var entity = await _db.Beneficiaries.FirstOrDefaultAsync(x => x.Id == id, ct);
                if (entity == null)
                    return (false, "Beneficiary not found.");

                entity.IsActive = isActive;
                entity.UpdatedOnUtc = DateTime.UtcNow;
                entity.UpdatedByUserId = _user.UserId;

                await _db.SaveChangesAsync(ct);
                return (true, null);
            }
        }


    }
}