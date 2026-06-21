using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class Patient
    {
        [Key]
        public int PatientID { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string SecondName { get; set; }

        public string ThirdName { get; set; }

        [Required]
        public string FourthName { get; set; }

        [Required]
        [Display(Name = "National ID / Passport Number")]
        public string NationalID_PassportNumber { get; set; }

        [Required]
        [Display(Name = "Nationality")]
        public int Nationality { get; set; }

        [Required]
        public string Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [Required]
        [StringLength(15)]
        public string PhoneNumber { get; set; }

        [StringLength(255)]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Appointment Date")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [Display(Name = "Appointment Time")]
        public string AppointmentTime { get; set; }

        public string? FatherName { get; set; }

        [StringLength(15)]
        public string? FatherPhone { get; set; }

        public string? MotherName { get; set; }

        [StringLength(15)]
        public string? MotherPhone { get; set; }

        [Required]
        public int StatusID { get; set; } = 1;

        [ForeignKey("StatusID")]
        public Status? Status { get; set; }

        // ✅ الصورة الشخصية - اختياري
        [NotMapped]
        public IFormFile? ProfilePhotoFile { get; set; }

        public string? ProfilePhotoPath { get; set; } // المسار المحفوظ في DB
        public string? AttendanceStatus { get; set; }



        public string? PatientStatus { get; set; } // "Screening" | "Allocated" | "Discharged"
    }
}