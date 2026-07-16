using System.Collections.Generic;
using DentalCollegeManagementSystem_AAU.Models;

namespace DentalCollegeManagementSystem_AAU.Models.ViewModels
{
    public class SearchUserViewModel
    {
        public string? SearchTerm { get; set; }

        public string? RoleFilter { get; set; }

        public int? StatusFilter { get; set; }

        /*
         * The results now come directly from dbo.Users.
         */
        public List<User> Results { get; set; }
            = new List<User>();

        public List<string> Roles { get; set; }
            = new List<string>();
    }
}
