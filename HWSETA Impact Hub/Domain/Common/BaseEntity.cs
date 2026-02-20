namespace HWSETA_Impact_Hub.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime CreatedOnUtc { get; set; }
        public string? CreatedByUserId { get; set; }

        public DateTime? UpdatedOnUtc { get; set; }
        public string? UpdatedByUserId { get; set; }

        // Optimistic concurrency (portable to PostgreSQL)
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
