using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public enum MessageChannel
    {
        Email = 1,
        Sms = 2
    }

    public enum MessageDeliveryStatus
    {
        Pending = 1,
        Sent = 2,
        Failed = 3
    }

    public sealed class OutboundMessageLog : BaseEntity
    {
        public Guid BeneficiaryId { get; set; }
        public Beneficiary Beneficiary { get; set; } = null!;

        public MessageChannel Channel { get; set; }
        public MessageDeliveryStatus Status { get; set; } = MessageDeliveryStatus.Pending;

        public string To { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Body { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
        public string? ProviderMessageId { get; set; }
        public string? Error { get; set; }
    }
}
