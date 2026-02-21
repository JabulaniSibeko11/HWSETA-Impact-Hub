using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Enrollment
{
    public sealed class EnrollmentDocumentUploadVm
    {
        [Required]
        public Guid EnrollmentId { get; set; }

        [Required]
        public Guid DocumentTypeId { get; set; }

        [Required]
        public IFormFile File { get; set; } = null!;

        public List<SelectListItem> DocumentTypes { get; set; } = new();
    }
}
