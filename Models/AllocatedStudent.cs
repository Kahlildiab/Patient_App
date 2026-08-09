using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class AllocatedStudent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PatientID { get; set; }

        [ForeignKey("PatientID")]
        public Patient Patient { get; set; }

        [Required]
        public int AppUserId { get; set; }

        [ForeignKey("AppUserId")]
        public AppUser AppUser { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        // True = currently assigned, False = previous assignment.
        public bool IsActive { get; set; } = true;

        // Date and time when the student was removed from the patient.
        public DateTime? RemovedDate { get; set; }
    }
}