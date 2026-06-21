using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class ConsentOrder
    {
        [Key]
        public int ConsentOrderID { get; set; }

        [Required]
        public string ConsentType { get; set; } = string.Empty;
        // "Generic" | "Surgical" | "Anesthesia" | "Photography"

        public string? Description { get; set; }
        public bool IsSigned { get; set; } = false;
        public DateTime? SignedDate { get; set; }

        public int OrderID { get; set; }
        [ForeignKey("OrderID")]
        public Order? Order { get; set; }
    }
}