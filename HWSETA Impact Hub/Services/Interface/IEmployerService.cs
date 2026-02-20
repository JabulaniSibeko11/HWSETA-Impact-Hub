using HWSETA_Impact_Hub.Domain.Entities;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IEmployerService
    {
        Task<List<Employer>> ListAsync(CancellationToken ct);
        Task<Employer?> GetAsync(Guid id, CancellationToken ct);
        Task<Employer> CreateAsync(Employer p, CancellationToken ct);
        Task<bool> UpdateAsync(Employer p, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }
}
