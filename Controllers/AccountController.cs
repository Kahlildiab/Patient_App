using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;
using DentalCollegeManagementSystem_AAU.Models.ViewModels;
using DentalCollegeManagementSystem_AAU.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        private readonly ActiveDirectoryValidator
            _activeDirectoryValidator;

        private readonly JwtTokenService
            _jwtTokenService;

        public AccountController(
            AppDbContext context,
            ActiveDirectoryValidator activeDirectoryValidator,
            JwtTokenService jwtTokenService)
        {
            _context = context;

            _activeDirectoryValidator =
                activeDirectoryValidator;

            _jwtTokenService =
                jwtTokenService;
        }

        // =====================================================
        // INDEX
        // =====================================================

        public IActionResult Index()
        {
            return View();
        }

        // =====================================================
        // LOGIN - GET
        // =====================================================

        [HttpGet]
        public IActionResult Login()
        {
            string? currentRole =
                HttpContext.Session.GetString(
                    "UserRole"
                );

            if (!string.IsNullOrWhiteSpace(currentRole))
            {
                return RedirectToAction(
                    "Index",
                    "Home"
                );
            }

            return View(
                new LoginViewModel()
            );
        }

        // =====================================================
        // LOGIN - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string enteredUsername =
                model.Username.Trim();

            /*
             * أولًا:
             * البحث عن المستخدم داخل جدول Users.
             */
            var localUser =
                await _context.Users
                    .FirstOrDefaultAsync(
                        user =>
                            user.Username
                            == enteredUsername
                    );

            /*
             * حساب Admin المحلي:
             *
             * يدخل باستخدام Username وPassword
             * الموجودين في جدول Users.
             *
             * لا يتم إرسال Admin إلى Active Directory.
             */
            if (
                localUser != null
                &&
                string.Equals(
                    localUser.UserRole,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                if (
                    localUser.Password
                    != model.Password
                )
                {
                    ModelState.AddModelError(
                        "",
                        "❌ Invalid username or password."
                    );

                    return View(model);
                }

                if (localUser.IsActive == 0)
                {
                    ModelState.AddModelError(
                        "",
                        "⏳ Your account is not active."
                    );

                    return View(model);
                }

                await CreateUserSessionAsync(
                    user: localUser,
                    username: localUser.Username,
                    fullName: localUser.FullName,
                    email: localUser.Email,
                    givenName: localUser.FullName,
                    surname: string.Empty
                );

                localUser.LastLoginDate =
                    DateTime.Now;

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    "Index",
                    "Home"
                );
            }

            /*
             * باقي المستخدمين:
             * يتم فحص Username وPassword
             * عن طريق Active Directory.
             */
            ActiveDirectoryUserInfo? adUser;

            try
            {
                adUser =
                    _activeDirectoryValidator
                        .Authenticate(
                            enteredUsername,
                            model.Password
                        );
            }
            catch (
                ActiveDirectoryUnavailableException
            )
            {
                ModelState.AddModelError(
                    "",
                    "❌ The university login service "
                    + "is currently unavailable. "
                    + "Please contact IT support."
                );

                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "❌ An error occurred while "
                    + "connecting to Active Directory."
                );

                return View(model);
            }

            if (adUser == null)
            {
                ModelState.AddModelError(
                    "",
                    "❌ Invalid university "
                    + "username or password."
                );

                return View(model);
            }

            /*
             * بعد نجاح Active Directory:
             *
             * البحث عن المستخدم في جدول Users
             * لتحديد:
             *
             * UserRole
             * IsActive
             * UserID
             */
            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        currentUser =>
                            currentUser.Username
                                == adUser.UserName
                            ||
                            (
                                !string.IsNullOrWhiteSpace(
                                    adUser.Email
                                )
                                &&
                                currentUser.Email
                                    == adUser.Email
                            )
                    );

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "❌ Your university account "
                    + "is valid, but you are not "
                    + "registered in this system."
                );

                return View(model);
            }

            if (user.IsActive == 0)
            {
                ModelState.AddModelError(
                    "",
                    "⏳ Your account is pending "
                    + "approval from the Manager."
                );

                return View(model);
            }

            string fullName =
                !string.IsNullOrWhiteSpace(
                    user.FullName
                )
                    ? user.FullName
                    : adUser.DisplayName;

            string email =
                !string.IsNullOrWhiteSpace(
                    user.Email
                )
                    ? user.Email
                    : adUser.Email;

            await CreateUserSessionAsync(
                user: user,
                username: adUser.UserName,
                fullName: fullName,
                email: email,
                givenName: adUser.GivenName,
                surname: adUser.Surname
            );

            user.LastLoginDate =
                DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Index",
                "Home"
            );
        }

        // =====================================================
        // CREATE SESSION + JWT
        // =====================================================

        private async Task CreateUserSessionAsync(
            User user,
            string username,
            string fullName,
            string email,
            string givenName,
            string surname)
        {
            HttpContext.Session.SetString(
                "UserID",
                user.UserID.ToString()
            );

            HttpContext.Session.SetString(
                "Username",
                username ?? string.Empty
            );

            HttpContext.Session.SetString(
                "FullName",
                fullName ?? string.Empty
            );

            HttpContext.Session.SetString(
                "UserRole",
                user.UserRole ?? string.Empty
            );

            HttpContext.Session.SetString(
                "UserEmail",
                email ?? string.Empty
            );

            /*
             * جلب AppUserID:
             *
             * تتم المطابقة باستخدام UserLog
             * أو Email.
             */
            var appUser =
                await _context.AppUsers
                    .FirstOrDefaultAsync(
                        currentAppUser =>
                            currentAppUser.Status
                                == "Active"
                            &&
                            (
                                currentAppUser.UserLog
                                    == username
                                ||
                                (
                                    !string.IsNullOrWhiteSpace(
                                        email
                                    )
                                    &&
                                    currentAppUser.Email
                                        == email
                                )
                            )
                    );

            if (appUser != null)
            {
                HttpContext.Session.SetString(
                    "AppUserID",
                    appUser.Id.ToString()
                );
            }
            else
            {
                HttpContext.Session.Remove(
                    "AppUserID"
                );
            }

            /*
             * إنشاء JWT Token.
             */
            string token =
                _jwtTokenService.GenerateToken(
                    user.UserID,
                    username ?? string.Empty,
                    givenName ?? string.Empty,
                    surname ?? string.Empty,
                    user.UserRole ?? string.Empty
                );

            HttpContext.Session.SetString(
                "AccessToken",
                token
            );
        }

        // =====================================================
        // REGISTER - GET
        // =====================================================

        [HttpGet]
        public IActionResult Register()
        {
            string? currentRole =
                HttpContext.Session.GetString(
                    "UserRole"
                );

            if (!string.IsNullOrWhiteSpace(currentRole))
            {
                return RedirectToAction(
                    "Index",
                    "Home"
                );
            }

            return View(
                new RegisterViewModel()
            );
        }

        // =====================================================
        // REGISTER - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string username =
                model.Username.Trim();

            string email =
                model.Email.Trim();

            bool exists =
                await _context.Users
                    .AnyAsync(
                        user =>
                            user.Username == username
                            ||
                            user.Email == email
                    );

            if (exists)
            {
                ModelState.AddModelError(
                    "",
                    "❌ Username or Email already exists."
                );

                return View(model);
            }

            /*
             * Active Directory سيقوم بفحص
             * كلمة المرور الحقيقية.
             *
             * لذلك لا نخزن كلمة مرور المستخدم
             * الحقيقية في قاعدة البيانات.
             *
             * يتم وضع قيمة عشوائية فقط لأن
             * عمود Password موجود حاليًا.
             */
            var user =
                new User
                {
                    Username =
                        username,

                    FullName =
                        model.FullName,

                    Email =
                        email,

                    PhoneNumber =
                        model.PhoneNumber,

                    Password =
                        Guid.NewGuid()
                            .ToString("N"),

                    UserRole =
                        "Receptionist",

                    IsActive =
                        0,

                    CreatedDate =
                        DateTime.Now
                };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            TempData["RegisterSuccess"] =
                "✅ Registration successful! "
                + "Use your university password "
                + "after your account is approved.";

            return RedirectToAction(
                "Login"
            );
        }

        // =====================================================
        // LOGOUT
        // =====================================================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Login",
                "Account"
            );
        }

        // =====================================================
        // PENDING USERS
        // =====================================================

        public async Task<IActionResult>
            PendingUsers()
        {
            string? role =
                HttpContext.Session.GetString(
                    "UserRole"
                );

            if (
                !string.Equals(
                    role,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase
                )
                &&
                !string.Equals(
                    role,
                    "Manager",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var pending =
                await _context.Users
                    .Where(
                        user =>
                            user.IsActive == 0
                    )
                    .OrderByDescending(
                        user =>
                            user.CreatedDate
                    )
                    .ToListAsync();

            return View(pending);
        }

        // =====================================================
        // APPROVE USER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            ApproveUser(int id)
        {
            string? role =
                HttpContext.Session.GetString(
                    "UserRole"
                );

            if (
                !string.Equals(
                    role,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase
                )
                &&
                !string.Equals(
                    role,
                    "Manager",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var user =
                await _context.Users
                    .FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            user.IsActive =
                1;

            user.ModifiedDate =
                DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"✅ {user.FullName} "
                + "has been approved!";

            return RedirectToAction(
                "PendingUsers"
            );
        }

        // =====================================================
        // REJECT USER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            RejectUser(int id)
        {
            string? role =
                HttpContext.Session.GetString(
                    "UserRole"
                );

            if (
                !string.Equals(
                    role,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase
                )
                &&
                !string.Equals(
                    role,
                    "Manager",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var user =
                await _context.Users
                    .FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "🗑️ User has been rejected "
                + "and removed.";

            return RedirectToAction(
                "PendingUsers"
            );
        }

        // =====================================================
        // ACCESS DENIED
        // =====================================================

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}