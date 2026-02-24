using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Registrations
{
    public sealed class SetPasswordVm
    {
        [Required]
        public string Token { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, MinLength(10)]
        public string Password { get; set; } = "";

        [Required]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = "";
    }
}
