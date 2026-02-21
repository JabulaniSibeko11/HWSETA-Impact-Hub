using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Models.ViewModels.Enrollment;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IEnrollmentService
    {
        Task<List<Enrollment>> ListAsync(CancellationToken ct);
        Task<Enrollment?> GetAsync(Guid id, CancellationToken ct);

        Task<(bool ok, string? error, Guid? enrollmentId)> CreateAsync(EnrollmentCreateVm vm, CancellationToken ct);

        Task<(bool ok, string? error)> UpdateStatusAsync(EnrollmentStatusUpdateVm vm, CancellationToken ct);

        Task<List<EnrollmentStatusHistory>> GetHistoryAsync(Guid enrollmentId, CancellationToken ct);
    }
}
