using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class XRayOrder
    {
        [Key]
        public int XRayOrderID { get; set; }

        [Required]
        public string XRayType { get; set; } = string.Empty;
        // "Periapical" | "Bitewing" | "Panoramic" | "CBCT"

        public string? ToothNumber { get; set; } // Tooth 36
        public string? Region { get; set; }
        public string? ClinicalIndication { get; set; }

        public int OrderID { get; set; }
        [ForeignKey("OrderID")]
        public Order? Order { get; set; }
    }
}