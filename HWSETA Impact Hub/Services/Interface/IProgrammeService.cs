using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Programme;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IProgrammeService
    {
        Task<List<Programme>> ListAsync(CancellationToken ct);
        Task<(bool ok, string? error)> CreateAsync(ProgrammeCreateVm vm, CancellationToken ct);
        Task<ProgrammeImportResultVm> ImportFromExcelAsync(IFormFile file, CancellationToken ct);
    }
}
