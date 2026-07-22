using System.ComponentModel.DataAnnotations;

namespace DentalCollegeManagementSystem_AAU.Options
{
    public sealed class ActiveDirectoryOptions
    {
        public const string SectionName = "ActiveDirectory";

        [Required]
        public string Domain { get; set; } = string.Empty;

        [Required]
        public string Server { get; set; } = string.Empty;
    }
}