using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Admin
{
    public sealed class CreateUserVm
    {
        [Required, EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required, MinLength(10)]
        [Display(Name = "Temporary password")]
        public string TempPassword { get; set; } = "";

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } = "Beneficiary";

        [Display(Name = "Email confirmed")]
        public bool EmailConfirmed { get; set; } = true;

        // Optional: force password change on first login later (Phase 2)
        // public bool RequirePasswordChange { get; set; } = true;
    }
}
