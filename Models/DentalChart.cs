using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    /// <summary>
    /// يخزن بيانات الـ Dental Chart كاملة لكل مريض لكل جلسة
    /// </summary>
    public class DentalChart
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// رقم المريض - ربطه بجدول المرضى
        /// </summary>
        [Required]
        public int PatientId { get; set; }

        /// <summary>
        /// JSON كامل لحالة الأسنان (يُرسَل مباشرة من الـ JavaScript)
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string ChartDataJson { get; set; } = "{}";

        /// <summary>
        /// ملاحظة عامة للجلسة
        /// </summary>
        [Column(TypeName = "nvarchar(500)")]
        public string? SessionNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property — فعّلها إذا عندك Patient model
        // [ForeignKey("PatientId")]
        // public virtual Patient? Patient { get; set; }
    }
}