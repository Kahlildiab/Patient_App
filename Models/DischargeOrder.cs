using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class DischargeOrder
    {
        [Key]
        public int DischargeOrderID { get; set; }

        [Required]
        public string DischargeReason { get; set; } = string.Empty;
        // "Treatment Completed" | "Patient Request" | "Referral" | "Other"

        public string? AfterCareInstructions { get; set; }
        public string? FollowUpPlan { get; set; }
        public DateTime? DischargeDate { get; set; }

        public int OrderID { get; set; }
        [ForeignKey("OrderID")]
        public Order? Order { get; set; }
    }
}