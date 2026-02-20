using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Employers;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IEmployerService
    {
        Task<List<Employer>> ListAsync(CancellationToken ct);
        Task<(bool ok, string? error)> CreateAsync(EmployerCreateVm vm, CancellationToken ct);
        Task<EmployerImportResultVm> ImportFromExcelAsync(IFormFile file, CancellationToken ct);
    }
}
