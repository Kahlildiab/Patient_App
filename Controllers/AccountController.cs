using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;
using DentalCollegeManagementSystem_AAU.Models.ViewModels;
using DentalCollegeManagementSystem_AAU.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.DirectoryServices.AccountManagement;
using System.Text;

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
            //bool isAuthenticated;

            //try
            //{
            //    isAuthenticated =
            //        _activeDirectoryValidator.IsAuthenticated(
            //            _domain,
            //            enteredUsername,
            //            model.Password);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(
            //        ex,
            //        "Active Directory authentication error for user {UserName}. Domain: {Domain}. Error: {Error}",
            //        enteredUsername,
            //        _domain,
            //        ex.Message);

            //    ModelState.AddModelError(
            //        string.Empty,
            //        "❌ The university login service is currently unavailable. Please contact IT support.");

            //    return View(model);
            //}
            // =====================================================
            // ACTIVE DIRECTORY AUTHENTICATION
            // =====================================================

            bool isAuthenticated;

            try
            {
                _logger.LogInformation(
                    @"Starting Active Directory authentication.

UserName: {UserName}
Domain: {Domain}
ConfiguredPath: {ConfiguredPath}
ApplicationServer: {ApplicationServer}
ProcessUser: {ProcessUser}",
                    enteredUsername,
                    _domain,
                    _activeDirectoryValidator.LdapPath,
                    Environment.MachineName,
                    Environment.UserDomainName
                        + "\\"
                        + Environment.UserName);

                isAuthenticated =
                    _activeDirectoryValidator.IsAuthenticated(
                        _domain,
                        enteredUsername,
                        model.Password);
            }
            catch (PrincipalServerDownException ex)
            {
                string completeError =
                    BuildActiveDirectoryErrorMessage(
                        title:
                            "The Active Directory server could not be contacted.",
                        exception: ex,
                        enteredUsername: enteredUsername);

                _logger.LogError(
                    ex,
                    "Active Directory server is unavailable for user {UserName}.",
                    enteredUsername);

                /*
                 * تظهر مباشرة في صفحة Login.
                 */
                ViewBag.ActiveDirectoryError =
                    completeError;

                ModelState.AddModelError(
                    string.Empty,
                    completeError);

                return View(model);
            }
            catch (PrincipalOperationException ex)
            {
                string completeError =
                    BuildActiveDirectoryErrorMessage(
                        title:
                            "Active Directory returned an operation error.",
                        exception: ex,
                        enteredUsername: enteredUsername);

                _logger.LogError(
                    ex,
                    "Active Directory operation error for user {UserName}.",
                    enteredUsername);

                ViewBag.ActiveDirectoryError =
                    completeError;

                ModelState.AddModelError(
                    string.Empty,
                    completeError);

                return View(model);
            }
            catch (UnauthorizedAccessException ex)
            {
                string completeError =
                    BuildActiveDirectoryErrorMessage(
                        title:
                            "The IIS application account was denied access to Active Directory.",
                        exception: ex,
                        enteredUsername: enteredUsername);

                _logger.LogError(
                    ex,
                    "Active Directory access denied for user {UserName}.",
                    enteredUsername);

                ViewBag.ActiveDirectoryError =
                    completeError;

                ModelState.AddModelError(
                    string.Empty,
                    completeError);

                return View(model);
            }
            catch (ArgumentException ex)
            {
                string completeError =
                    BuildActiveDirectoryErrorMessage(
                        title:
                            "The Active Directory configuration is invalid.",
                        exception: ex,
                        enteredUsername: enteredUsername);

                _logger.LogError(
                    ex,
                    "Invalid Active Directory configuration.");

                ViewBag.ActiveDirectoryError =
                    completeError;

                ModelState.AddModelError(
                    string.Empty,
                    completeError);

                return View(model);
            }
            catch (Exception ex)
            {
                string completeError =
                    BuildActiveDirectoryErrorMessage(
                        title:
                            "An unexpected Active Directory error occurred.",
                        exception: ex,
                        enteredUsername: enteredUsername);

                _logger.LogError(
                    ex,
                    "Unexpected Active Directory error for user {UserName}.",
                    enteredUsername);

                ViewBag.ActiveDirectoryError =
                    completeError;

                ModelState.AddModelError(
                    string.Empty,
                    completeError);

                return View(model);
            }

            /*
             * مهم:
             *
             * ValidateCredentials قد لا يرمي Exception.
             * قد يرجع false فقط.
             *
             * لذلك يجب عرض رسالة تشخيصية هنا أيضًا.
             */
            if (!isAuthenticated)
            {
                string processAccount =
                    Environment.UserDomainName
                    + "\\"
                    + Environment.UserName;

                string rejectedMessage =
                    "ACTIVE DIRECTORY LOGIN FAILED"
                    + Environment.NewLine
                    + "----------------------------------------"
                    + Environment.NewLine
                    + "Active Directory returned FALSE."
                    + Environment.NewLine
                    + Environment.NewLine
                    + "This means one of the following:"
                    + Environment.NewLine
                    + "1. The username or password is incorrect."
                    + Environment.NewLine
                    + "2. The account is locked or disabled."
                    + Environment.NewLine
                    + "3. The account has logon restrictions."
                    + Environment.NewLine
                    + "4. The server contacted a different domain controller."
                    + Environment.NewLine
                    + Environment.NewLine
                    + "Username: "
                    + enteredUsername
                    + Environment.NewLine
                    + "Domain: "
                    + _domain
                    + Environment.NewLine
                    + "Configured path/server: "
                    + _activeDirectoryValidator.LdapPath
                    + Environment.NewLine
                    + "Application server: "
                    + Environment.MachineName
                    + Environment.NewLine
                    + "IIS process account: "
                    + processAccount
                    + Environment.NewLine
                    + "Exception: No exception was thrown.";

                _logger.LogWarning(
                    @"Active Directory returned false.

UserName: {UserName}
Domain: {Domain}
ConfiguredPath: {ConfiguredPath}
ApplicationServer: {ApplicationServer}
ProcessAccount: {ProcessAccount}",
                    enteredUsername,
                    _domain,
                    _activeDirectoryValidator.LdapPath,
                    Environment.MachineName,
                    processAccount);

                ViewBag.ActiveDirectoryError =
                    rejectedMessage;

                ModelState.AddModelError(
                    string.Empty,
                    rejectedMessage);

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
        // ACTIVE DIRECTORY ERROR DESCRIPTION
        // =====================================================

        private static string GetActiveDirectoryProblemDescription(
            Exception exception)
        {
            if (ContainsException<PrincipalServerDownException>(
                    exception))
            {
                return
                    "The application server cannot contact the "
                    + "Active Directory domain controller. "
                    + "Check DNS, network, firewall, domain name "
                    + "and LDAP/Kerberos ports.";
            }

            if (ContainsException<UnauthorizedAccessException>(
                    exception))
            {
                return
                    "The IIS application process does not have "
                    + "permission to access Active Directory.";
            }

            if (ContainsException<ArgumentException>(
                    exception))
            {
                return
                    "The Active Directory domain or server "
                    + "configuration is invalid or empty.";
            }

            if (ContainsException<PrincipalOperationException>(
                    exception))
            {
                return
                    "Active Directory returned an operation error. "
                    + "Check the domain controller, account status "
                    + "and directory service availability.";
            }

            string allMessages =
                GetExceptionDetails(exception);

            if (allMessages.Contains(
                    "The server is not operational",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    "The Active Directory server is not operational "
                    + "or cannot be reached from the application server.";
            }

            if (allMessages.Contains(
                    "The specified domain either does not exist",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    "The domain could not be found. Check the server "
                    + "DNS settings and Active Directory domain name.";
            }

            if (allMessages.Contains(
                    "could not be contacted",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    "The Active Directory domain controller could not "
                    + "be contacted from the application server.";
            }

            if (allMessages.Contains(
                    "Logon failure",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    "Active Directory rejected the account used "
                    + "to establish the directory connection.";
            }

            if (allMessages.Contains(
                    "Access is denied",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    "Access to Active Directory was denied for the "
                    + "IIS application process.";
            }

            return
                "An unexpected Active Directory communication "
                + "error occurred.";
        }

        // =====================================================
        // GET FULL EXCEPTION CHAIN
        // =====================================================

        private static string GetExceptionDetails(
            Exception exception)
        {
            StringBuilder result =
                new StringBuilder();

            Exception? currentException =
                exception;

            int level = 0;

            while (currentException != null)
            {
                if (level > 0)
                {
                    result.Append(" --> Inner Exception: ");
                }

                result.Append(
                    currentException.GetType().FullName);

                result.Append(": ");

                result.Append(
                    currentException.Message);

                result.Append(
                    " [HResult: 0x");

                result.Append(
                    currentException.HResult.ToString("X8"));

                result.Append(']');

                currentException =
                    currentException.InnerException;

                level++;
            }

            return result.ToString();
        }

        // =====================================================
        // SEARCH EXCEPTION CHAIN
        // =====================================================

        private static bool ContainsException<TException>(
            Exception exception)
            where TException : Exception
        {
            Exception? currentException =
                exception;

            while (currentException != null)
            {
                if (currentException is TException)
                {
                    return true;
                }

                currentException =
                    currentException.InnerException;
            }

            return false;
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
        // =====================================================
        // BUILD ACTIVE DIRECTORY ERROR MESSAGE
        // =====================================================

        private string BuildActiveDirectoryErrorMessage(
            string title,
            Exception exception,
            string enteredUsername)
        {
            StringBuilder exceptionMessages =
                new StringBuilder();

            Exception? currentException =
                exception;

            int exceptionLevel = 1;

            while (currentException != null)
            {
                exceptionMessages.AppendLine(
                    "Exception Level "
                    + exceptionLevel
                    + ":");

                exceptionMessages.AppendLine(
                    "Type: "
                    + currentException.GetType().FullName);

                exceptionMessages.AppendLine(
                    "Message: "
                    + currentException.Message);

                exceptionMessages.AppendLine(
                    "HResult: 0x"
                    + currentException.HResult.ToString("X8"));

                if (currentException.InnerException != null)
                {
                    exceptionMessages.AppendLine(
                        "----------------------------------------");
                }

                currentException =
                    currentException.InnerException;

                exceptionLevel++;
            }

            string processAccount =
                Environment.UserDomainName
                + "\\"
                + Environment.UserName;

            string suggestedSolution =
                GetActiveDirectorySuggestedSolution(
                    exception);

            return
                "ACTIVE DIRECTORY ERROR"
                + Environment.NewLine
                + "========================================"
                + Environment.NewLine
                + title
                + Environment.NewLine
                + Environment.NewLine
                + "User: "
                + enteredUsername
                + Environment.NewLine
                + "Domain: "
                + _domain
                + Environment.NewLine
                + "Configured path/server: "
                + _activeDirectoryValidator.LdapPath
                + Environment.NewLine
                + "Application server: "
                + Environment.MachineName
                + Environment.NewLine
                + "IIS process account: "
                + processAccount
                + Environment.NewLine
                + Environment.NewLine
                + "EXCEPTION DETAILS"
                + Environment.NewLine
                + "----------------------------------------"
                + Environment.NewLine
                + exceptionMessages
                + Environment.NewLine
                + "SUGGESTED SOLUTION"
                + Environment.NewLine
                + "----------------------------------------"
                + Environment.NewLine
                + suggestedSolution;
        }

        // =====================================================
        // ACTIVE DIRECTORY SUGGESTED SOLUTION
        // =====================================================

        private static string GetActiveDirectorySuggestedSolution(
            Exception exception)
        {
            string fullError =
                exception.ToString();

            if (exception is PrincipalServerDownException ||
                fullError.Contains(
                    "server is not operational",
                    StringComparison.OrdinalIgnoreCase) ||
                fullError.Contains(
                    "could not be contacted",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    "The IIS server cannot reach Active Directory."
                    + Environment.NewLine
                    + "Check the following:"
                    + Environment.NewLine
                    + "1. The IIS server DNS must point to the university Active Directory DNS."
                    + Environment.NewLine
                    + "2. Run: nslookup AMMAN.LOCAL"
                    + Environment.NewLine
                    + "3. Run: nltest /dsgetdc:AMMAN.LOCAL /force"
                    + Environment.NewLine
                    + "4. Test ports 53, 88, 389, 445 and 135."
                    + Environment.NewLine
                    + "5. Confirm the server is connected to the university network or VPN.";
            }

            if (exception is UnauthorizedAccessException ||
                fullError.Contains(
                    "access is denied",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    "The IIS Application Pool account does not have permission."
                    + Environment.NewLine
                    + "Test the Application Pool using an approved domain service account."
                    + Environment.NewLine
                    + "Do not use a personal or Domain Administrator account.";
            }

            if (exception is ArgumentException)
            {
                return
                    "Check the Active Directory settings in appsettings.json "
                    + "and appsettings.Production.json."
                    + Environment.NewLine
                    + "The domain must be similar to: AMMAN.LOCAL"
                    + Environment.NewLine
                    + "Do not pass LDAP://AMMAN.LOCAL as the domain name.";
            }

            if (fullError.Contains(
                "specified domain either does not exist",
                StringComparison.OrdinalIgnoreCase))
            {
                return
                    "The server cannot discover the domain."
                    + Environment.NewLine
                    + "Correct the DNS configuration on the IIS server "
                    + "and verify the domain name.";
            }

            if (fullError.Contains(
                "logon failure",
                StringComparison.OrdinalIgnoreCase))
            {
                return
                    "Active Directory rejected the account."
                    + Environment.NewLine
                    + "Check the username, password, lock status, disabled status "
                    + "and logon restrictions.";
            }

            return
                "Review the exception details and the application logs. "
                + "Also verify DNS, firewall, domain configuration and IIS identity.";
        }
    }

}