using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Models.ViewModels.Forms;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;

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

        public Task<List<FormTemplateListRowVm>> ListAsync(CancellationToken ct) =>
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
    }
}
