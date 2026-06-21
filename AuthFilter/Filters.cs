using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DentalCollegeManagementSystem_AAU.Filters
{
    public class AuthFilter : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public AuthFilter(params string[] roles)
        {
            _allowedRoles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;
            var userRole = session.GetString("UserRole");

            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();

            if (controller == "Account" && (action == "Login" || action == "Register"))
                return;

            if (string.IsNullOrEmpty(userRole))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (_allowedRoles.Length > 0 && !_allowedRoles.Contains(userRole))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }
        }
    }
}