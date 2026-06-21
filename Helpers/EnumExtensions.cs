// Helpers/EnumExtensions.cs
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace DentalCollegeManagementSystem_AAU.Helpers
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum value)
        {
            var member = value.GetType()
                              .GetMember(value.ToString())
                              .FirstOrDefault();

            var display = member?.GetCustomAttribute<DisplayAttribute>();
            return display?.Name ?? value.ToString();
        }
    }
}
