using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    [Table("TreatmentPlanDiagnoses")]
    public class TreatmentPlanDiagnosis
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        [StringLength(4000)]
        public string DiagnosisAndTreatmentPlan { get; set; }
            = string.Empty;

        [Required]
        [StringLength(50)]
        public string AdminApprovalStatus { get; set; }
            = "Pending";

        [StringLength(200)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }
            = DateTime.Now;

        [StringLength(200)]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(200)]
        public string? AdminApprovedBy { get; set; }

        public DateTime? AdminApprovedDate { get; set; }

        [ForeignKey(nameof(PatientId))]
        public Patient? Patient { get; set; }
    }
}
