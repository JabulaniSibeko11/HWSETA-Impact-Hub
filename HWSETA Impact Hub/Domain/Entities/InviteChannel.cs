using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public enum InviteChannel
    {
        Email = 1,
        Sms = 2,
        Both = 3
    }

    public enum InviteStatus
    {
        Created = 1,
        Sent = 2,
        Used = 3,
        Revoked = 4
    }

    public sealed class BeneficiaryInvite : BaseEntity
    {
        public Guid BeneficiaryId { get; set; }
        public Beneficiary Beneficiary { get; set; } = null!;

        // Store HASH only (security)
        public string TokenHash { get; set; } = "";

        public InviteChannel Channel { get; set; } = InviteChannel.Email;
        public InviteStatus Status { get; set; } = InviteStatus.Created;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        public string? LastError { get; set; }

        // Convenience: keep who created/sent
        public string? CreatedBy { get; set; }
        public string? SentBy { get; set; }
    }
}
