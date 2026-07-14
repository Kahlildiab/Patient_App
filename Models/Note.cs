using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class Note
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string NoteType { get; set; } = "General";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        [StringLength(200)]
        public string CreatedBy { get; set; } = "Unknown";

        /*
         * Student notes are linked to the exact open visit.
         * Nullable is required so old notes remain valid.
         */
        public int? VisitId { get; set; }

        [StringLength(50)]
        public string CreatedByRole { get; set; } = "Unknown";

        /*
         * Valid values:
         * Pending, Approved, Rejected
         *
         * Approved is the default so old notes do not block
         * closing a visit after the migration.
         */
        [Required]
        [StringLength(20)]
        public string ApprovalStatus { get; set; } = "Approved";

        [StringLength(200)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        [ForeignKey(nameof(PatientId))]
        public Patient? Patient { get; set; }

        [ForeignKey(nameof(VisitId))]
        public Visit? Visit { get; set; }
    }
}
