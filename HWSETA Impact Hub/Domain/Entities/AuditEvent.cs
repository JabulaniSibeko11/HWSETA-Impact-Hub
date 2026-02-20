using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public sealed class AuditEvent : BaseEntity
    {
        // Who
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserRole { get; set; }

        // What
        public string ActionType { get; set; } = "";   // Create/Update/Delete/View/Export/Login...
        public string EntityName { get; set; } = "";
        public string? EntityId { get; set; }

        // When
        public DateTime OccurredOnUtc { get; set; }

        // Where
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        // Context
        public string? CorrelationId { get; set; }
        public string? RequestPath { get; set; }
        public string? HttpMethod { get; set; }

        // Before/After (store JSON text now; later can go JSONB in PostgreSQL)
        public string? BeforeJson { get; set; }
        public string? AfterJson { get; set; }

        // Result
        public bool Succeeded { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }
}
