namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IAuditService
    {
        Task LogViewAsync(string entityName, string entityId, string? note = null, CancellationToken ct = default);

        Task LogAsync(
           string actionType,
           string entityName,
           string? entityId = null,
           bool succeeded = true,
           string? note = null,
           string? beforeJson = null,
           string? afterJson = null,
           CancellationToken ct = default);
        Task LogErrorAsync(
         string actionType,
         string entityName,
         string? entityId,
         string errorMessage,
         string? note = null,
         string? beforeJson = null,
         string? afterJson = null,
         CancellationToken ct = default);
    }
}
