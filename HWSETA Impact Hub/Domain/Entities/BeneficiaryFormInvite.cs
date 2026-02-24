using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
   

    public enum InviteDeliveryStatus
    {
        Pending = 1,
        Sent = 2,
        Failed = 3
    }

    public sealed class BeneficiaryFormInvite : BaseEntity
    {
        public Guid BeneficiaryId { get; set; }
        public Beneficiary Beneficiary { get; set; } = null!;

        public Guid FormPublishId { get; set; }
        public FormPublish FormPublish { get; set; } = null!;

        // ✅ unique token for this beneficiary+send
        public string InviteToken { get; set; } = ""; // store as URL-safe token

        public InviteChannel Channel { get; set; }
        public InviteDeliveryStatus DeliveryStatus { get; set; } = InviteDeliveryStatus.Pending;

        public int Attempts { get; set; } = 0;
        public DateTime? LastAttemptAtUtc { get; set; }
        public DateTime? SentAtUtc { get; set; }
        public string? LastError { get; set; }

        public string? SentByUserId { get; set; } // admin user id

        // submission tracking (optional but powerful)
        public Guid? FormSubmissionId { get; set; }
        public DateTime? CompletedAtUtc { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
