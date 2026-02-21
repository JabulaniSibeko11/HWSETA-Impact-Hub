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
                .OrderByDescending(x => x.CreatedOnUtc)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    Status = x.Status.ToString(),
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

            return templates.Select(t =>
            {
                pubByTemplate.TryGetValue(t.Id, out var pub);

                return new FormTemplateListRowVm
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = Enum.TryParse<FormStatus>(t.Status, out var status)
    ? status
    : FormStatus.Draft,
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
            var t = await _db.FormTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == templateId, ct);
            if (t == null) return null;

            var p = await _db.FormPublishes.AsNoTracking().FirstOrDefaultAsync(x => x.FormTemplateId == templateId, ct);

            var token = p?.PublicToken;
            var url = string.IsNullOrWhiteSpace(token) ? null : $"{baseUrl.TrimEnd('/')}/f/{token}";

            return new FormPublishVm
            {
                TemplateId = t.Id,
                Title = t.Title,
                IsPublished = p?.IsPublished ?? false,
                Token = token,
                OpenFromUtc = p.OpenFromUtc,
                CloseAtUtc = p.CloseAtUtc,
                MaxSubmissions = p?.MaxSubmissions,
                AllowMultipleSubmissions = p?.AllowMultipleSubmissions ?? true,
                PublicUrl = url
            };
        }

        public async Task<(bool ok, string? error)> PublishAsync(FormPublishVm vm, CancellationToken ct)
        {
            if (vm.TemplateId == Guid.Empty)
                return (false, "Invalid template.");

            var t = await _db.FormTemplates.FirstOrDefaultAsync(x => x.Id == vm.TemplateId, ct);
            if (t == null) return (false, "Template not found.");
            if (!t.IsActive) return (false, "Template is not active.");

            // Must have at least 1 active field
            var hasField = await _db.FormFields.AnyAsync(f =>
                f.IsActive && f.FormSection.FormTemplateId == vm.TemplateId, ct);

            if (!hasField)
                return (false, "You cannot publish a form with no questions.");

            // Dates are NOT nullable in your VM, so validate directly
            vm.OpenFromUtc = vm.OpenFromUtc == default ? DateTime.UtcNow : vm.OpenFromUtc;
            vm.CloseAtUtc = vm.CloseAtUtc == default ? vm.OpenFromUtc.AddDays(30) : vm.CloseAtUtc;

            if (vm.CloseAtUtc <= vm.OpenFromUtc)
                return (false, "Close date must be after open date.");

            var p = await _db.FormPublishes.FirstOrDefaultAsync(x => x.FormTemplateId == vm.TemplateId, ct);

            if (p == null)
            {
                p = new FormPublish
                {
                    FormTemplateId = vm.TemplateId,
                    PublicToken = GenerateToken(),
                    IsPublished = true,

                    OpenFromUtc = vm.OpenFromUtc,
                    CloseAtUtc = vm.CloseAtUtc,

                    MaxSubmissions = vm.MaxSubmissions,
                    AllowMultipleSubmissions = vm.AllowMultipleSubmissions,

                    // optional flags
                    IsOpen = true,

                    CreatedOnUtc = DateTime.UtcNow,
                    CreatedByUserId = _user.UserId
                };

                _db.FormPublishes.Add(p);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(p.PublicToken))
                    p.PublicToken = GenerateToken();

                p.IsPublished = true;
                p.IsOpen = true;

                p.OpenFromUtc = vm.OpenFromUtc;
                p.CloseAtUtc = vm.CloseAtUtc;

                p.MaxSubmissions = vm.MaxSubmissions;
                p.AllowMultipleSubmissions = vm.AllowMultipleSubmissions;

                p.UpdatedOnUtc = DateTime.UtcNow;
                p.UpdatedByUserId = _user.UserId;
            }

            // keep your template status update
            t.Status = FormStatus.Published;
            t.UpdatedOnUtc = DateTime.UtcNow;
            t.UpdatedByUserId = _user.UserId;

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        public async Task<(bool ok, string? error)> UnpublishAsync(Guid templateId, CancellationToken ct)
        {
            var p = await _db.FormPublishes.FirstOrDefaultAsync(x => x.FormTemplateId == templateId, ct);
            if (p == null) return (false, "Publish record not found.");

            p.IsPublished = false;
            p.IsOpen = false;

            p.UpdatedOnUtc = DateTime.UtcNow;
            p.UpdatedByUserId = _user.UserId;

            var t = await _db.FormTemplates.FirstOrDefaultAsync(x => x.Id == templateId, ct);
            if (t != null)
            {
                t.Status = FormStatus.Draft;
                t.UpdatedOnUtc = DateTime.UtcNow;
                t.UpdatedByUserId = _user.UserId;
            }

            await _db.SaveChangesAsync(ct);
            return (true, null);
        }

        public async Task<PublicFormVm?> GetPublicFormAsync(string token, CancellationToken ct)
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

            if (now > pub.CloseAtUtc)
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

            // Sections
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

            // Fields with FormSectionId included so we don't do extra queries
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
                    f.RegexPattern
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

            foreach (var s in sections)
            {
                s.Questions = fields
                    .Where(f => f.FormSectionId == s.Id)
                    .OrderBy(f => f.SortOrder)
                    .Select(f => new PublicQuestionVm
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
                        Options = optByField.TryGetValue(f.Id, out var list) ? list : new List<PublicOptionVm>()
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
                Sections = sections
            };
        }

        public async Task<(bool ok, string? error, Guid? submissionId)> SubmitPublicAsync(
            PublicFormSubmitVm vm,
            string? ip,
            string? userAgent,
            CancellationToken ct)
        {
            var token = (vm.Token ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token))
                return (false, "Invalid token.", null);

            // IMPORTANT: Your publish uses PublicToken
            var pub = await _db.FormPublishes.FirstOrDefaultAsync(x => x.PublicToken == token && x.IsPublished, ct);
            if (pub == null)
                return (false, "Form not found or not published.", null);

            var now = DateTime.UtcNow;

            if (!pub.IsOpen) return (false, "This form is closed.", null);
            if (now < pub.OpenFromUtc) return (false, "This form is not open yet.", null);
            if (now > pub.CloseAtUtc) return (false, "This form is closed.", null);

            if (pub.MaxSubmissions.HasValue)
            {
                var count = await _db.FormSubmissions.CountAsync(s => s.FormPublishId == pub.Id, ct);
                if (count >= pub.MaxSubmissions.Value)
                    return (false, "This form has reached the maximum number of submissions.", null);
            }

            // Load schema fields
            var fields = await _db.FormFields.AsNoTracking()
                .Where(f => f.IsActive && f.FormSection.FormTemplateId == pub.FormTemplateId)
                .Select(f => new
                {
                    f.Id,
                    f.Label,
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
                    return (false, $"'{f.Label}' is required.", null);
            }

            var answers = new List<FormAnswer>();

            foreach (var f in fields)
            {
                // multi checkbox
                if (f.FieldType == FormFieldType.Checkbox)
                {
                    if (vm.MultiAnswers.TryGetValue(f.Id, out var list) && list != null && list.Count > 0)
                    {
                        // store each value as one row OR use ValueJson
                        // We'll store JSON array in ValueJson (cleaner)
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
                    return (false, $"'{f.Label}' exceeds max length {f.MaxLength.Value}.", null);

                if (f.FieldType == FormFieldType.Number)
                {
                    if (!int.TryParse(raw, out var n))
                        return (false, $"'{f.Label}' must be a whole number.", null);

                    if (f.MinInt.HasValue && n < f.MinInt.Value) return (false, $"'{f.Label}' must be >= {f.MinInt.Value}.", null);
                    if (f.MaxInt.HasValue && n > f.MaxInt.Value) return (false, $"'{f.Label}' must be <= {f.MaxInt.Value}.", null);
                }

                if (f.FieldType == FormFieldType.Decimal)
                {
                    if (!decimal.TryParse(raw, out var d))
                        return (false, $"'{f.Label}' must be a decimal number.", null);

                    if (f.MinDecimal.HasValue && d < f.MinDecimal.Value) return (false, $"'{f.Label}' must be >= {f.MinDecimal.Value}.", null);
                    if (f.MaxDecimal.HasValue && d > f.MaxDecimal.Value) return (false, $"'{f.Label}' must be <= {f.MaxDecimal.Value}.", null);
                }

                answers.Add(new FormAnswer
                {
                    FormFieldId = f.Id,
                    Value = raw,
                    CreatedOnUtc = DateTime.UtcNow
                });
            }

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            // IMPORTANT: Your FormSubmission links to FormPublishId (not template id)
            var submission = new FormSubmission
            {
                FormPublishId = pub.Id,
                SubmittedOnUtc = DateTime.UtcNow,

                // optional: if you want, store identity
                SubmittedByUserId = _user.UserId,

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

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return (true, null, submission.Id);
        }
        private static string GenerateToken()
        {
            // URL-safe, short
            var bytes = RandomNumberGenerator.GetBytes(18);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}
