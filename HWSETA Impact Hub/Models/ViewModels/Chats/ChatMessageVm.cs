namespace HWSETA_Impact_Hub.Models.ViewModels.Chats
{
    public sealed class ChatMessageVm
    {
        public Guid MessageId { get; set; }
        public string SenderDisplayName { get; set; } = "";
        public string SenderType { get; set; } = "";
        public string MessageText { get; set; } = "";
        public DateTime SentOnUtc { get; set; }
        public bool IsMine { get; set; }

        public bool IsFormShareMessage { get; set; }
        public Guid? FormPublishId { get; set; }
        public Guid? BeneficiaryFormInviteId { get; set; }
        public string? FormTitle { get; set; }
    }
}
