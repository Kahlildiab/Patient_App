using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class MedicationOrder
    {
        [Key]
        public int MedicationOrderID { get; set; }

        [Required]
        public string MedicationName { get; set; } = string.Empty; // Amoxicillin

        [Required]
        public string Dosage { get; set; } = string.Empty; // 500mg

        [Required]
        public string Frequency { get; set; } = string.Empty; // 3 times daily

        public int DurationDays { get; set; } // 7

        public string? Instructions { get; set; }

        public int OrderID { get; set; }
        [ForeignKey("OrderID")]
        public Order? Order { get; set; }
    }
}