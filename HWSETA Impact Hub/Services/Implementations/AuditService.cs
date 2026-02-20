using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Infrastructure.RequestContext;
using HWSETA_Impact_Hub.Services.Interface;
using System;

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

        public async Task LogViewAsync(string entityName, string entityId, string? note = null, CancellationToken ct = default)
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

                ActionType = "View",
                EntityName = entityName,
                EntityId = entityId,

                IpAddress = _req.IpAddress,
                UserAgent = _req.UserAgent,
                CorrelationId = _req.CorrelationId,
                RequestPath = _req.Path,
                HttpMethod = _req.Method,

                Succeeded = true,
                ErrorMessage = note
            });

            await _db.SaveChangesAsync(ct);
        }
    }
}

