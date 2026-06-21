namespace DentalCollegeManagementSystem_AAU.Models
{
    public class UserType
    {
        public int Id { get; set; }
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public string Status { get; set; } = "Active";
    }
}