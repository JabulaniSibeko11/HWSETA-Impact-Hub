using HWSETA_Impact_Hub.Models.ViewModels.Forms;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IFormSubmissionService
    {
        Task<FormSubmissionsListVm?> ListByTokenAsync(string token, CancellationToken ct);
        Task<FormSubmissionDetailsVm?> GetDetailsAsync(Guid submissionId, CancellationToken ct);
    }
}
