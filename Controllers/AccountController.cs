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
        private readonly ActiveDirectoryValidator _activeDirectoryValidator;
        private readonly JwtTokenService _jwtTokenService;
        private readonly ILogger<AccountController> _logger;
        private readonly string _domain;

        public AccountController(
            AppDbContext context,
            ActiveDirectoryValidator activeDirectoryValidator,
            JwtTokenService jwtTokenService,
            IConfiguration configuration,
            ILogger<AccountController> logger)
        {
            _context = context;
            _activeDirectoryValidator = activeDirectoryValidator;
            _jwtTokenService = jwtTokenService;
            _logger = logger;

            _domain = configuration["ActiveDirectory:Domain"]?.Trim()
                ?? throw new InvalidOperationException(
                    "ActiveDirectory:Domain is missing from appsettings.json.");
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
                HttpContext.Session.GetString("UserRole");

            if (!string.IsNullOrWhiteSpace(currentRole))
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            return View(new LoginViewModel());
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
                NormalizeUserName(model.Username);

            /*
             * البحث عن المستخدم داخل جدول Users.
             */
            var localUser =
                await _context.Users
                    .FirstOrDefaultAsync(
                        user =>
                            user.Username == enteredUsername);

            /*
             * حساب Admin المحلي:
             *
             * لا يتم فحصه من Active Directory.
             * يتم استخدام كلمة المرور الموجودة في جدول Users.
             */
            if (localUser != null &&
                string.Equals(
                    localUser.UserRole,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (localUser.Password != model.Password)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "❌ Invalid username or password.");

                    return View(model);
                }

                if (localUser.IsActive == 0)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "⏳ Your account is not active.");

                    return View(model);
                }

                await CreateUserSessionAsync(
                    user: localUser,
                    username: localUser.Username,
                    fullName: localUser.FullName,
                    email: localUser.Email,
                    givenName: localUser.FullName,
                    surname: string.Empty);

                localUser.LastLoginDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            /*
             * باقي المستخدمين:
             * التحقق من اسم المستخدم وكلمة المرور
             * باستخدام Active Directory.
             */
            bool isAuthenticated;

            try
            {
                isAuthenticated =
                    _activeDirectoryValidator.IsAuthenticated(
                        _domain,
                        enteredUsername,
                        model.Password);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Active Directory authentication error for user {UserName}. Domain: {Domain}. Error: {Error}",
                    enteredUsername,
                    _domain,
                    ex.Message);

                ModelState.AddModelError(
                    string.Empty,
                    "❌ The university login service is currently unavailable. Please contact IT support.");

                return View(model);
            }

            if (!isAuthenticated)
            {
                _logger.LogWarning(
                    "Active Directory rejected credentials for user {UserName}.",
                    enteredUsername);

                ModelState.AddModelError(
                    string.Empty,
                    "❌ Invalid university username or password.");

                return View(model);
            }

            /*
             * نجح التحقق من Active Directory.
             *
             * يجب أن يكون المستخدم مسجلًا أيضًا
             * داخل جدول Users في النظام.
             */
            if (localUser == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "❌ Your university account is valid, but you are not registered in this system.");

                return View(model);
            }

            if (localUser.IsActive == 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "⏳ Your account is pending approval from the Manager.");

                return View(model);
            }

            /*
             * جلب الاسم الأول واسم العائلة
             * من Active Directory.
             *
             * فشل جلب الاسم لا يمنع تسجيل الدخول
             * بعد نجاح كلمة المرور.
             */
            string givenName = string.Empty;
            string surname = string.Empty;

            try
            {
                givenName =
                    CleanActiveDirectoryValue(
                        _activeDirectoryValidator.GetGivenName(
                            _domain,
                            enteredUsername,
                            model.Password));

                surname =
                    CleanActiveDirectoryValue(
                        _activeDirectoryValidator.GetLastName(
                            _domain,
                            enteredUsername,
                            model.Password));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Login succeeded, but Active Directory user details could not be retrieved for {UserName}.",
                    enteredUsername);
            }

            string activeDirectoryFullName =
                $"{givenName} {surname}".Trim();

            string fullName;

            if (!string.IsNullOrWhiteSpace(localUser.FullName))
            {
                fullName = localUser.FullName;
            }
            else if (!string.IsNullOrWhiteSpace(activeDirectoryFullName))
            {
                fullName = activeDirectoryFullName;
            }
            else
            {
                fullName = enteredUsername;
            }

            string email =
                localUser.Email ?? string.Empty;

            string tokenGivenName =
                !string.IsNullOrWhiteSpace(givenName)
                    ? givenName
                    : fullName;

            await CreateUserSessionAsync(
                user: localUser,
                username: enteredUsername,
                fullName: fullName,
                email: email,
                givenName: tokenGivenName,
                surname: surname);

            localUser.LastLoginDate = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "User {UserName} logged in successfully.",
                enteredUsername);

            return RedirectToAction(
                "Index",
                "Home");
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
                user.UserID.ToString());

            HttpContext.Session.SetString(
                "Username",
                username ?? string.Empty);

            HttpContext.Session.SetString(
                "FullName",
                fullName ?? string.Empty);

            HttpContext.Session.SetString(
                "UserRole",
                user.UserRole ?? string.Empty);

            HttpContext.Session.SetString(
                "UserEmail",
                email ?? string.Empty);

            /*
             * جلب AppUserID باستخدام:
             *
             * UserLog
             * أو Email
             */
            var appUser =
                await _context.AppUsers
                    .FirstOrDefaultAsync(
                        currentAppUser =>
                            currentAppUser.Status == "Active"
                            &&
                            (
                                currentAppUser.UserLog == username
                                ||
                                (
                                    !string.IsNullOrWhiteSpace(email)
                                    &&
                                    currentAppUser.Email == email
                                )
                            ));

            if (appUser != null)
            {
                HttpContext.Session.SetString(
                    "AppUserID",
                    appUser.Id.ToString());
            }
            else
            {
                HttpContext.Session.Remove("AppUserID");
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
                    user.UserRole ?? string.Empty);

            HttpContext.Session.SetString(
                "AccessToken",
                token);
        }

        // =====================================================
        // REGISTER - GET
        // =====================================================

        [HttpGet]
        public IActionResult Register()
        {
            string? currentRole =
                HttpContext.Session.GetString("UserRole");

            if (!string.IsNullOrWhiteSpace(currentRole))
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            return View(new RegisterViewModel());
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
                NormalizeUserName(model.Username);

            string email =
                model.Email.Trim();

            bool exists =
                await _context.Users
                    .AnyAsync(
                        user =>
                            user.Username == username
                            ||
                            user.Email == email);

            if (exists)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "❌ Username or Email already exists.");

                return View(model);
            }

            /*
             * لا يتم حفظ كلمة مرور Active Directory.
             *
             * نخزن قيمة عشوائية بسبب وجود عمود Password
             * في جدول Users.
             */
            var user =
                new User
                {
                    Username = username,
                    FullName = model.FullName,
                    Email = email,
                    PhoneNumber = model.PhoneNumber,

                    Password =
                        Guid.NewGuid().ToString("N"),

                    UserRole = "Receptionist",
                    IsActive = 0,
                    CreatedDate = DateTime.Now
                };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            TempData["RegisterSuccess"] =
                "✅ Registration successful! "
                + "Use your university password "
                + "after your account is approved.";

            return RedirectToAction("Login");
        }

        // =====================================================
        // LOGOUT
        // =====================================================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Login",
                "Account");
        }

        // =====================================================
        // PENDING USERS
        // =====================================================

        public async Task<IActionResult> PendingUsers()
        {
            string? role =
                HttpContext.Session.GetString("UserRole");

            if (!IsAdminOrManager(role))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var pending =
                await _context.Users
                    .Where(user => user.IsActive == 0)
                    .OrderByDescending(
                        user => user.CreatedDate)
                    .ToListAsync();

            return View(pending);
        }

        // =====================================================
        // APPROVE USER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(
            int id)
        {
            string? role =
                HttpContext.Session.GetString("UserRole");

            if (!IsAdminOrManager(role))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var user =
                await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = 1;
            user.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"✅ {user.FullName} has been approved!";

            return RedirectToAction("PendingUsers");
        }

        // =====================================================
        // REJECT USER
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUser(
            int id)
        {
            string? role =
                HttpContext.Session.GetString("UserRole");

            if (!IsAdminOrManager(role))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var user =
                await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "🗑️ User has been rejected and removed.";

            return RedirectToAction("PendingUsers");
        }

        // =====================================================
        // ACCESS DENIED
        // =====================================================

        public IActionResult AccessDenied()
        {
            return View();
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static bool IsAdminOrManager(
            string? role)
        {
            return
                string.Equals(
                    role,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    role,
                    "Manager",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeUserName(
            string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return string.Empty;
            }

            string value = userName.Trim();

            /*
             * AMMAN\username
             * يصبح:
             * username
             */
            int slashIndex =
                value.LastIndexOf('\\');

            if (slashIndex >= 0 &&
                slashIndex < value.Length - 1)
            {
                value =
                    value[(slashIndex + 1)..];
            }

            /*
             * username@amman.local
             * يصبح:
             * username
             */
            int atIndex =
                value.IndexOf('@');

            if (atIndex > 0)
            {
                value =
                    value[..atIndex];
            }

            return value.Trim();
        }

        private static string CleanActiveDirectoryValue(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string result = value.Trim();

            /*
             * الدوال الموجودة في ActiveDirectoryValidator
             * ترجع Error كنص بدل رمي Exception.
             */
            if (result.StartsWith(
                    "Error:",
                    StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            if (result.StartsWith(
                    "No given name",
                    StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            if (result.StartsWith(
                    "No surname",
                    StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return result;
        }
    }
}