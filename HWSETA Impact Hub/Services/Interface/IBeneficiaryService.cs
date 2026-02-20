using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Beneficiaries;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IBeneficiaryService
    {
        Task<List<Beneficiary>> ListAsync(CancellationToken ct);
        Task<(bool ok, string? error)> CreateAsync(BeneficiaryCreateVm vm, CancellationToken ct);
        Task<BeneficiaryImportResultVm> ImportFromExcelAsync(IFormFile file, CancellationToken ct);
    }
}
