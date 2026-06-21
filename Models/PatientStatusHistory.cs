namespace DentalCollegeManagementSystem_AAU.Models
{
    public class PatientStatusHistory
    {
        public int Id { get; set; }
        public int PatientID { get; set; }
        public string OldStatus { get; set; } = "";
        public string NewStatus { get; set; } = "";
        public DateTime ChangedAt { get; set; } = DateTime.Now;

        // Navigation
        public Patient? Patient { get; set; }
    }
}