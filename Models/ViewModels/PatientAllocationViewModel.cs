namespace DentalCollegeManagementSystem_AAU.Models
{
    public class PatientAllocationViewModel
    {
        public Patient Patient { get; set; }
        public List<AppUser> AssignedStudents { get; set; } = new();
        public List<AppUser> AvailableStudents { get; set; } = new();
    }
}