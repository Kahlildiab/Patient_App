using System.Collections.Generic;

namespace DentalCollegeManagementSystem_AAU.Models
{
    public class AllocatedStudentIndexViewModel
    {
        public List<AllocatedStudentRowViewModel> Students { get; set; }
            = new List<AllocatedStudentRowViewModel>();

        public string SearchName { get; set; } = string.Empty;
        public string StudentNo { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AllocationStatus { get; set; } = string.Empty;

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;

        public int TotalActiveStudents { get; set; }
        public int TotalAssignedStudents { get; set; }
        public int TotalUnassignedStudents { get; set; }
    }

    public class AllocatedStudentRowViewModel
    {
        public int AppUserId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string AccountStatus { get; set; } = string.Empty;
        public int AssignedPatientsCount { get; set; }

        public bool HasAssignedPatients => AssignedPatientsCount > 0;
    }
}