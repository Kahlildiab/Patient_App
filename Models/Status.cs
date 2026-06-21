using System;
using System.ComponentModel.DataAnnotations;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class Status
    {
        [Key]
        public int StatusID { get; set; }

        [Required]
        public string StatusName { get; set; }

        public string StatusDescription { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}