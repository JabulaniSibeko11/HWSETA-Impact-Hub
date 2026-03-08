using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Infrastructure.Encryption;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Models.ViewModels.Search;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class GlobalSearchService : IGlobalSearchService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public GlobalSearchService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<GlobalSearchResponseVm> SearchAsync(string query, string currentUserId, CancellationToken ct)
        {
            query = (query ?? "").Trim();
            var vm = new GlobalSearchResponseVm { Query = query };

            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return vm;

            var user = await _userManager.FindByIdAsync(currentUserId);
            var roles = user != null
                ? await _userManager.GetRolesAsync(user)
                : new List<string>();

            var isBeneficiary = roles.Any(r => r.Equals("Beneficiary", StringComparison.OrdinalIgnoreCase));

            if (isBeneficiary)
            {
                var beneficiary = await _db.Beneficiaries
                    .AsNoTracking()
                    .Where(x => x.UserId == currentUserId)
                    .Select(x => new { x.Id })
                    .FirstOrDefaultAsync(ct);

                if (beneficiary == null)
                    return vm;

                var forms = await _db.BeneficiaryFormInvites
                    .AsNoTracking()
                    .Where(x =>
                        x.BeneficiaryId == beneficiary.Id &&
                        x.FormPublish != null &&
                        x.FormPublish.FormTemplate != null &&
                        x.FormPublish.FormTemplate.Title != null &&
                        x.FormPublish.FormTemplate.Title.Contains(query))
                    .OrderByDescending(x => x.SentOnUtc)
                    .Select(x => new
                    {
                        x.Id,
                        Title = x.FormPublish!.FormTemplate!.Title,
                        Status = x.Status
                    })
                    .Take(5)
                    .ToListAsync(ct);

                vm.Results.AddRange(forms.Select(x => new GlobalSearchResultVm
                {
                    Section = "My Forms",
                    Title = x.Title ?? "Form",
                    Subtitle = x.Status ?? "Sent",
                    Url = "/BeneficiaryForms",
                    Icon = "bi-file-earmark-text"
                }));

                var threads = await _db.ConversationThreads
                    .AsNoTracking()
                    .Where(x =>
                        x.BeneficiaryId == beneficiary.Id &&
                        x.Subject != null &&
                        x.Subject.Contains(query))
                    .OrderByDescending(x => x.LastMessageOnUtc)
                    .Select(x => new
                    {
                        x.Id,
                        x.Subject
                    })
                    .Take(5)
                    .ToListAsync(ct);

                vm.Results.AddRange(threads.Select(x => new GlobalSearchResultVm
                {
                    Section = "My Messages",
                    Title = x.Subject ?? "Conversation",
                    Subtitle = "Conversation thread",
                    Url = $"/BeneficiaryMessages/Thread/{x.Id}",
                    Icon = "bi-chat-dots"
                }));

                return vm;
            }

            var beneficiaryResults = new List<GlobalSearchResultVm>();
            var queryHash = _db.GetService<IAesEncryptionService>().BlindIndex(query);

            // Exact hash matches only
            var exactBeneficiaries = await _db.Beneficiaries
                .AsNoTracking()
                .Where(x =>
                    x.IdentifierValueHash == queryHash ||
                    (x.EmailHash != null && x.EmailHash == queryHash))
                .OrderBy(x => x.CreatedOnUtc)
                .Take(5)
                .ToListAsync(ct);

            beneficiaryResults.AddRange(exactBeneficiaries.Select(x => new GlobalSearchResultVm
            {
                Section = "Beneficiaries",
                Title = $"{x.FirstName ?? ""} {x.LastName ?? ""}".Trim(),
                Subtitle = x.Email ?? x.MobileNumber ?? "Beneficiary",
                Url = $"/Beneficiaries/Details/{x.Id}",
                Icon = "bi-person-lines-fill"
            }));

            if (beneficiaryResults.Count < 5)
            {
                var recentBeneficiaries = await _db.Beneficiaries
                    .AsNoTracking()
                    .OrderByDescending(x => x.CreatedOnUtc)
                    .Take(200)
                    .ToListAsync(ct);

                var q = query.Trim();

                var memoryMatches = recentBeneficiaries
                    .Where(x =>
                        (!string.IsNullOrWhiteSpace(x.FirstName) && x.FirstName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(x.LastName) && x.LastName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(x.Email) && x.Email.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(x.MobileNumber) && x.MobileNumber.Contains(q, StringComparison.OrdinalIgnoreCase)))
                    .Where(x => beneficiaryResults.All(r => !r.Url.EndsWith(x.Id.ToString())))
                    .Take(5 - beneficiaryResults.Count)
                    .ToList();

                beneficiaryResults.AddRange(memoryMatches.Select(x => new GlobalSearchResultVm
                {
                    Section = "Beneficiaries",
                    Title = $"{x.FirstName ?? ""} {x.LastName ?? ""}".Trim(),
                    Subtitle = x.Email ?? x.MobileNumber ?? "Beneficiary",
                    Url = $"/Beneficiaries/Details/{x.Id}",
                    Icon = "bi-person-lines-fill"
                }));
            }

            vm.Results.AddRange(beneficiaryResults);

            var programmes = await _db.Programmes
                .AsNoTracking()
                .Where(x =>
                    (x.ProgrammeName != null && x.ProgrammeName.Contains(query)) ||
                    (x.ProgrammeCode != null && x.ProgrammeCode.Contains(query)))
                .OrderBy(x => x.ProgrammeName)
                .Select(x => new
                {
                    x.Id,
                    x.ProgrammeName,
                    x.ProgrammeCode
                })
                .Take(5)
                .ToListAsync(ct);

            vm.Results.AddRange(programmes.Select(x => new GlobalSearchResultVm
            {
                Section = "Programmes",
                Title = x.ProgrammeName ?? "Programme",
                Subtitle = x.ProgrammeCode ?? "",
                Url = "/Programmes",
                Icon = "bi-mortarboard"
            }));

            var providers = await _db.Providers
                .AsNoTracking()
                .Where(x =>
                    (x.ProviderName != null && x.ProviderName.Contains(query)) ||
                    (x.ProviderCode != null && x.ProviderCode.Contains(query)) ||
                    (x.AccreditationNo != null && x.AccreditationNo.Contains(query)))
                .OrderBy(x => x.ProviderName)
                .Select(x => new
                {
                    x.Id,
                    x.ProviderName,
                    x.ProviderCode,
                    x.AccreditationNo
                })
                .Take(5)
                .ToListAsync(ct);

            vm.Results.AddRange(providers.Select(x => new GlobalSearchResultVm
            {
                Section = "Providers",
                Title = x.ProviderName ?? "Provider",
                Subtitle = x.AccreditationNo ?? x.ProviderCode ?? "",
                Url = "/Providers",
                Icon = "bi-building"
            }));

            var employers = await _db.Employers
                .AsNoTracking()
                .Where(x =>
                    (x.EmployerName != null && x.EmployerName.Contains(query)) ||
                    (x.EmployerCode != null && x.EmployerCode.Contains(query)) ||
                    (x.RegistrationNumber != null && x.RegistrationNumber.Contains(query)))
                .OrderBy(x => x.EmployerName)
                .Select(x => new
                {
                    x.Id,
                    x.EmployerName,
                    x.EmployerCode,
                    x.RegistrationNumber
                })
                .Take(5)
                .ToListAsync(ct);

            vm.Results.AddRange(employers.Select(x => new GlobalSearchResultVm
            {
                Section = "Employers",
                Title = x.EmployerName ?? "Employer",
                Subtitle = x.EmployerCode ?? x.RegistrationNumber ?? "",
                Url = "/Employers",
                Icon = "bi-briefcase"
            }));

            var cohorts = await _db.Cohorts
                .AsNoTracking()
                .Where(x =>
                    (x.CohortCode != null && x.CohortCode.Contains(query)) ||
                    (x.Programme != null && x.Programme.ProgrammeName != null && x.Programme.ProgrammeName.Contains(query)))
                .OrderByDescending(x => x.StartDate)
                .Select(x => new
                {
                    x.Id,
                    x.CohortCode,
                    ProgrammeName = x.Programme != null ? x.Programme.ProgrammeName : null
                })
                .Take(5)
                .ToListAsync(ct);

            vm.Results.AddRange(cohorts.Select(x => new GlobalSearchResultVm
            {
                Section = "Cohorts",
                Title = x.CohortCode ?? "Cohort",
                Subtitle = x.ProgrammeName ?? "",
                Url = "/Cohorts",
                Icon = "bi-diagram-3"
            }));

            var formsAdmin = await _db.FormTemplates
                .AsNoTracking()
                .Where(x => x.Title != null && x.Title.Contains(query))
                .OrderByDescending(x => x.CreatedOnUtc)
                .Select(x => new
                {
                    x.Id,
                    x.Title
                })
                .Take(5)
                .ToListAsync(ct);

            vm.Results.AddRange(formsAdmin.Select(x => new GlobalSearchResultVm
            {
                Section = "Forms",
                Title = x.Title ?? "Form template",
                Subtitle = "Form template",
                Url = "/FormTemplates",
                Icon = "bi-file-earmark-text"
            }));

            var threadsAdmin = await _db.ConversationThreads
                .AsNoTracking()
                .Where(x => x.Subject != null && x.Subject.Contains(query))
                .Select(x => new
                {
                    x.Id,
                    x.Subject,
                    BeneficiaryFirstName = x.Beneficiary != null ? x.Beneficiary.FirstName : null,
                    BeneficiaryLastName = x.Beneficiary != null ? x.Beneficiary.LastName : null,
                    x.LastMessageOnUtc
                })
                .OrderByDescending(x => x.LastMessageOnUtc)
                .Take(5)
                .ToListAsync(ct);

            vm.Results.AddRange(threadsAdmin.Select(x => new GlobalSearchResultVm
            {
                Section = "Communication",
                Title = x.Subject ?? "Conversation",
                Subtitle = $"{x.BeneficiaryFirstName ?? ""} {x.BeneficiaryLastName ?? ""}".Trim(),
                Url = $"/Chat/Thread/{x.Id}",
                Icon = "bi-chat-dots"
            }));

            return vm;
        }
    }
}