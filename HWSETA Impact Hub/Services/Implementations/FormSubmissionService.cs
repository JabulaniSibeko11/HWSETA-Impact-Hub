using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Models.ViewModels.Forms;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class FormSubmissionService : IFormSubmissionService
    {
        private readonly ApplicationDbContext _db;

        public FormSubmissionService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<FormSubmissionsListVm?> ListByTokenAsync(string token, CancellationToken ct)
        {
            token = (token ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token)) return null;

            var pub = await _db.FormPublishes.AsNoTracking()
                .Include(x => x.FormTemplate)
                .FirstOrDefaultAsync(x => x.PublicToken == token && x.IsPublished, ct);

            if (pub == null) return null;

            var rows = await _db.FormSubmissions.AsNoTracking()
                .Where(s => s.FormPublishId == pub.Id)
                .OrderByDescending(s => s.SubmittedOnUtc)
                .Select(s => new FormSubmissionRowVm
                {
                    SubmissionId = s.Id,
                    SubmittedOnUtc = s.SubmittedOnUtc,
                    SubmittedByUserId = s.SubmittedByUserId,
                    BeneficiaryId = s.BeneficiaryId,
                    IpAddress = s.IpAddress
                })
                .ToListAsync(ct);

            return new FormSubmissionsListVm
            {
                TemplateId = pub.FormTemplateId,
                Title = pub.FormTemplate.Title,
                Token = pub.PublicToken,
                Total = rows.Count,
                Rows = rows
            };
        }

        public async Task<FormSubmissionDetailsVm?> GetDetailsAsync(Guid submissionId, CancellationToken ct)
        {
            var sub = await _db.FormSubmissions.AsNoTracking()
                .Include(x => x.FormPublish).ThenInclude(p => p.FormTemplate)
                .FirstOrDefaultAsync(x => x.Id == submissionId, ct);

            if (sub == null) return null;

            // Load answers with field + section
            var answers = await _db.FormAnswers.AsNoTracking()
                .Where(a => a.FormSubmissionId == submissionId)
                .Include(a => a.FormField)
                    .ThenInclude(f => f.FormSection)
                .OrderBy(a => a.FormField.FormSection.SortOrder)
                .ThenBy(a => a.FormField.SortOrder)
                .ToListAsync(ct);

            var list = new List<FormSubmissionAnswerVm>();

            foreach (var a in answers)
            {
                string? value = a.Value;

                // if checkbox stored JSON array
                if (!string.IsNullOrWhiteSpace(a.ValueJson))
                {
                    try
                    {
                        var arr = JsonSerializer.Deserialize<List<string>>(a.ValueJson);
                        value = arr != null ? string.Join(", ", arr) : a.ValueJson;
                    }
                    catch
                    {
                        value = a.ValueJson;
                    }
                }

                list.Add(new FormSubmissionAnswerVm
                {
                    SectionTitle = a.FormField.FormSection.Title,
                    QuestionLabel = a.FormField.Label,
                    Value = value
                });
            }

            return new FormSubmissionDetailsVm
            {
                SubmissionId = sub.Id,
                FormTitle = sub.FormPublish.FormTemplate.Title,
                SubmittedOnUtc = sub.SubmittedOnUtc,
                Answers = list
            };
        }
    }
}
