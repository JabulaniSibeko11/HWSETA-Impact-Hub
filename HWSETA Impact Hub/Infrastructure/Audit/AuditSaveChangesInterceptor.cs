using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Identity;
using HWSETA_Impact_Hub.Infrastructure.RequestContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Text.Json;

namespace HWSETA_Impact_Hub.Infrastructure.Audit
{
    public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IRequestContext _req;

        public AuditSaveChangesInterceptor(ICurrentUserService currentUser, IRequestContext req)
        {
            _currentUser = currentUser;
            _req = req;
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            // We audit in SavingChanges so we can capture before/after; SavedChanges just completes.
            return base.SavedChanges(eventData, result);
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context is ApplicationDbContext ctx)
            {
                CreateAuditEvents(ctx);
            }

            return base.SavingChanges(eventData, result);
        }

        private void CreateAuditEvents(ApplicationDbContext ctx)
        {
            var utcNow = DateTime.UtcNow;

            // Only audit entity changes (exclude audit table itself)
            var entries = ctx.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Where(e => e.Entity is not AuditEvent);

            foreach (var e in entries)
            {
                var entityName = e.Entity.GetType().Name;
                var entityId = TryGetEntityId(e);

                string? beforeJson = null;
                string? afterJson = null;

                if (e.State == EntityState.Modified || e.State == EntityState.Deleted)
                {
                    var before = e.OriginalValues.Properties.ToDictionary(
                        p => p.Name,
                        p => e.OriginalValues[p]?.ToString()
                    );
                    beforeJson = JsonSerializer.Serialize(before);
                }

                if (e.State == EntityState.Added || e.State == EntityState.Modified)
                {
                    var after = e.CurrentValues.Properties.ToDictionary(
                        p => p.Name,
                        p => e.CurrentValues[p]?.ToString()
                    );
                    afterJson = JsonSerializer.Serialize(after);
                }

                ctx.AuditEvents.Add(new AuditEvent
                {
                    CreatedOnUtc = utcNow,
                    OccurredOnUtc = utcNow,
                    CreatedByUserId = _currentUser.UserId,

                    UserId = _currentUser.UserId,
                    UserEmail = _currentUser.Email,
                    UserRole = _currentUser.Role,

                    ActionType = e.State switch
                    {
                        EntityState.Added => "Create",
                        EntityState.Modified => "Update",
                        EntityState.Deleted => "Delete",
                        _ => "Unknown"
                    },

                    EntityName = entityName,
                    EntityId = entityId,

                    IpAddress = _req.IpAddress,
                    UserAgent = _req.UserAgent,
                    CorrelationId = _req.CorrelationId,
                    RequestPath = _req.Path,
                    HttpMethod = _req.Method,

                    BeforeJson = beforeJson,
                    AfterJson = afterJson,
                    Succeeded = true
                });
            }
        }

        private static string? TryGetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            // We standardise on "Id" GUID in BaseEntity, but we keep it safe.
            var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
            return idProp?.CurrentValue?.ToString() ?? idProp?.OriginalValue?.ToString();
        }
    }
}
