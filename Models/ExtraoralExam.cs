using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    // ═══════════════════════════════════════
    //  Enums
    // ═══════════════════════════════════════

    public enum FacialSymmetryType
    {
        NormalSymmetrical,
        Asymmetric
    }

    public enum SkinColorType
    {
        Normal,
        Pale,
        Cyanotic,
        Jaundiced
    }

    public enum FacialProfileType
    {
        Straight,
        Convex,
        Concave
    }

    // ═══════════════════════════════════════
    //  ExtraoralExam — Main Model
    // ═══════════════════════════════════════

    public class ExtraoralExam
    {
        [Key]
        public int ExtraoralExamID { get; set; }

        [Required]
        public int PatientID { get; set; }

        public FacialSymmetryType FacialSymmetry { get; set; } = FacialSymmetryType.NormalSymmetrical;
        public SkinColorType SkinColor { get; set; } = SkinColorType.Normal;
        public FacialProfileType FacialProfile { get; set; } = FacialProfileType.Straight;

        public string? TMJExamination { get; set; }
        public string? LymphNodesPalpation { get; set; }
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
        public virtual ICollection<ExtraoralExamPhoto> Photos { get; set; }
            = new List<ExtraoralExamPhoto>();
    }
}