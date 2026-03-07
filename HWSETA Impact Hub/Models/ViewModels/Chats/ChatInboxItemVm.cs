namespace HWSETA_Impact_Hub.Models.ViewModels.Chats
{
    public sealed class ChatInboxItemVm
    {
        public Guid ThreadId { get; set; }
        public Guid BeneficiaryId { get; set; }

        public string BeneficiaryName { get; set; } = "";
        public string Subject { get; set; } = "";
        public string LastMessagePreview { get; set; } = "";
        public string LastSender { get; set; } = "";
        public DateTime LastMessageOnUtc { get; set; }

        public string Status { get; set; } = "";
        public bool HasUnreadAdminMessage { get; set; }
        public bool HasUnreadBeneficiaryMessage { get; set; }
    }
}
