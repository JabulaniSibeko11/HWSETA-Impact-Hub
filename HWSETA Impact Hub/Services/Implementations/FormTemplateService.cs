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

       

        public async Task<List<FormTemplateListRowVm>> ListAsync(CancellationToken ct)
        {
            var templates = await _db.FormTemplates.AsNoTracking()
     .Where(x => x.IsActive)
     .OrderByDescending(x => x.Purpose == FormPurpose.Registration)
     .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt)
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

            // Upsert publish record so the page always has a token + safe schedule defaults
            var p = await _db.FormPublishes
                .FirstOrDefaultAsync(x => x.FormTemplateId == templateId, ct);

            if (p == null)
            {
                p = new FormPublish
                {
                    FormTemplateId = templateId,
                    PublicToken = NewPublicToken(),
                    IsPublished = false,

                    // safe defaults
                    IsOpen = true,
                    OpenFromUtc = DateTime.UtcNow,
                    CloseAtUtc = DateTime.MaxValue,
                    AllowMultipleSubmissions = true,

                    CreatedOnUtc = DateTime.UtcNow
                };

                _db.FormPublishes.Add(p);
                await _db.SaveChangesAsync(ct);
            }
            else
            {
                // fix legacy bad close date in db (0001-01-01)
                if (p.CloseAtUtc == default)
                {
                    p.CloseAtUtc = DateTime.MaxValue;
                    await _db.SaveChangesAsync(ct);
                }

                // keep token stable (never change)
                if (string.IsNullOrWhiteSpace(p.PublicToken))
                {
                    p.PublicToken = NewPublicToken();
                    await _db.SaveChangesAsync(ct);
                }
            }

            // Build public url from publish token (public link should come from FormPublish)
            var url = $"{baseUrl}/public/forms/{p.PublicToken}";

            // If you still store token on template, keep aligned (optional)
            if (!string.IsNullOrWhiteSpace(t.PublicToken) && t.PublicToken != p.PublicToken)
            {
                // DO NOT auto-overwrite here because t was loaded AsNoTracking.
                // If you want alignment, do it inside PublishAsync only.
            }

            return new FormPublishVm
            {
                TemplateId = t.Id,
                Title = t.Title,
                Purpose = t.Purpose,
                Status = t.Status,

                IsPublished = p.IsPublished,
                IsOpen = p.IsOpen,
                Token = p.PublicToken,
                PublicUrl = url,

                OpenFromUtc = p.OpenFromUtc,
                CloseAtUtc = (p.CloseAtUtc == DateTime.MaxValue ? null : p.CloseAtUtc),
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
            var t = await _db.FormTemplates
                .FirstOrDefaultAsync(x => x.Id == vm.TemplateId, ct);

            if (t == null) return (false, "Template not found.");

            // Enforce: only one Registration template
            if (t.Purpose == FormPurpose.Registration)
            {
                var otherRegistrationExists = await _db.FormTemplates
                    .AnyAsync(x => x.Id != t.Id
                                && x.Purpose == FormPurpose.Registration
                                && x.IsActive, ct);

                if (otherRegistrationExists)
                    return (false, "Only one Registration FormTemplate is allowed. Edit the existing Registration template instead of creating another.");
            }
            else
            {
                // ✅ Only validate fields for NON-registration forms (dynamic forms)
                var hasAnyFields = await _db.FormFields
                    .AnyAsync(f => f.IsActive && f.FormSection.FormTemplateId == t.Id, ct);

                if (!hasAnyFields)
                    return (false, "You cannot publish an empty form. Add at least one question.");
            }

            // Upsert FormPublish (kept for dynamic/public forms; harmless for Registration)
            var p = await _db.FormPublishes
                .FirstOrDefaultAsync(x => x.FormTemplateId == t.Id, ct);

            if (p == null)
            {
                p = new FormPublish
                {
                    FormTemplateId = t.Id,
                    PublicToken = NewPublicToken(),
                    CreatedOnUtc = DateTime.UtcNow,

                    IsOpen = true,
                    OpenFromUtc = DateTime.UtcNow,
                    CloseAtUtc = DateTime.MaxValue,
                    AllowMultipleSubmissions = true
                };
                _db.FormPublishes.Add(p);
            }

            var now = DateTime.UtcNow;
            var openFrom = vm.OpenFromUtc ?? now;
            var closeAt = vm.CloseAtUtc ?? DateTime.MaxValue;

            if (closeAt != DateTime.MaxValue && closeAt <= openFrom)
                return (false, "Close At must be after Open From.");

            p.IsPublished = true;
            p.IsOpen = vm.IsOpen;
            p.OpenFromUtc = openFrom;
            p.CloseAtUtc = closeAt;
            p.MaxSubmissions = vm.MaxSubmissions;
            p.AllowMultipleSubmissions = vm.AllowMultipleSubmissions;

            if (string.IsNullOrWhiteSpace(p.PublicToken))
                p.PublicToken = NewPublicToken();

            // Template admin state
            t.Status = FormStatus.Published;
            t.IsActive = true;
            t.UpdatedAt = now;
            t.PublishedAt = now;
            t.UnpublishedAt = null;

            if (string.IsNullOrWhiteSpace(t.PublicToken))
                t.PublicToken = p.PublicToken;

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }
        public async Task<(bool ok, string? error)> UnpublishAsync(Guid templateId, CancellationToken ct)
        {
            var t = await _db.FormTemplates.FirstOrDefaultAsync(x => x.Id == templateId, ct);
            if (t == null) return (false, "Template not found.");

            var p = await _db.FormPublishes.FirstOrDefaultAsync(x => x.FormTemplateId == templateId, ct);

            // Unpublish should stop public access
            if (p != null)
            {
                p.IsPublished = false;
                p.IsOpen = false;
                // Keep token/history, do not delete
            }

            t.Status = FormStatus.Draft;
            t.UpdatedAt = DateTime.UtcNow;
            t.UnpublishedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }
        public async Task<PublicFormVm?> GetPublicFormAsync(
       string token,
       string? prefillEmail,
       string? prefillPhone,
       CancellationToken ct)
        {
            token = (token ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token)) return null;

            // Only serve if actually published
            var pub = await _db.FormPublishes.AsNoTracking()
                .Include(x => x.FormTemplate)
                .FirstOrDefaultAsync(x => x.PublicToken == token && x.IsPublished, ct);

            if (pub == null) return null;

            var now = DateTime.UtcNow;

            // Normalise bad data: if CloseAtUtc is default, treat as "no close"
            var closeAt = (pub.CloseAtUtc == default) ? DateTime.MaxValue : pub.CloseAtUtc;

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

            // time window check
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

            if (now > closeAt)
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

            // Max submissions
            if (pub.MaxSubmissions.HasValue)
            {
                var count = await _db.FormSubmissions
                    .AsNoTracking()
                    .CountAsync(s => s.FormPublishId == pub.Id, ct);

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
            BeneficiaryPrefillVm? prefill = null;

            var email = (prefillEmail ?? "").Trim();
            var phone = (prefillPhone ?? "").Trim();

            Beneficiary? ben = null;

            if (!string.IsNullOrWhiteSpace(email))
            {
                ben = await _db.Beneficiaries.AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Email == email, ct);
            }

            if (ben == null && !string.IsNullOrWhiteSpace(phone))
            {
                ben = await _db.Beneficiaries.AsNoTracking()
                    .FirstOrDefaultAsync(b => b.MobileNumber == phone, ct);
            }

            if (ben != null)
            {
                // Load lookups only if we found a beneficiary
                var benFull = await _db.Beneficiaries.AsNoTracking()
                    .Include(b => b.Gender)
                    .Include(b => b.Race)
                    .Include(b => b.CitizenshipStatus)
                    .Include(b => b.DisabilityStatus)
                    .Include(b => b.DisabilityType)
                    .Include(b => b.EducationLevel)
                    .Include(b => b.EmploymentStatus)
                    .FirstOrDefaultAsync(b => b.Id == ben.Id, ct);

                if (benFull != null)
                {
                    prefill = new BeneficiaryPrefillVm
                    {
                        BeneficiaryId = benFull.Id,
                        IdentifierType = benFull.IdentifierType.ToString(),
                        IdentifierValue = benFull.IdentifierValue,
                        FirstName = benFull.FirstName,
                        MiddleName = benFull.MiddleName,
                        LastName = benFull.LastName,
                        DateOfBirth = benFull.DateOfBirth,

                        Email = benFull.Email,
                        MobileNumber = benFull.MobileNumber,

                        Province = benFull.Province,
                        City = benFull.City,
                        AddressLine1 = benFull.AddressLine1,
                        PostalCode = benFull.PostalCode,

                        Gender = benFull.Gender?.Name ?? "",
                        Race = benFull.Race?.Name ?? "",
                        CitizenshipStatus = benFull.CitizenshipStatus?.Name ?? "",
                        DisabilityStatus = benFull.DisabilityStatus?.Name ?? "",
                        DisabilityType = benFull.DisabilityType?.Name,
                        EducationLevel = benFull.EducationLevel?.Name ?? "",
                        EmploymentStatus = benFull.EmploymentStatus?.Name ?? ""
                    };
                }
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

            // Fields
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

                        if (prefill != null && !string.IsNullOrWhiteSpace(f.FieldCode))
                        {
                            q.PrefillValue = GetPrefillValue(f.FieldCode, prefill);
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



        public async Task<(bool ok, string? error)> DeleteAsync(Guid templateId, CancellationToken ct)
        {
            var t = await _db.FormTemplates.FirstOrDefaultAsync(x => x.Id == templateId, ct);
            if (t == null) return (false, "Template not found.");

            // 🔒 Protect system Registration form
            if (t.Purpose == FormPurpose.Registration)
                return (false, "This is the system Beneficiary Registration form and cannot be deleted.");

            // If already inactive
            if (!t.IsActive)
                return (false, "Template is already deleted/inactive.");

            // Block delete if it has submissions (recommended)
            var hasSubmissions = await _db.FormSubmissions
                .AsNoTracking()
                .AnyAsync(s => s.FormPublish.FormTemplateId == templateId, ct);

            if (hasSubmissions)
                return (false, "This template already has submissions and cannot be deleted. Archive it instead.");

            // Also block delete if it is published (force unpublish first)
            var isPublished = await _db.FormPublishes
                .AsNoTracking()
                .AnyAsync(p => p.FormTemplateId == templateId && p.IsPublished, ct);

            if (isPublished)
                return (false, "Unpublish this form first before deleting it.");

            // Optional: hard delete child schema if you want (sections/fields/options)
            // We'll do safe soft-delete for the template + hard delete schema (no submissions)
            var sections = await _db.FormSections
                .Where(s => s.FormTemplateId == templateId)
                .ToListAsync(ct);

            var sectionIds = sections.Select(s => s.Id).ToList();

            var fields = await _db.FormFields
                .Where(f => sectionIds.Contains(f.FormSectionId))
                .ToListAsync(ct);

            var fieldIds = fields.Select(f => f.Id).ToList();

            var options = await _db.FormFieldOptions
                .Where(o => fieldIds.Contains(o.FormFieldId))
                .ToListAsync(ct);

            _db.FormFieldOptions.RemoveRange(options);
            _db.FormFields.RemoveRange(fields);
            _db.FormSections.RemoveRange(sections);

            // soft delete template
            t.IsActive = false;
            t.Status = FormStatus.Archived;
            t.UpdatedAt = DateTime.UtcNow;
            t.UpdatedOnUtc = DateTime.UtcNow;
            t.UpdatedByUserId = _user.UserId;

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }
    }
}
