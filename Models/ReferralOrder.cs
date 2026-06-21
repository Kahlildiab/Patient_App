using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class ReferralOrder
    {
        [Key]
        public int ReferralOrderID { get; set; }

        [Required]
        public string ReferredTo { get; set; } = string.Empty; // Dr. Martinez

        [Required]
        public string Specialty { get; set; } = string.Empty; // Oral Surgery

        public string? Reason { get; set; }
        public DateTime? ReferralDate { get; set; }

        public int OrderID { get; set; }
        [ForeignKey("OrderID")]
        public Order? Order { get; set; }
    }
}