using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    // ═══════════════════════════════════════════════════════════════
    //  DentalChartSession — جدول الجلسات
    // ═══════════════════════════════════════════════════════════════
    public class DentalChartSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Column(TypeName = "nvarchar(500)")]
        public string? SessionNote { get; set; }

        /// <summary>
        /// تقرير الجلسة النصي — يُولَّد تلقائياً عند الحفظ
        /// مثال: "Tooth 11: Caries (buccal) | Tooth 21: Root Canal (root)"
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? ReportJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<DentalToothData> ToothData { get; set; } = new List<DentalToothData>();
    }

    // ═══════════════════════════════════════════════════════════════
    //  DentalToothData — جدول بيانات الأسنان
    // ═══════════════════════════════════════════════════════════════
    public class DentalToothData
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SessionId { get; set; }

        [ForeignKey("SessionId")]
        public virtual DentalChartSession? Session { get; set; }

        /// <summary>U0–U15 (Upper) | L0–L15 (Lower)</summary>
        [Required]
        [MaxLength(4)]
        public string ToothId { get; set; } = "";

        /// <summary>whole | buccal | lingual | palatal | mesial | distal | occlusal | root</summary>
        [Required]
        [MaxLength(20)]
        public string AreaName { get; set; } = "";

        [MaxLength(500)] public string? DiseaseToolId { get; set; }
        [MaxLength(500)] public string? PreviousToolId { get; set; }
        [MaxLength(500)] public string? InsideToolId { get; set; }
        [MaxLength(500)] public string? OthersToolId { get; set; }

        [Column(TypeName = "nvarchar(1000)")]
        public string? Note { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  DTOs
    // ═══════════════════════════════════════════════════════════════

    public class SaveDentalChartDto
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public string ChartDataJson { get; set; } = "{}";

        public string? SessionNote { get; set; }

        /// <summary>
        /// تقرير اختياري مولَّد من الـ JS
        /// إذا ما أُرسل، الـ Controller بيولّده تلقائياً
        /// </summary>
        public string? ReportJson { get; set; }
    }

    public class DentalChartResponseDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string ChartDataJson { get; set; } = "{}";
        public string? SessionNote { get; set; }
        public string? ReportJson { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class DentalChartHistoryDto
    {
        public int Id { get; set; }
        public string? SessionNote { get; set; }
        public string? ReportJson { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Report DTOs — بنية التقرير المنظّم
    // ═══════════════════════════════════════════════════════════════

    /// <summary>تقرير كامل للجلسة</summary>
    public class SessionReportDto
    {
        public int SessionId { get; set; }
        public int PatientId { get; set; }
        public string? SessionNote { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TotalItems { get; set; }
        public List<ToothReportDto> Teeth { get; set; } = new();
    }

    /// <summary>تقرير سن واحد</summary>
    public class ToothReportDto
    {
        public string ToothId { get; set; } = "";
        public int FdiCode { get; set; }
        public List<AreaReportDto> Areas { get; set; } = new();
    }

    /// <summary>تقرير منطقة واحدة داخل السن</summary>
    public class AreaReportDto
    {
        public string AreaName { get; set; } = "";
        public List<ToolEntryDto> Tools { get; set; } = new();
        public string? Note { get; set; }
    }

    /// <summary>أداة واحدة مطبّقة</summary>
    public class ToolEntryDto
    {
        public string Group { get; set; } = "";   // disease | previous | inside | others
        public string ToolId { get; set; } = "";   // caries | rct | ...
    }
}

// ═══════════════════════════════════════════════════════════════════
//  Migration hint — أضف في DbContext:
//
//  public DbSet<DentalChartSession> DentalChartSessions { get; set; }
//  public DbSet<DentalToothData>    DentalToothData     { get; set; }
//
//  OnModelCreating:
//  modelBuilder.Entity<DentalChartSession>()
//      .HasIndex(s => s.PatientId);
//  modelBuilder.Entity<DentalToothData>()
//      .HasIndex(d => new { d.SessionId, d.ToothId, d.AreaName })
//      .IsUnique();
//
//  ثم: Add-Migration AddReportJson  →  Update-Database
// ═══════════════════════════════════════════════════════════════════