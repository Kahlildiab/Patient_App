using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class Competency
    {
        [Key]
        public int CompetencyID { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        public int CategoryOrder { get; set; }

        [Required]
        [MaxLength(150)]
        public string ItemName { get; set; } = string.Empty;

        public int ItemOrder { get; set; }

        // ✅ null = قالب أصلي | قيمة = سجل خاص بمريض
        public int? PatientID { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedDate { get; set; }

        // Navigation Property
        [ForeignKey("PatientID")]
        public Patient? Patient { get; set; }
    }
}