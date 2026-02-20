using HWSETA_Impact_Hub.Domain.Entities;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IProviderService
    {
        Task<List<Provider>> ListAsync(CancellationToken ct);
        Task<Provider?> GetAsync(Guid id, CancellationToken ct);
        Task<Provider> CreateAsync(Provider p, CancellationToken ct);
        Task<bool> UpdateAsync(Provider p, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }
}
