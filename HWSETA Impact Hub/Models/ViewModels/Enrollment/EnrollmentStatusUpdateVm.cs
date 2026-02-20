using HWSETA_Impact_Hub.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Enrollment
{
    public sealed class EnrollmentStatusUpdateVm
    {
        [Required]
        public Guid EnrollmentId { get; set; }

        [Required]
        public EnrollmentStatus Status { get; set; }

        [DataType(DataType.Date)]
        public DateTime StatusDate { get; set; } = DateTime.Today;

        [MaxLength(200)]
        public string? Reason { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }


       
    }
}
