using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class PatientPhoto
    {
        [Key]
        public int PatientPhotoID { get; set; }

        public int PatientID { get; set; }

        [Required]
        public string PhotoPath { get; set; } = "";

        [MaxLength(100)]
        public string FileName { get; set; } = "";

        public string Notes { get; set; } = "";

        public DateTime UploadedDate { get; set; } = DateTime.Now;

        [ForeignKey("PatientID")]
        public virtual Patient? Patient { get; set; }
    }
}