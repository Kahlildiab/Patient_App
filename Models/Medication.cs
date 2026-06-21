using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class Medication
    {
        [Key]
        public int MedicationID { get; set; }
        [Required]
        public int PatientID { get; set; }
        [Required]
        [StringLength(200)]
        public string DrugName { get; set; }
        public string? Dose { get; set; }
        public string? Frequency { get; set; }
        public string? Duration { get; set; }
        // Checkboxes
        public string? FlagType { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("PatientID")]
        public virtual Patient? Patient { get; set; }
    }
}
