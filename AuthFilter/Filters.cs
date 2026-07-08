using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DentalCollegeManagementSystem_AAU.Filters
{
    public class AuthFilter : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public AuthFilter(params string[] roles)
        {
            _allowedRoles = roles ?? Array.Empty<string>();
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            /*
             * مهم:
             * إذا كان الـController أو الـAction يحتوي على [AllowAnonymous]
             * لا يتم فحص Session ولا يتم تحويل المستخدم إلى Login.
             */
            var endpoint = context.HttpContext.GetEndpoint();

            var allowAnonymous =
                endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null;

            if (allowAnonymous)
            {
                return;
            }

            var session = context.HttpContext.Session;

            string? userRole =
                session.GetString("UserRole");

            string controller =
                context.RouteData.Values["controller"]?
                    .ToString() ?? string.Empty;

            string action =
                context.RouteData.Values["action"]?
                    .ToString() ?? string.Empty;

            /*
             * السماح بصفحات الحساب العامة.
             */
            if (
                controller.Equals(
                    "Account",
                    StringComparison.OrdinalIgnoreCase
                )
                &&
                (
                    action.Equals(
                        "Login",
                        StringComparison.OrdinalIgnoreCase
                    )
                    ||
                    action.Equals(
                        "Register",
                        StringComparison.OrdinalIgnoreCase
                    )
                    ||
                    action.Equals(
                        "AccessDenied",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            {
                return;
            }

            /*
             * لا توجد جلسة دخول.
             */
            if (string.IsNullOrWhiteSpace(userRole))
            {
                context.Result =
                    new RedirectToActionResult(
                        "Login",
                        "Account",
                        null
                    );

                return;
            }

            /*
             * فحص الصلاحيات المحددة داخل AuthFilter.
             *
             * مثال:
             * [AuthFilter("Admin", "Doctor")]
             */
            if (
                _allowedRoles.Length > 0
                &&
                !_allowedRoles.Any(
                    role =>
                        string.Equals(
                            role,
                            userRole,
                            StringComparison.OrdinalIgnoreCase
                        )
                )
            )
            {
                context.Result =
                    new RedirectToActionResult(
                        "AccessDenied",
                        "Account",
                        null
                    );

                return;
            }
        }
    }
}