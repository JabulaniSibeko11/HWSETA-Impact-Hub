using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public enum ConversationThreadStatus
    {
        Open = 1,
        Closed = 2
    }

    public sealed class ConversationThread : BaseEntity
    {
        public Guid BeneficiaryId { get; set; }
        public Beneficiary Beneficiary { get; set; } = null!;

        public string Subject { get; set; } = "";
        public ConversationThreadStatus Status { get; set; } = ConversationThreadStatus.Open;

        public bool HasUnreadAdminMessage { get; set; }
        public bool HasUnreadBeneficiaryMessage { get; set; }

        public DateTime LastMessageOnUtc { get; set; }

        public ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();
    }
}
