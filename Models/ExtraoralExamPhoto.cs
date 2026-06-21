using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    // ═══════════════════════════════════════
    //  ExtraoralExamPhoto — Photo Model
    // ═══════════════════════════════════════

    public class ExtraoralExamPhoto
    {
        [Key]
        public int ExtraoralExamPhotoID { get; set; }

        // ── Foreign Key ──
        public int ExtraoralExamID { get; set; }

        // ── Photo Info ──

        /// <summary>
        /// المسار النسبي للصورة داخل wwwroot
        /// مثال: /uploads/extraoral/1_extraoral_frontal_1.jpg
        /// </summary>
        [Required]
        public string PhotoPath { get; set; } = "";

        /// <summary>
        /// القسم اللي تنتمي له الصورة:
        /// frontal | profile | oblique | smiling | lips | forehead | extra
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string PhotoSection { get; set; } = "extra";

        public DateTime UploadedDate { get; set; } = DateTime.Now;

        // ── Navigation ──
        [ForeignKey("ExtraoralExamID")]
        public virtual ExtraoralExam? ExtraoralExam { get; set; }
    }
}