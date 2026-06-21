// Models/TreatmentProcedure.cs
using System.ComponentModel.DataAnnotations;

namespace DentalCollegeManagementSystem_AAU.Models  
{
    public class TreatmentProcedure
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public TreatmentCategory Category { get; set; }
        public string ToothNumber { get; set; }
        public string Procedure { get; set; }
        public int EstimatedSessions { get; set; }
        public TreatmentPriority Priority { get; set; }
        public string Notes { get; set; }
        public ProcedureStatus Status { get; set; }
        public DateTime? CompletionDate { get; set; }

        public virtual Patient Patient { get; set; }
        public string AdminApprovalStatus { get; set; } = "Approved";
        public string? AdminApprovedBy { get; set; }
        public DateTime? AdminApprovedDate { get; set; }
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