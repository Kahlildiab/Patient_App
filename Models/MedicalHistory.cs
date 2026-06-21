using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public enum ChangesStatusType
    {
        NoChanges,
        ChangesReported
    }

    public class MedicalHistory
    {
        [Key]
        public int MedicalHistoryID { get; set; }

        [Required]
        public int PatientID { get; set; }

        // ── Verification ──────────────────────────────
        public bool MedicalHistoryVerified { get; set; }


        // ── Visit Review ──────────────────────────────
        public bool MedicalHistoryReviewedThisVisit { get; set; }
        public ChangesStatusType? ChangesStatus { get; set; }
        public DateTime? ReviewDate { get; set; }
        public string? AllergyNotes { get; set; }

        // ── Case Complexity ───────────────────────────
        // 0 = Easy | 1 = Medium | 2 = Hard
        public int? CaseComplexity { get; set; }

        // ── Allergies ─────────────────────────────────
        public bool Allergies { get; set; }
        public string? OngoingTreatment { get; set; }

        // ── Recent Hospitalization ────────────────────
        public bool RecentHospitalization { get; set; }
        public string? HospitalizationNotes { get; set; }
        // ── Pregnancy ─────────────────────────────────
        public bool IsPregnant { get; set; }
        public bool IsBreastfeeding { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [ForeignKey("PatientID")]
        public virtual Patient? Patient { get; set; }
    }
}