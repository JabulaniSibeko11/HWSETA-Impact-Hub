using System.ComponentModel.DataAnnotations;

namespace HWSETA_Impact_Hub.Models.ViewModels.Enrollment
{
    public sealed class EnrollmentCreateVm
    {

  
    

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required]
        public Guid BeneficiaryId { get; set; }

        [Required]
        public Guid ProgrammeId { get; set; }

        [Required]
        public Guid ProviderId { get; set; }

        public Guid? EmployerId { get; set; }

        
    }
}
