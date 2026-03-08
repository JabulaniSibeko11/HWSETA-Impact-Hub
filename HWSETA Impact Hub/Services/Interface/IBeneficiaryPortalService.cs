using HWSETA_Impact_Hub.Models.ViewModels.BeneficiaryPortal;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IBeneficiaryPortalService
    {
        Task<BeneficiaryDashboardVm?> GetDashboardAsync(string currentUserId, CancellationToken ct);
    }
}
