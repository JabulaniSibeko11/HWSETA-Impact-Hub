using HWSETA_Impact_Hub.Models.ViewModels.Enrollment;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IEnrollmentDocumentService
    {
        Task<List<EnrollmentDocumentRowVm>> ListAsync(Guid enrollmentId, CancellationToken ct);
        Task<(bool ok, string? error)> UploadAsync(EnrollmentDocumentUploadVm vm, CancellationToken ct);
        Task<(bool ok, string? error, string filePath, string downloadName)> GetFileAsync(Guid docId, CancellationToken ct);
        Task<(bool ok, string? error)> DeleteAsync(Guid docId, CancellationToken ct);
    }
}
