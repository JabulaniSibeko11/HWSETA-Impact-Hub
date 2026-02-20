using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Provider;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IProviderService
    {
        Task<List<Provider>> ListAsync(CancellationToken ct);
        Task<(bool ok, string? error)> CreateAsync(ProviderCreateVm vm, CancellationToken ct);
        Task<ProviderImportResultVm> ImportFromExcelAsync(IFormFile file, CancellationToken ct);
    }
}
