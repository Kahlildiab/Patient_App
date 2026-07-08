using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class Competency
    {
        [Key]
        public int CompetencyID { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } =
            string.Empty;

        public int CategoryOrder { get; set; }

        [Required]
        [MaxLength(150)]
        public string ItemName { get; set; } =
            string.Empty;

        public int ItemOrder { get; set; }

        public int? PatientID { get; set; }

        [ForeignKey(nameof(PatientID))]
        public Patient? Patient { get; set; }

        public int? StudentUserID { get; set; }

        [ForeignKey(nameof(StudentUserID))]
        public User? Student { get; set; }

        public int? TemplateCompetencyID { get; set; }

        [ForeignKey(nameof(TemplateCompetencyID))]
        public Competency? TemplateCompetency { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedDate { get; set; }

        [MaxLength(200)]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}