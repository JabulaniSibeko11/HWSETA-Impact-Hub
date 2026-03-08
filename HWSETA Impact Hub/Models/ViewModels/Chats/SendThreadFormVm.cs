using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Chats
{
    public sealed class SendThreadFormVm
    {
        [Required]
        public Guid ThreadId { get; set; }

        [Required]
        [Display(Name = "Send As")]
        public Guid? AdminChatProfileId { get; set; }

        [Required]
        [Display(Name = "Published Form")]
        public Guid? FormPublishId { get; set; }

        [StringLength(1000)]
        [Display(Name = "Optional Note")]
        public string? Note { get; set; }

        public List<SelectListItem> AdminChatProfiles { get; set; } = new();
        public List<SelectListItem> PublishedForms { get; set; } = new();
    }
}
