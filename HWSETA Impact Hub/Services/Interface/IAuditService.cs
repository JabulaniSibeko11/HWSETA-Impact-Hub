namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IAuditService
    {
        Task LogViewAsync(string entityName, string entityId, string? note = null, CancellationToken ct = default);
    }
}
