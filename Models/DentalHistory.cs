using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class DentalHistory
    {
        [Key]
        public int DentalHistoryID { get; set; }

        [ForeignKey("Patient")]
        public int PatientID { get; set; }
        public Patient? Patient { get; set; }

        // First Appointment Details
        public DateTime? FirstAppointmentDate { get; set; }
        public bool? AttendedAppointment { get; set; }

        // Previous Dental Attendance
        public string? FrequencyOfDentalCheckup { get; set; }

        // Oral Hygiene Practice
        public string? BrushingFrequency { get; set; }
        public string? BrushingTechnique { get; set; }
        public string? TypeOfToothbrush { get; set; }
        public string? TypeOfToothpaste { get; set; }

        // Adjunctive Hygiene Methods
        public bool DentalFloss { get; set; }
        public bool InterdentalBrushes { get; set; }
        public bool Mouthwash { get; set; }
        public bool TongueCleaner { get; set; }
        public bool WaterFlosser { get; set; }
    }
}