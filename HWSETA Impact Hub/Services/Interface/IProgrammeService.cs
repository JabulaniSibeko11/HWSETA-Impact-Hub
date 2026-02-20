using HWSETA_Impact_Hub.Domain.Entities;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IProgrammeService
    {
        Task<List<Programme>> ListAsync(CancellationToken ct);
        Task<Programme?> GetAsync(Guid id, CancellationToken ct);
        Task<Programme> CreateAsync(Programme p, CancellationToken ct);
        Task<bool> UpdateAsync(Programme p, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }
}
