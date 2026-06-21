using System;
using System.ComponentModel.DataAnnotations;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        [StringLength(255)]
        public string Password { get; set; }

        [Required]
        public string FullName { get; set; }
        [Required]

        public string Email { get; set; }

        [StringLength(15)]
        public string PhoneNumber { get; set; }

        [Required]
        public string UserRole { get; set; } = "Reception";

        [Required]
        public int IsActive { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastLoginDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        
    }
}