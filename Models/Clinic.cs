using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class Clinic
    {
        [Key]
        public int ClinicID { get; set; }

        [Required]
        public string ClinicName { get; set; }

        public string ClinicDescription { get; set; }

        public string Address { get; set; }

        [Required]
        public int IsActive { get; set; } = 1;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        public virtual ICollection<Appointment> Appointments { get; set; }
    }
}