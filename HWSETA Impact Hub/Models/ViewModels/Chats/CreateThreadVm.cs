using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Chats
{
    public sealed class CreateThreadVm
    {
        [Required]
        public Guid BeneficiaryId { get; set; }

        [Required]
        [StringLength(300)]
        public string Subject { get; set; } = "";

        [Required]
        [StringLength(4000)]
        public string MessageText { get; set; } = "";

        [Required]
        [Display(Name = "Send As")]
        public Guid? AdminChatProfileId { get; set; }

        public List<SelectListItem> Beneficiaries { get; set; } = new();
        public List<SelectListItem> AdminChatProfiles { get; set; } = new();
    }
}