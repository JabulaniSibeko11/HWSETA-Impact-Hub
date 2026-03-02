using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Registrations
{
    public sealed class UploadProofVm
    {
        [Required]
        public string Token { get; set; } = "";

        [Required(ErrorMessage = "Please upload the completion letter (PDF).")]
        public IFormFile File { get; set; } = default!;
    }
}
