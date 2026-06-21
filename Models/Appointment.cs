using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentID { get; set; }
        public int PatientID { get; set; }

        [Required]
        [StringLength(100)]
        public string ClinicName { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [StringLength(20)]
        public string AppointmentDay { get; set; }

        [Required]
        public TimeSpan TimeFrom { get; set; }

        [Required]
        public TimeSpan TimeTo { get; set; }

        [Required]
        [StringLength(20)]
        public string AppointmentStatus { get; set; } = "Scheduled";

        // ✅ أضف هاد
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("PatientID")]
        [ValidateNever]
        public virtual Patient Patient { get; set; }
    }
}