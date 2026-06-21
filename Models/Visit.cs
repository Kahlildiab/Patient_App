using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class Visit
    {
        [Key]
        public int VisitID { get; set; }

        [Required]
        public int PatientID { get; set; }

        [ForeignKey("PatientID")]
        public Patient? Patient { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime VisitDate { get; set; } = DateTime.Today;

        // AM or PM
        [StringLength(2)]
        public string? AppointmentPeriod { get; set; }

        [StringLength(500)]
        public string? ChiefComplaint { get; set; }

        [StringLength(1000)]
        public string? ProceduresPerformed { get; set; }

        [StringLength(500)]
        public string? MaterialsUsed { get; set; }

        [StringLength(500)]
        public string? Complications { get; set; }

        [StringLength(1000)]
        public string? StudentNotes { get; set; }

        // Supervisor Approval
        public bool IsApproved { get; set; } = false;

        [StringLength(200)]
        public string? ApprovedBy { get; set; }
        public bool Attended { get; set; }
        public DateTime? ApprovedDate { get; set; }

        [StringLength(500)]
        public string? SupervisorComments { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string AdminApprovalStatus { get; set; } = "Approved";
        public string? AdminApprovedBy { get; set; }
        public DateTime? AdminApprovedDate { get; set; }
    }
}