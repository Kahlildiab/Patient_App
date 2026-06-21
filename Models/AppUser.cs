namespace DentalCollegeManagementSystem_AAU.Models
{
    public class AppUser
    {
        public int Id { get; set; }
        public string UserLog { get; set; }
        public string Email { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public string Mobile { get; set; }
        public int UserTypeId { get; set; }
        public UserType UserType { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; } = "Active";
    }
}