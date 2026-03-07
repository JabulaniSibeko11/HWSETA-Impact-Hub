using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public sealed class AdminChatProfile : BaseEntity
    {
        public string DisplayName { get; set; } = "";
        public string? AvatarColor { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
