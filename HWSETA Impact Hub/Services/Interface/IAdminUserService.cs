using HWSETA_Impact_Hub.Models.ViewModels.Admin;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IAdminUserService
    {
        Task<List<UserRowVm>> ListAsync(CancellationToken ct);
        Task<(bool ok, string? error)> CreateAsync(CreateUserVm vm, CancellationToken ct);
    }
}

