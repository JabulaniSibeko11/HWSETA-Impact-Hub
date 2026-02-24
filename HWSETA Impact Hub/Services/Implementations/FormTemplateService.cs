using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Models.ViewModels.Forms;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class FormTemplateService : IFormTemplateService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _user;

        public FormTemplateService(ApplicationDbContext db, ICurrentUserService user)
        {
            _db = db;
            _user = user;
        }

        public Task<List<FormTemplateListRowVm>> ListAsync1(CancellationToken ct) =>
            _db.FormTemplates.AsNoTracking()
                .OrderByDescending(x => x.CreatedOnUtc)
                .Select(x => new FormTemplateListRowVm
                {
                    Id = x.Id,
                    Title = x.Title,
                    Status = x.Status,
                    Version = x.Version,
                    IsActive = x.IsActive,
                    CreatedOnUtc = x.CreatedOnUtc
                })
                .ToListAsync(ct);


        public async Task<List<FormTemplateListRowVm>> ListAsync(CancellationToken ct)
        {
            var templates = await _db.FormTemplates.AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Purpose,               // ✅ ADD THIS
                    StatusEnum = x.Status,   // ✅ keep as enum (easier than string)
                    x.Version,
                    x.IsActive,
                    x.CreatedOnUtc
                })
                .ToListAsync(ct);

            var ids = templates.Select(x => x.Id).ToList();

            var publishes = await _db.FormPublishes.AsNoTracking()
                .Where(p => ids.Contains(p.FormTemplateId))
                .Select(p => new
                {
                    p.FormTemplateId,
                    p.IsPublished,
                    p.PublicToken
                })
                .ToListAsync(ct);

            var pubByTemplate = publishes
                .GroupBy(x => x.FormTemplateId)
                .ToDictionary(g => g.Key, g => g.First());

            // ✅ ORDER AFTER we have publish info
            var ordered = templates
                .OrderByDescending(t => t.Purpose == FormPurpose.Registration) // Registration pinned on top
                .ThenByDescending(t =>
                {
                    pubByTemplate.TryGetValue(t.Id, out var pub);
                    return pub?.IsPublished ?? false; // Published first
                })
                .ThenByDescending(t => t.CreatedOnUtc);

            return ordered.Select(t =>
            {
                pubByTemplate.TryGetValue(t.Id, out var pub);

                return new FormTemplateListRowVm
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.StatusEnum,
                    Version = t.Version,
                    IsActive = t.IsActive,
                    CreatedOnUtc = t.CreatedOnUtc,

                    IsPublished = pub?.IsPublished ?? false,
                    PublicToken = pub?.PublicToken
                };
            }).ToList();
        }
        public Task<FormTemplate?> GetEntityAsync(Guid id, CancellationToken ct) =>
            _db.FormTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<(bool ok, string? error, Guid? id)> CreateAsync(FormTemplateCreateVm vm, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(vm.Title))
                return (false, "Title is required.", null);

            var title = vm.Title.Trim();

            var entity = new FormTemplate
            {
                Title = title,
                Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
                Purpose = vm.Purpose, // ✅ SAVE TO DB
                Status = FormStatus.Draft,
                Version = 1,
                IsActive = true,
                CreatedOnUtc = DateTime.UtcNow,
                CreatedByUserId = _user.UserId
            };

            _db.FormTemplates.Add(entity);
            await _db.SaveChangesAsync(ct);

            // auto create 1 default section (nice UX)
            var sec = new FormSection
            {
                FormTemplateId = entity.Id,
                Title = "Section 1",
                SortOrder = 1,
                CreatedOnUtc = DateTime.UtcNow,
                CreatedByUserId = _user.UserId
            };
            _db.FormSections.Add(sec);
            await _db.SaveChangesAsync(ct);

            return (true, null, entity.Id);
        }

        public async Task<(bool ok, string? error)> UpdateAsync(FormTemplateEditVm vm, CancellationToken ct)
        {
            var entity = await _db.FormTemplates.FirstOrDefaultAsync(x => x.Id == vm.Id, ct);
            if (entity == null) return (false, "Form template not found.");

            if (string.IsNullOrWhiteSpace(vm.Title))
                return (false, "Title is required.");

            if (entity.Status == FormStatus.Archived)
                return (false, "Archived templates cannot be edited.");

            entity.Title = vm.Title.Trim();
            entity.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();
            entity.IsActive = vm.IsActive;

            entity.UpdatedOnUtc = DateTime.UtcNow;
            entity.UpdatedByUserId = _user.UserId;

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        public async Task<FormTemplateBuilderVm?> GetBuilderAsync(Guid templateId, CancellationToken ct)
        {
            var t = await _db.FormTemplates.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == templateId, ct);

            if (t == null) return null;

            var sections = await _db.FormSections.AsNoTracking()
                .Where(x => x.FormTemplateId == templateId)
                .OrderBy(x => x.SortOrder)
                .Select(s => new FormSectionVm
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    SortOrder = s.SortOrder,
                    Fields = new List<FormFieldVm>()
                })
                .ToListAsync(ct);

            var sectionIds = sections.Select(x => x.Id).ToList();

            var fields = await _db.FormFields.AsNoTracking()
                .Where(f => sectionIds.Contains(f.FormSectionId) && f.IsActive)
                .OrderBy(f => f.SortOrder)
                .Select(f => new FormFieldVm
                {
                    Id = f.Id,
                    FormSectionId = f.FormSectionId,
                    Label = f.Label,
                    HelpText = f.HelpText,
                    FieldType = f.FieldType,
                    IsRequired = f.IsRequired,
                    SortOrder = f.SortOrder,
                    MaxLength = f.MaxLength,
                    MinInt = f.MinInt,
                    MaxInt = f.MaxInt,
                    MinDecimal = f.MinDecimal,
                    MaxDecimal = f.MaxDecimal,
                    RegexPattern = f.RegexPattern,
                    SettingsJson = f.SettingsJson,
                    Options = new List<FormFieldOptionVm>()
                })
                .ToListAsync(ct);

            var fieldIds = fields.Select(x => x.Id).ToList();

            var options = await _db.FormFieldOptions.AsNoTracking()
                .Where(o => fieldIds.Contains(o.FormFieldId) && o.IsActive)
                .OrderBy(o => o.SortOrder)
                .Select(o => new FormFieldOptionVm
                {
                    Id = o.Id,
                    FormFieldId = o.FormFieldId,
                    Value = o.Value,
                    Text = o.Text,
                    SortOrder = o.SortOrder
                })
                .ToListAsync(ct);

            var optionsByField = options.GroupBy(x => x.FormFieldId).ToDictionary(g => g.Key, g => g.ToList());
            var fieldsBySection = fields.GroupBy(x => x.FormSectionId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var s in sections)
            {
                if (fieldsBySection.TryGetValue(s.Id, out var sf))
                {
                    foreach (var f in sf)
                    {
                        if (optionsByField.TryGetValue(f.Id, out var fo))
                            f.Options = fo;
                    }
                    s.Fields = sf.OrderBy(x => x.SortOrder).ToList();
                }
            }

            return new FormTemplateBuilderVm
            {
                TemplateId = t.Id,
                Title = t.Title,
                Status = t.Status,
                Version = t.Version,
                IsActive = t.IsActive,
                Sections = sections
            };
        }

        // ---------------- Sections ----------------
        public async Task<(bool ok, string? error, Guid? sectionId)> AddSectionAsync(FormSectionCreateVm vm, CancellationToken ct)
        {
            if (vm.FormTemplateId == Guid.Empty) return (false, "Invalid template.", null);
            if (string.IsNullOrWhiteSpace(vm.Title)) return (false, "Section title is required.", null);

            var exists = await _db.FormTemplates.AnyAsync(x => x.Id == vm.FormTemplateId, ct);
            if (!exists) return (false, "Template not found.", null);

            var nextOrder = await _db.FormSections
                .Where(x => x.FormTemplateId == vm.FormTemplateId)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync(ct) ?? 0;

            var entity = new FormSection
            {
                FormTemplateId = vm.FormTemplateId,
                Title = vm.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
                SortOrder = nextOrder + 1,
                CreatedOnUtc = DateTime.UtcNow,
                CreatedByUserId = _user.UserId
            };

            _db.FormSections.Add(entity);
            await _db.SaveChangesAsync(ct);
            return (true, null, entity.Id);
        }

        public async Task<(bool ok, string? error)> UpdateSectionAsync(FormSectionEditVm vm, CancellationToken ct)
        {
            var s = await _db.FormSections.FirstOrDefaultAsync(x => x.Id == vm.Id, ct);
            if (s == null) return (false, "Section not found.");

            if (string.IsNullOrWhiteSpace(vm.Title)) return (false, "Section title is required.");

            s.Title = vm.Title.Trim();
            s.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();
            s.UpdatedOnUtc = DateTime.UtcNow;
            s.UpdatedByUserId = _user.UserId;

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        public async Task<(bool ok, string? error)> DeleteSectionAsync(Guid sectionId, CancellationToken ct)
        {
            var s = await _db.FormSections.FirstOrDefaultAsync(x => x.Id == sectionId, ct);
            if (s == null) return (false, "Section not found.");

            var hasFields = await _db.FormFields.AnyAsync(x => x.FormSectionId == sectionId && x.IsActive, ct);
            if (hasFields) return (false, "Delete fields first before deleting this section.");

            _db.FormSections.Remove(s);
            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        public async Task<(bool ok, string? error)> ReorderSectionsAsync(Guid templateId, List<Guid> orderedSectionIds, CancellationToken ct)
        {
            var sections = await _db.FormSections.Where(x => x.FormTemplateId == templateId).ToListAsync(ct);
            var map = sections.ToDictionary(x => x.Id, x => x);

            int order = 1;
            foreach (var id in orderedSectionIds)
            {
                if (!map.TryGetValue(id, out var s)) continue;
                s.SortOrder = order++;
                s.UpdatedOnUtc = DateTime.UtcNow;
                s.UpdatedByUserId = _user.UserId;
            }

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        // ---------------- Fields ----------------
        public async Task<(bool ok, string? error, Guid? fieldId)> AddFieldAsync(FormFieldCreateVm vm, CancellationToken ct)
        {
            if (vm.FormSectionId == Guid.Empty) return (false, "Invalid section.", null);
            if (string.IsNullOrWhiteSpace(vm.Label)) return (false, "Field label is required.", null);

            var section = await _db.FormSections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == vm.FormSectionId, ct);
            if (section == null) return (false, "Section not found.", null);

            var nextOrder = await _db.FormFields
                .Where(x => x.FormSectionId == vm.FormSectionId)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync(ct) ?? 0;

            var entity = new FormField
            {
                FormSectionId = vm.FormSectionId,
                Label = vm.Label.Trim(),
                HelpText = string.IsNullOrWhiteSpace(vm.HelpText) ? null : vm.HelpText.Trim(),
                FieldType = vm.FieldType,
                IsRequired = vm.IsRequired,
                SortOrder = nextOrder + 1,

                MaxLength = vm.MaxLength,
                MinInt = vm.MinInt,
                MaxInt = vm.MaxInt,
                MinDecimal = vm.MinDecimal,
                MaxDecimal = vm.MaxDecimal,
                RegexPattern = string.IsNullOrWhiteSpace(vm.RegexPattern) ? null : vm.RegexPattern.Trim(),
                SettingsJson = string.IsNullOrWhiteSpace(vm.SettingsJson) ? null : vm.SettingsJson.Trim(),

                IsActive = true,
                CreatedOnUtc = DateTime.UtcNow,
                CreatedByUserId = _user.UserId
            };

            _db.FormFields.Add(entity);
            await _db.SaveChangesAsync(ct);
            return (true, null, entity.Id);
        }

        public async Task<(bool ok, string? error)> UpdateFieldAsync(FormFieldEditVm vm, CancellationToken ct)
        {
            var f = await _db.FormFields.FirstOrDefaultAsync(x => x.Id == vm.Id, ct);
            if (f == null) return (false, "Field not found.");

            if (string.IsNullOrWhiteSpace(vm.Label)) return (false, "Field label is required.");

            f.Label = vm.Label.Trim();
            f.HelpText = string.IsNullOrWhiteSpace(vm.HelpText) ? null : vm.HelpText.Trim();
            f.FieldType = vm.FieldType;
            f.IsRequired = vm.IsRequired;

            f.MaxLength = vm.MaxLength;
            f.MinInt = vm.MinInt;
            f.MaxInt = vm.MaxInt;
            f.MinDecimal = vm.MinDecimal;
            f.MaxDecimal = vm.MaxDecimal;
            f.RegexPattern = string.IsNullOrWhiteSpace(vm.RegexPattern) ? null : vm.RegexPattern.Trim();
            f.SettingsJson = string.IsNullOrWhiteSpace(vm.SettingsJson) ? null : vm.SettingsJson.Trim();

            f.UpdatedOnUtc = DateTime.UtcNow;
            f.UpdatedByUserId = _user.UserId;

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        public async Task<(bool ok, string? error)> DeleteFieldAsync(Guid fieldId, CancellationToken ct)
        {
            var f = await _db.FormFields.FirstOrDefaultAsync(x => x.Id == fieldId, ct);
            if (f == null) return (false, "Field not found.");

            // soft-delete to preserve answers later
            f.IsActive = false;
            f.UpdatedOnUtc = DateTime.UtcNow;
            f.UpdatedByUserId = _user.UserId;

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        public async Task<(bool ok, string? error)> ReorderFieldsAsync(Guid sectionId, List<Guid> orderedFieldIds, CancellationToken ct)
        {
            var fields = await _db.FormFields.Where(x => x.FormSectionId == sectionId && x.IsActive).ToListAsync(ct);
            var map = fields.ToDictionary(x => x.Id, x => x);

            int order = 1;
            foreach (var id in orderedFieldIds)
            {
                if (!map.TryGetValue(id, out var f)) continue;
                f.SortOrder = order++;
                f.UpdatedOnUtc = DateTime.UtcNow;
                f.UpdatedByUserId = _user.UserId;
            }

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        // ---------------- Options ----------------
        public async Task<(bool ok, string? error, Guid? optionId)> AddOptionAsync(FormFieldOptionCreateVm vm, CancellationToken ct)
        {
            if (vm.FormFieldId == Guid.Empty) return (false, "Invalid field.", null);
            if (string.IsNullOrWhiteSpace(vm.Text)) return (false, "Option text is required.", null);

            var f = await _db.FormFields.AsNoTracking().FirstOrDefaultAsync(x => x.Id == vm.FormFieldId, ct);
            if (f == null) return (false, "Field not found.", null);

            if (f.FieldType != FormFieldType.Dropdown && f.FieldType != FormFieldType.Radio && f.FieldType != FormFieldType.Checkbox)
                return (false, "Options only apply to Dropdown/Radio/Checkbox fields.", null);

            var nextOrder = await _db.FormFieldOptions
                .Where(x => x.FormFieldId == vm.FormFieldId)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync(ct) ?? 0;

            var value = string.IsNullOrWhiteSpace(vm.Value) ? vm.Text.Trim() : vm.Value.Trim();

            var entity = new FormFieldOption
            {
                FormFieldId = vm.FormFieldId,
                Value = value,
                Text = vm.Text.Trim(),
                SortOrder = nextOrder + 1,
                IsActive = true,
                CreatedOnUtc = DateTime.UtcNow,
                CreatedByUserId = _user.UserId
            };

            _db.FormFieldOptions.Add(entity);
            await _db.SaveChangesAsync(ct);

            return (true, null, entity.Id);
        }

        public async Task<(bool ok, string? error)> UpdateOptionAsync(FormFieldOptionEditVm vm, CancellationToken ct)
        {
            var o = await _db.FormFieldOptions.FirstOrDefaultAsync(x => x.Id == vm.Id, ct);
            if (o == null) return (false, "Option not found.");

            if (string.IsNullOrWhiteSpace(vm.Text)) return (false, "Option text is required.");

            o.Text = vm.Text.Trim();
            o.Value = string.IsNullOrWhiteSpace(vm.Value) ? o.Text : vm.Value.Trim();
            o.UpdatedOnUtc = DateTime.UtcNow;
            o.UpdatedByUserId = _user.UserId;

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        public async Task<(bool ok, string? error)> DeleteOptionAsync(Guid optionId, CancellationToken ct)
        {
            var o = await _db.FormFieldOptions.FirstOrDefaultAsync(x => x.Id == optionId, ct);
            if (o == null) return (false, "Option not found.");

            o.IsActive = false;
            o.UpdatedOnUtc = DateTime.UtcNow;
            o.UpdatedByUserId = _user.UserId;

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        public async Task<(bool ok, string? error)> ReorderOptionsAsync(Guid fieldId, List<Guid> orderedOptionIds, CancellationToken ct)
        {
            var options = await _db.FormFieldOptions.Where(x => x.FormFieldId == fieldId && x.IsActive).ToListAsync(ct);
            var map = options.ToDictionary(x => x.Id, x => x);

            int order = 1;
            foreach (var id in orderedOptionIds)
            {
                if (!map.TryGetValue(id, out var o)) continue;
                o.SortOrder = order++;
                o.UpdatedOnUtc = DateTime.UtcNow;
                o.UpdatedByUserId = _user.UserId;
            }

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }
        public async Task<FormPublishVm?> GetPublishVmAsync(Guid templateId, string baseUrl, CancellationToken ct)
        {
            var t = await _db.FormTemplates.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == templateId, ct);

            if (t == null) return null;

            // IMPORTANT: we need a FormPublish row to exist
            var p = await _db.FormPublishes
                .FirstOrDefaultAsync(x => x.FormTemplateId == templateId, ct);

            if (p == null)
            {
                // Create a draft publish row so GET Publish never breaks
                p = new FormPublish
                {
                    FormTemplateId = templateId,
                    IsPublished = false,

                    // keep token stable forever
                    PublicToken = NewPublicToken(),

                    // sensible defaults
                    IsOpen = true,
                    AllowMultipleSubmissions = true,

                    CreatedOnUtc = DateTime.UtcNow
                };

                _db.FormPublishes.Add(p);
                await _db.SaveChangesAsync(ct);
            }

            var token = p.PublicToken;
            var url = string.IsNullOrWhiteSpace(token)
                ? null
                : $"{baseUrl.TrimEnd('/')}/f/{token}";

            return new FormPublishVm
            {
                TemplateId = t.Id,
                Title = t.Title,

                IsPublished = p.IsPublished,
                Token = token,
                PublicUrl = url,

                // null-safe (these may be null by design)
                OpenFromUtc = p.OpenFromUtc,
                CloseAtUtc = p.CloseAtUtc,
                MaxSubmissions = p.MaxSubmissions,
                AllowMultipleSubmissions = p.AllowMultipleSubmissions
            };
        }

        private static string NewPublicToken()
        {
            var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(18);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
        public async Task<(bool ok, string? error)> PublishAsync(FormPublishVm vm, CancellationToken ct)
        {
            var t = await _db.FormTemplates.FirstOrDefaultAsync(x => x.Id == vm.TemplateId, ct);
            if (t == null) return (false, "Template not found.");

            // Enforce: only one Registration template in the whole system
            if (t.Purpose == FormPurpose.Registration)
            {
                var otherRegistrationExists = await _db.FormTemplates
                    .AnyAsync(x => x.Id != t.Id && x.Purpose == FormPurpose.Registration && x.IsActive, ct);

                if (otherRegistrationExists)
                    return (false, "Only one Registration FormTemplate is allowed. Edit the existing Registration template instead of creating another.");
            }

            // publish
            t.Status = FormStatus.Published;
            t.IsActive = true;
            t.UpdatedAt = DateTime.UtcNow;
            t.PublishedAt = DateTime.UtcNow;
            t.UnpublishedAt = null;

            if (string.IsNullOrWhiteSpace(t.PublicToken))
                t.PublicToken = NewPublicToken(); // keep forever so link never changes

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }
      
        
        public async Task<(bool ok, string? error)> UnpublishAsync(Guid templateId, CancellationToken ct)
        {
            var t = await _db.FormTemplates.FirstOrDefaultAsync(x => x.Id == templateId, ct);
            if (t == null) return (false, "Template not found.");

            // You may choose to disallow unpublishing Registration if you want it always available
            t.Status = FormStatus.Draft;
            t.UpdatedAt = DateTime.UtcNow;
            t.UnpublishedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        public async Task<PublicFormVm?> GetPublicFormAsync(string token, string? prefillEmail, string? prefillPhone, CancellationToken ct)
        {
            token = (token ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token)) return null;

            var pub = await _db.FormPublishes.AsNoTracking()
                .Include(x => x.FormTemplate)
                .FirstOrDefaultAsync(x => x.PublicToken == token && x.IsPublished, ct);

            if (pub == null) return null;

            var now = DateTime.UtcNow;

            // check open flag first
            if (!pub.IsOpen)
            {
                return new PublicFormVm
                {
                    Token = token,
                    TemplateId = pub.FormTemplateId,
                    Title = pub.FormTemplate.Title,
                    Description = pub.FormTemplate.Description,
                    IsOpen = false,
                    ClosedReason = "This form is currently closed."
                };
            }

            // time window check (nullable-safe)
            if (now < pub.OpenFromUtc)
            {
                return new PublicFormVm
                {
                    Token = token,
                    TemplateId = pub.FormTemplateId,
                    Title = pub.FormTemplate.Title,
                    Description = pub.FormTemplate.Description,
                    IsOpen = false,
                    ClosedReason = "This form is not open yet."
                };
            }

            if ( now > pub.CloseAtUtc)
            {
                return new PublicFormVm
                {
                    Token = token,
                    TemplateId = pub.FormTemplateId,
                    Title = pub.FormTemplate.Title,
                    Description = pub.FormTemplate.Description,
                    IsOpen = false,
                    ClosedReason = "This form is closed."
                };
            }

            if (pub.MaxSubmissions.HasValue)
            {
                var count = await _db.FormSubmissions.CountAsync(s => s.FormPublishId == pub.Id, ct);
                if (count >= pub.MaxSubmissions.Value)
                {
                    return new PublicFormVm
                    {
                        Token = token,
                        TemplateId = pub.FormTemplateId,
                        Title = pub.FormTemplate.Title,
                        Description = pub.FormTemplate.Description,
                        IsOpen = false,
                        ClosedReason = "This form has reached the maximum number of submissions."
                    };
                }
            }

            // -------------------------
            // ✅ Prefill Beneficiary (by email/phone)
            // -------------------------
            Beneficiary? ben = null;
            var email = (prefillEmail ?? "").Trim();
            var phone = (prefillPhone ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(email))
            {
                ben = await _db.Beneficiaries.AsNoTracking()
                    .Include(b => b.Gender)
                    .Include(b => b.Race)
                    .Include(b => b.CitizenshipStatus)
                    .Include(b => b.DisabilityStatus)
                    .Include(b => b.DisabilityType)
                    .Include(b => b.EducationLevel)
                    .Include(b => b.EmploymentStatus)
                    .FirstOrDefaultAsync(b => b.Email == email, ct);
            }

            if (ben == null && !string.IsNullOrWhiteSpace(phone))
            {
                ben = await _db.Beneficiaries.AsNoTracking()
                    .Include(b => b.Gender)
                    .Include(b => b.Race)
                    .Include(b => b.CitizenshipStatus)
                    .Include(b => b.DisabilityStatus)
                    .Include(b => b.DisabilityType)
                    .Include(b => b.EducationLevel)
                    .Include(b => b.EmploymentStatus)
                    .FirstOrDefaultAsync(b => b.MobileNumber == phone, ct);
            }

            BeneficiaryPrefillVm? prefill = null;
            if (ben != null)
            {
                prefill = new BeneficiaryPrefillVm
                {
                    BeneficiaryId = ben.Id,
                    IdentifierType = ben.IdentifierType.ToString(),
                    IdentifierValue = ben.IdentifierValue,
                    FirstName = ben.FirstName,
                    MiddleName = ben.MiddleName,
                    LastName = ben.LastName,
                    DateOfBirth = ben.DateOfBirth,

                    Email = ben.Email,
                    MobileNumber = ben.MobileNumber,

                    Province = ben.Province,
                    City = ben.City,
                    AddressLine1 = ben.AddressLine1,
                    PostalCode = ben.PostalCode,

                    Gender = ben.Gender?.Name ?? "",
                    Race = ben.Race?.Name ?? "",
                    CitizenshipStatus = ben.CitizenshipStatus?.Name ?? "",
                    DisabilityStatus = ben.DisabilityStatus?.Name ?? "",
                    DisabilityType = ben.DisabilityType?.Name,
                    EducationLevel = ben.EducationLevel?.Name ?? "",
                    EmploymentStatus = ben.EmploymentStatus?.Name ?? ""
                };
            }

            // -------------------------
            // Sections
            // -------------------------
            var sections = await _db.FormSections.AsNoTracking()
                .Where(s => s.FormTemplateId == pub.FormTemplateId)
                .OrderBy(s => s.SortOrder)
                .Select(s => new PublicSectionVm
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    SortOrder = s.SortOrder
                })
                .ToListAsync(ct);

            var sectionIds = sections.Select(s => s.Id).ToList();

            // Fields (✅ include FieldCode if you added it)
            var fields = await _db.FormFields.AsNoTracking()
                .Where(f => sectionIds.Contains(f.FormSectionId) && f.IsActive)
                .OrderBy(f => f.SortOrder)
                .Select(f => new
                {
                    f.Id,
                    f.FormSectionId,
                    f.Label,
                    f.HelpText,
                    f.FieldType,
                    f.IsRequired,
                    f.SortOrder,
                    f.MaxLength,
                    f.MinInt,
                    f.MaxInt,
                    f.MinDecimal,
                    f.MaxDecimal,
                    f.RegexPattern,
                    f.FieldCode
                })
                .ToListAsync(ct);

            var fieldIds = fields.Select(x => x.Id).ToList();

            var options = await _db.FormFieldOptions.AsNoTracking()
                .Where(o => fieldIds.Contains(o.FormFieldId) && o.IsActive)
                .OrderBy(o => o.SortOrder)
                .Select(o => new { o.FormFieldId, o.Value, o.Text })
                .ToListAsync(ct);

            var optByField = options
                .GroupBy(x => x.FormFieldId)
                .ToDictionary(g => g.Key,
                    g => g.Select(x => new PublicOptionVm { Value = x.Value, Text = x.Text }).ToList());

            // Helper: decide which fields should be readonly + prefilled
            bool IsBeneficiaryReadOnlyCode(string? code)
                => code is "IdentifierType" or "IdentifierValue" or "FirstName" or "LastName" or "DateOfBirth"
                    or "Gender" or "Race" or "CitizenshipStatus" or "DisabilityStatus" or "DisabilityType"
                    or "EducationLevel" or "EmploymentStatus";

            string? GetPrefillValue(string? code, BeneficiaryPrefillVm p)
                => code switch
                {
                    "IdentifierType" => p.IdentifierType,
                    "IdentifierValue" => p.IdentifierValue,
                    "FirstName" => p.FirstName,
                    "LastName" => p.LastName,
                    "DateOfBirth" => p.DateOfBirth.ToString("yyyy-MM-dd"),

                    "Gender" => p.Gender,
                    "Race" => p.Race,
                    "CitizenshipStatus" => p.CitizenshipStatus,
                    "DisabilityStatus" => p.DisabilityStatus,
                    "DisabilityType" => p.DisabilityType,
                    "EducationLevel" => p.EducationLevel,
                    "EmploymentStatus" => p.EmploymentStatus,

                    // Editable contact/address fields can also be prefilled (not readonly)
                    "Email" => p.Email,
                    "MobileNumber" => p.MobileNumber,
                    "Province" => p.Province,
                    "City" => p.City,
                    "AddressLine1" => p.AddressLine1,
                    "PostalCode" => p.PostalCode,

                    _ => null
                };

            foreach (var s in sections)
            {
                s.Questions = fields
                    .Where(f => f.FormSectionId == s.Id)
                    .OrderBy(f => f.SortOrder)
                    .Select(f =>
                    {
                        var q = new PublicQuestionVm
                        {
                            FieldId = f.Id,
                            Label = f.Label,
                            HelpText = f.HelpText,
                            FieldType = (int)f.FieldType,
                            IsRequired = f.IsRequired,
                            SortOrder = f.SortOrder,
                            MaxLength = f.MaxLength,
                            MinInt = f.MinInt,
                            MaxInt = f.MaxInt,
                            MinDecimal = f.MinDecimal,
                            MaxDecimal = f.MaxDecimal,
                            RegexPattern = f.RegexPattern,
                            Options = optByField.TryGetValue(f.Id, out var list) ? list : new List<PublicOptionVm>(),

                            FieldCode = f.FieldCode
                        };

                        // Prefill logic (only if beneficiary found)
                        if (prefill != null && !string.IsNullOrWhiteSpace(f.FieldCode))
                        {
                            q.PrefillValue = GetPrefillValue(f.FieldCode, prefill);

                            // Read-only beneficiary identity/demographics (not contact/address)
                            q.IsReadOnly = IsBeneficiaryReadOnlyCode(f.FieldCode);
                        }

                        return q;
                    })
                    .ToList();
            }

            return new PublicFormVm
            {
                Token = token,
                TemplateId = pub.FormTemplateId,
                Title = pub.FormTemplate.Title,
                Description = pub.FormTemplate.Description,
                IsOpen = true,
                Sections = sections,

                PrefillEmail = prefillEmail,
                PrefillPhone = prefillPhone,
                Prefill = prefill
            };
        }
        public async Task<(bool ok, string? error, Guid? submissionId, string? nextUrl)> SubmitPublicAsync(
        PublicFormSubmitVm vm,
        string? ip,
        string? userAgent,
        CancellationToken ct)
        {
            var token = (vm.Token ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token))
                return (false, "Invalid token.", null, null);

            // Load publish + template
            var pub = await _db.FormPublishes
                .Include(x => x.FormTemplate)
                .FirstOrDefaultAsync(x => x.PublicToken == token && x.IsPublished, ct);

            if (pub == null)
                return (false, "Form not found or not published.", null, null);

            var now = DateTime.UtcNow;

            if (!pub.IsOpen) return (false, "This form is closed.", null, null);

            if (now < pub.OpenFromUtc)
                return (false, "This form is not open yet.", null, null);

            if (now > pub.CloseAtUtc)
                return (false, "This form is closed.", null, null);

            if (pub.MaxSubmissions.HasValue)
            {
                var count = await _db.FormSubmissions.CountAsync(s => s.FormPublishId == pub.Id, ct);
                if (count >= pub.MaxSubmissions.Value)
                    return (false, "This form has reached the maximum number of submissions.", null, null);
            }

            // ✅ INVITE ENFORCEMENT
            // Registration MUST use invite token; other forms can also use invite (recommended).
            var inviteToken = (vm.InviteToken ?? "").Trim();

            BeneficiaryFormInvite? invite = null;

            if (string.IsNullOrWhiteSpace(inviteToken))
            {
                // Registration requires invite
                if (pub.FormTemplate.Purpose == FormPurpose.Registration)
                    return (false, "Invalid invite link. Please use the invite link sent to you.", null, null);
            }
            else
            {
                invite = await _db.BeneficiaryFormInvites
                    .FirstOrDefaultAsync(i => i.InviteToken == inviteToken && i.IsActive, ct);

                if (invite == null)
                    return (false, "Invite not found or not active.", null, null);

                // Invite must belong to THIS published form
                if (invite.FormPublishId != pub.Id)
                    return (false, "Invite does not match this form.", null, null);

                // ✅ Registration single submission airtight
                if (pub.FormTemplate.Purpose == FormPurpose.Registration)
                {
                    if (invite.FormSubmissionId != null || invite.CompletedAtUtc != null)
                        return (false, "Registration already submitted for this invite.", null, null);

                    // Also block if beneficiary already submitted registration (extra guard)
                    var benGuard = await _db.Beneficiaries.AsNoTracking()
                        .FirstOrDefaultAsync(b => b.Id == invite.BeneficiaryId, ct);

                    if (benGuard != null)
                    {
                        if (benGuard.RegistrationSubmittedAt != null ||
                            benGuard.RegistrationStatus >= BeneficiaryRegistrationStatus.RegistrationSubmitted)
                            return (false, "Beneficiary already submitted Registration.", null, null);
                    }
                }
            }

            // Load schema fields (✅ include FieldCode)
            var fields = await _db.FormFields.AsNoTracking()
                .Where(f => f.IsActive && f.FormSection.FormTemplateId == pub.FormTemplateId)
                .Select(f => new
                {
                    f.Id,
                    f.Label,
                    f.FieldCode,
                    f.IsRequired,
                    f.FieldType,
                    f.MaxLength,
                    f.MinInt,
                    f.MaxInt,
                    f.MinDecimal,
                    f.MaxDecimal
                })
                .ToListAsync(ct);

            // Required validation
            foreach (var f in fields.Where(x => x.IsRequired))
            {
                var hasSingle = vm.Answers.TryGetValue(f.Id, out var v) && !string.IsNullOrWhiteSpace(v);
                var hasMulti = vm.MultiAnswers.TryGetValue(f.Id, out var m) && m != null && m.Count > 0;

                if (!hasSingle && !hasMulti)
                    return (false, $"'{f.Label}' is required.", null, null);
            }

            var answers = new List<FormAnswer>();

            foreach (var f in fields)
            {
                // multi checkbox
                if (f.FieldType == FormFieldType.Checkbox)
                {
                    if (vm.MultiAnswers.TryGetValue(f.Id, out var list) && list != null && list.Count > 0)
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(list);
                        answers.Add(new FormAnswer
                        {
                            FormFieldId = f.Id,
                            ValueJson = json,
                            CreatedOnUtc = DateTime.UtcNow
                        });
                    }
                    continue;
                }

                vm.Answers.TryGetValue(f.Id, out var raw);
                raw = raw?.Trim();

                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                if (f.MaxLength.HasValue && raw.Length > f.MaxLength.Value)
                    return (false, $"'{f.Label}' exceeds max length {f.MaxLength.Value}.", null, null);

                if (f.FieldType == FormFieldType.Number)
                {
                    if (!int.TryParse(raw, out var n))
                        return (false, $"'{f.Label}' must be a whole number.", null, null);

                    if (f.MinInt.HasValue && n < f.MinInt.Value) return (false, $"'{f.Label}' must be >= {f.MinInt.Value}.", null, null);
                    if (f.MaxInt.HasValue && n > f.MaxInt.Value) return (false, $"'{f.Label}' must be <= {f.MaxInt.Value}.", null, null);
                }

                if (f.FieldType == FormFieldType.Decimal)
                {
                    if (!decimal.TryParse(raw, out var d))
                        return (false, $"'{f.Label}' must be a decimal number.", null, null);

                    if (f.MinDecimal.HasValue && d < f.MinDecimal.Value) return (false, $"'{f.Label}' must be >= {f.MinDecimal.Value}.", null, null);
                    if (f.MaxDecimal.HasValue && d > f.MaxDecimal.Value) return (false, $"'{f.Label}' must be <= {f.MaxDecimal.Value}.", null, null);
                }

                answers.Add(new FormAnswer
                {
                    FormFieldId = f.Id,
                    Value = raw,
                    CreatedOnUtc = DateTime.UtcNow
                });
            }

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            var submission = new FormSubmission
            {
                FormPublishId = pub.Id,
                SubmittedOnUtc = DateTime.UtcNow,

                SubmittedByUserId = _user.UserId, // ok even for anonymous; can be null
                IpAddress = ip,
                UserAgent = userAgent,

                CreatedOnUtc = DateTime.UtcNow,
                CreatedByUserId = _user.UserId
            };

            _db.FormSubmissions.Add(submission);
            await _db.SaveChangesAsync(ct);

            foreach (var a in answers)
            {
                a.FormSubmissionId = submission.Id;
                _db.FormAnswers.Add(a);
            }

            // ✅ POST-SUBMIT HOOK
            string? nextUrl = null;

            if (pub.FormTemplate.Purpose == FormPurpose.Registration)
            {
                // ✅ Use invite.BeneficiaryId as the source of truth (secure)
                if (invite == null)
                    return (false, "Invite is required for Registration.", null, null);

                var ben = await _db.Beneficiaries.FirstOrDefaultAsync(b => b.Id == invite.BeneficiaryId, ct);
                if (ben != null)
                {
                    ben.RegistrationSubmittedAt = DateTime.UtcNow;
                    ben.RegistrationStatus = BeneficiaryRegistrationStatus.RegistrationSubmitted;

                    // ✅ Read ProgressStatus by FieldCode
                    string? progressRaw = null;

                    var progressField = fields.FirstOrDefault(f =>
                        string.Equals(f.FieldCode, "ProgressStatus", StringComparison.OrdinalIgnoreCase));

                    if (progressField != null && vm.Answers.TryGetValue(progressField.Id, out var pr))
                        progressRaw = pr?.Trim();

                    // Decide Completed
                    if (!string.IsNullOrWhiteSpace(progressRaw))
                    {
                        if (progressRaw.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                            progressRaw == ((int)BeneficiaryRegistrationStatus.Completed).ToString())
                        {
                            ben.RegistrationStatus = BeneficiaryRegistrationStatus.Completed;
                        }
                    }

                    // If Completed => require proof upload
                    if (ben.RegistrationStatus == BeneficiaryRegistrationStatus.Completed)
                    {
                        nextUrl = $"/register/proof?token={Uri.EscapeDataString(token)}&invite={Uri.EscapeDataString(invite.InviteToken)}";
                    }

                    _db.Beneficiaries.Update(ben);
                }

                // ✅ Link invite → submission (audit + single submit enforcement)
                invite.FormSubmissionId = submission.Id;
                invite.CompletedAtUtc = DateTime.UtcNow;
                _db.BeneficiaryFormInvites.Update(invite);
            }
            else
            {
                // For non-registration forms, also link invite if provided (audit)
                if (invite != null)
                {
                    invite.FormSubmissionId = submission.Id;
                    invite.CompletedAtUtc = DateTime.UtcNow;
                    _db.BeneficiaryFormInvites.Update(invite);
                }
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return (true, null, submission.Id, nextUrl);
        }


    }
}
