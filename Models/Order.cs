using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class Order
    {
        [Key]
        public int OrderID { get; set; }

        [Required]
        public string OrderType { get; set; } = string.Empty;
        // "Consent" | "Referral" | "Medication" | "XRay" | "Discharge"

        public string OrderNumber { get; set; } = string.Empty; // ORD-001

        public string Status { get; set; } = "Pending";
        // Pending | Completed | Active | Signed | Cancelled

        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // FK
        public int PatientID { get; set; }
        [ForeignKey("PatientID")]
        public Patient? Patient { get; set; }

        public int CreatedByUserID { get; set; }
        [ForeignKey("CreatedByUserID")]
        public User? CreatedByUser { get; set; }

        // Navigation للتفاصيل
        public ConsentOrder? ConsentDetail { get; set; }
        public ReferralOrder? ReferralDetail { get; set; }
        public MedicationOrder? MedicationDetail { get; set; }
        public XRayOrder? XRayDetail { get; set; }
        public DischargeOrder? DischargeDetail { get; set; }
        public string AdminApprovalStatus { get; set; } = "Approved";
        public string? AdminApprovedBy { get; set; }
        public DateTime? AdminApprovedDate { get; set; }
    }
}