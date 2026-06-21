using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    // ═══════════════════════════════════════
    //  IntraoralExamPhoto — Photo Model
    // ═══════════════════════════════════════

    public class IntraoralExamPhoto
    {
        [Key]
        public int IntraoralExamPhotoID { get; set; }

        // ── Foreign Key ──
        public int IntraoralExamID { get; set; }

        // ── Photo Info ──

        /// <summary>
        /// المسار النسبي للصورة داخل wwwroot
        /// مثال: /uploads/intraoral/1_intraoral_upper_arch_1.jpg
        /// </summary>
        [Required]
        public string PhotoPath { get; set; } = "";

        /// <summary>
        /// القسم اللي تنتمي له الصورة:
        /// upper_arch | lower_arch | frontal_occlusion | right_lateral | left_lateral | palate | extra
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string PhotoSection { get; set; } = "extra";

        public DateTime UploadedDate { get; set; } = DateTime.Now;

        // ── Navigation ──
        [ForeignKey("IntraoralExamID")]
        public virtual IntraoralExam? IntraoralExam { get; set; }
    }
}