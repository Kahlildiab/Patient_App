namespace DentalCollegeManagementSystem_AAU.Models.ViewModels
{
    public class SearchUserViewModel
    {
        public string SearchTerm { get; set; }
        public List<AppUser> Results { get; set; } = new();
    }
}