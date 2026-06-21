using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class SocialHistory
    {
        [Key]
        public int SocialHistoryID { get; set; }

        [Required]
        public int PatientID { get; set; }

        // 1. Occupation
        public string? Occupation { get; set; }

        // 2. Smoking Status
        public SmokingStatusType SmokingStatus { get; set; } = SmokingStatusType.NeverSmoked;

        // 3. Alcohol Consumption
        public bool ConsumesAlcohol { get; set; } = false;
        public string? AlcoholType { get; set; }
        public string? AlcoholQuantity { get; set; }
        public string? AlcoholFrequency { get; set; }
        public string? AlcoholDuration { get; set; }

        // 4. Drug Abuse
        public bool UsesRecreationalDrugs { get; set; } = false;
        public string? DrugType { get; set; }
        public string? DrugQuantity { get; set; }
        public string? DrugFrequency { get; set; }
        public string? DrugDuration { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        // Navigation
        [ForeignKey("PatientID")]
        public virtual Patient? Patient { get; set; }
    }

    public enum SmokingStatusType
    {
        NeverSmoked,
        CurrentSmoker,
        FormerSmoker
    }
}