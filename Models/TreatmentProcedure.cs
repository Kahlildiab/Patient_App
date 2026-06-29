using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class TreatmentProcedure
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public TreatmentCategory Category { get; set; }

        [Required]
        [StringLength(50)]
        public string ToothNumber { get; set; }
            = string.Empty;

        [Required]
        [StringLength(500)]
        public string Procedure { get; set; }
            = string.Empty;

        [Range(1, 100)]
        public int EstimatedSessions { get; set; }
            = 1;

        [Required]
        public TreatmentPriority Priority { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        [Required]
        public ProcedureStatus Status { get; set; }
            = ProcedureStatus.Planned;

        public DateTime? CompletionDate { get; set; }

        [Required]
        [StringLength(50)]
        public string AdminApprovalStatus { get; set; }
            = "Approved";

        [StringLength(200)]
        public string? AdminApprovedBy { get; set; }

        public DateTime? AdminApprovedDate { get; set; }

        [ForeignKey(nameof(PatientId))]
        public virtual Patient? Patient { get; set; }
    }

    public enum TreatmentCategory
    {
        [Display(Name = "Emergency Treatment")]
        EmergencyTreatment,

        [Display(Name = "Stabilization Treatment")]
        StabilizationTreatment,

        [Display(Name = "Definitive Treatment")]
        DefinitiveTreatment,

        [Display(Name = "Maintenance Treatment")]
        MaintenanceTreatment
    }

    public enum TreatmentPriority
    {
        Urgent,
        High,
        Routine
    }

    public enum ProcedureStatus
    {
        Planned,

        [Display(Name = "In Progress")]
        InProgress,

        Completed
    }
}
