using System;
using System.Collections.Generic;
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
        /// تقرير الجلسة المنظم المولد من JavaScript.
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? ReportJson { get; set; }

        /// <summary>
        /// قيم Basic Periodontal Examination الخاصة بهذه الجلسة.
        /// مثال:
        /// {
        ///   "upperRight":"3*",
        ///   "upperAnterior":"2",
        ///   "upperLeft":"1",
        ///   "lowerRight":"4*",
        ///   "lowerAnterior":"0",
        ///   "lowerLeft":"2*"
        /// }
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(500)")]
        public string BpeJson { get; set; } =
            "{\"upperRight\":\"0\",\"upperAnterior\":\"0\",\"upperLeft\":\"0\",\"lowerRight\":\"0\",\"lowerAnterior\":\"0\",\"lowerLeft\":\"0\"}";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<DentalToothData> ToothData { get; set; }
            = new List<DentalToothData>();
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

        [ForeignKey(nameof(SessionId))]
        public virtual DentalChartSession? Session { get; set; }

        /// <summary>U0–U15 (Upper) | L0–L15 (Lower)</summary>
        [Required]
        [MaxLength(4)]
        public string ToothId { get; set; } = "";

        /// <summary>
        /// whole | buccal | lingual | palatal | mesial | distal | occlusal | root
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string AreaName { get; set; } = "";

        [MaxLength(500)]
        public string? DiseaseToolId { get; set; }

        [MaxLength(500)]
        public string? PreviousToolId { get; set; }

        [MaxLength(500)]
        public string? InsideToolId { get; set; }

        [MaxLength(500)]
        public string? OthersToolId { get; set; }

        [Column(TypeName = "nvarchar(1000)")]
        public string? Note { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  BPE DTO
    // ═══════════════════════════════════════════════════════════════
    public class BpeDataDto
    {
        public string UpperRight { get; set; } = "0";
        public string UpperAnterior { get; set; } = "0";
        public string UpperLeft { get; set; } = "0";
        public string LowerRight { get; set; } = "0";
        public string LowerAnterior { get; set; } = "0";
        public string LowerLeft { get; set; } = "0";
    }

    // ═══════════════════════════════════════════════════════════════
    //  Save / Response DTOs
    // ═══════════════════════════════════════════════════════════════
    public class SaveDentalChartDto
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public string ChartDataJson { get; set; } = "{}";

        public string? SessionNote { get; set; }
        public string? ReportJson { get; set; }

        /// <summary>
        /// BPE JSON المرسل من الصفحة. يبقى nullable للتوافق مع الجلسات القديمة.
        /// </summary>
        public string? BpeJson { get; set; }
    }

    public class DentalChartResponseDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string ChartDataJson { get; set; } = "{}";
        public string? SessionNote { get; set; }
        public string? ReportJson { get; set; }
        public string BpeJson { get; set; } = "{}";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class DentalChartHistoryDto
    {
        public int Id { get; set; }
        public string? SessionNote { get; set; }
        public string? ReportJson { get; set; }
        public string BpeJson { get; set; } = "{}";
        public DateTime UpdatedAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Report DTOs
    // ═══════════════════════════════════════════════════════════════
    public class SessionReportDto
    {
        public int SessionId { get; set; }
        public int PatientId { get; set; }
        public string? SessionNote { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TotalItems { get; set; }
        public BpeDataDto Bpe { get; set; } = new BpeDataDto();
        public List<ToothReportDto> Teeth { get; set; } = new List<ToothReportDto>();
    }

    public class ToothReportDto
    {
        public string ToothId { get; set; } = "";
        public int FdiCode { get; set; }
        public List<AreaReportDto> Areas { get; set; } = new List<AreaReportDto>();
    }

    public class AreaReportDto
    {
        public string AreaName { get; set; } = "";
        public List<ToolEntryDto> Tools { get; set; } = new List<ToolEntryDto>();
        public string? Note { get; set; }
    }

    public class ToolEntryDto
    {
        public string Group { get; set; } = "";
        public string ToolId { get; set; } = "";
    }
}
