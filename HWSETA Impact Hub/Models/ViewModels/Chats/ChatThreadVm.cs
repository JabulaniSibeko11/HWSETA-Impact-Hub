using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Chats
{
    public sealed class ChatThreadVm
    {
        public Guid ThreadId { get; set; }
        public Guid BeneficiaryId { get; set; }

        public string BeneficiaryName { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Status { get; set; } = "";

        public List<ChatMessageVm> Messages { get; set; } = new();

        [Required]
        [Display(Name = "Reply As")]
        public Guid? AdminChatProfileId { get; set; }

        public List<SelectListItem> AdminChatProfiles { get; set; } = new();

        [Required]
        [StringLength(4000)]
        public string ReplyText { get; set; } = "";
    }
}
