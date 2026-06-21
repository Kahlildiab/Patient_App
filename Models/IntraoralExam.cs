using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    // ═══════════════════════════════════════
    //  Enums
    // ═══════════════════════════════════════

    public enum OralHygieneStatusType
    {
        Excellent,
        Good,
        Fair,
        Poor
    }

    public enum OcclusionClassificationType
    {
        ClassI,
        ClassIIDivision1,
        ClassIIDivision2,
        ClassIII
    }

    // ═══════════════════════════════════════
    //  IntraoralExam — Main Model
    // ═══════════════════════════════════════

    public class IntraoralExam
    {
        [Key]
        public int IntraoralExamID { get; set; }

        [Required]
        public int PatientID { get; set; }

        public OralHygieneStatusType OralHygieneStatus { get; set; } = OralHygieneStatusType.Excellent;
        public string? OralMucosa { get; set; }
        public string? TongueExamination { get; set; }
        public string? Palate { get; set; }
        public string? FloorOfMouth { get; set; }
        public OcclusionClassificationType? OcclusionClassification { get; set; }
        public string? OtherFindings { get; set; }

        // ── Legacy single-photo fields (kept for backward compatibility) ──
        public string? PhotoPath { get; set; }
        public byte[]? PhotoData { get; set; }
        public string? PhotoContentType { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        // ── Navigation ──
        [ForeignKey("PatientID")]
        public virtual Patient? Patient { get; set; }

        // ✅ Navigation property — كل الصور المرتبطة بهذا الفحص
        public virtual ICollection<IntraoralExamPhoto> Photos { get; set; }
            = new List<IntraoralExamPhoto>();
    }
}