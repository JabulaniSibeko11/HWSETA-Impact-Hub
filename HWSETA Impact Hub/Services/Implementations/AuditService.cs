using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Infrastructure.RequestContext;
using HWSETA_Impact_Hub.Services.Interface;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _user;
        private readonly IRequestContext _req;

        public AuditService(ApplicationDbContext db, ICurrentUserService user, IRequestContext req)
        {
            _db = db;
            _user = user;
            _req = req;
        }

        public Task LogViewAsync(string entityName, string entityId, string? note = null, CancellationToken ct = default)
            => LogAsync(
                actionType: "View",
                entityName: entityName,
                entityId: entityId,
                succeeded: true,
                note: note,
                beforeJson: null,
                afterJson: null,
                ct: ct);

        public async Task LogAsync(
            string actionType,
            string entityName,
            string? entityId = null,
            bool succeeded = true,
            string? note = null,
            string? beforeJson = null,
            string? afterJson = null,
            CancellationToken ct = default)
        {
            var utcNow = DateTime.UtcNow;

            _db.AuditEvents.Add(new AuditEvent
            {
                CreatedOnUtc = utcNow,
                OccurredOnUtc = utcNow,
                CreatedByUserId = _user.UserId,

                UserId = _user.UserId,
                UserEmail = _user.Email,
                UserRole = _user.Role,

                ActionType = string.IsNullOrWhiteSpace(actionType) ? "Unknown" : actionType.Trim(),
                EntityName = string.IsNullOrWhiteSpace(entityName) ? "Unknown" : entityName.Trim(),
                EntityId = string.IsNullOrWhiteSpace(entityId) ? null : entityId.Trim(),

                IpAddress = _req.IpAddress,
                UserAgent = _req.UserAgent,
                CorrelationId = _req.CorrelationId,
                RequestPath = _req.Path,
                HttpMethod = _req.Method,

                BeforeJson = beforeJson,
                AfterJson = afterJson,

                Succeeded = succeeded,
                ErrorMessage = note
            });

            await _db.SaveChangesAsync(ct);
        }

        public Task LogErrorAsync(
            string actionType,
            string entityName,
            string? entityId,
            string errorMessage,
            string? note = null,
            string? beforeJson = null,
            string? afterJson = null,
            CancellationToken ct = default)
        {
            var msg = string.IsNullOrWhiteSpace(note)
                ? errorMessage
                : $"{note} | {errorMessage}";

            return LogAsync(
                actionType: actionType,
                entityName: entityName,
                entityId: entityId,
                succeeded: false,
                note: msg,
                beforeJson: beforeJson,
                afterJson: afterJson,
                ct: ct);
        }
    }
}