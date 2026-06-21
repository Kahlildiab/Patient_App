using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class Radiograph
    {
        [Key]
        public int RadiographID { get; set; }

        public int PatientID { get; set; }

        [Required]
        public string PhotoPath { get; set; } = "";

        [MaxLength(100)]
        public string FileName { get; set; } = "";

        [MaxLength(50)]
        public string RadiographType { get; set; } = "General"; // Panoramic, Periapical, Bitewing...

        public DateTime UploadedDate { get; set; } = DateTime.Now;

        [ForeignKey("PatientID")]
        public virtual Patient? Patient { get; set; }
    }
}