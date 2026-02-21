using HWSETA_Impact_Hub.Domain.Entities;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface ICohortService
    {
        Task<List<Cohort>> ListAsync(CancellationToken ct);
        Task<(bool ok, string? error)> CreateAsync(Guid currentUserId, Cohort cohort, CancellationToken ct);
    }
}
