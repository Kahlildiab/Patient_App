// Models/Note.cs
namespace DentalCollegeManagementSystem_AAU.Models
{
    public class Note
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string Content { get; set; }
        public string NoteType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }
        public Patient Patient { get; set; }
    }
}