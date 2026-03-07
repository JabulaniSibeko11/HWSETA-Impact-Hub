using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public enum ConversationSenderType
    {
        Admin = 1,
        Beneficiary = 2
    }

    public sealed class ConversationMessage : BaseEntity
    {
        public Guid ThreadId { get; set; }
        public ConversationThread Thread { get; set; } = null!;

        public ConversationSenderType SenderType { get; set; }

        public Guid? BeneficiaryId { get; set; }
        public Beneficiary? Beneficiary { get; set; }

        public string? SenderUserId { get; set; }
        public string SenderDisplayName { get; set; } = "";

        public string MessageText { get; set; } = "";
        public bool IsRead { get; set; }

        public DateTime SentOnUtc { get; set; }

        public Guid? AdminChatProfileId { get; set; }
        public AdminChatProfile? AdminChatProfile { get; set; }

    }
}
