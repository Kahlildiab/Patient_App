using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class Condition
    {
        [Key]
        public int ConditionID { get; set; }

        [Required]
        public int PatientID { get; set; }

        [Required]
        [Display(Name = "Condition")]
        public string ConditionName { get; set; } = string.Empty;

        [Display(Name = "Year of Diagnosis")]
        public int? YearOfDiagnosis { get; set; }

        [Display(Name = "Currently Active")]
        public bool IsCurrentlyActive { get; set; } = true;

        [Display(Name = "Flag this condition")]
        public bool IsFlagged { get; set; } = false;

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("PatientID")]
        public virtual Patient? Patient { get; set; }
    }
}