namespace HWSETA_Impact_Hub.Infrastructure.Confugations
{
    public sealed class ChatProfileSeedOptions
    {
        public List<ChatProfileSeedItem> Profiles { get; set; } = new();
    }

    public sealed class ChatProfileSeedItem
    {
        public string DisplayName { get; set; } = "";
        public string? AvatarColor { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
