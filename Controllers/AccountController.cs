using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;
using DentalCollegeManagementSystem_AAU.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserRole") != null)
                return RedirectToAction("Index", "Home");
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username
                                       && u.Password == model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "❌ Invalid username or password");
                return View(model);
            }

            if (user.IsActive == 0)
            {
                ModelState.AddModelError("", "⏳ Your account is pending approval from the Manager.");
                return View(model);
            }

            HttpContext.Session.SetString("UserID", user.UserID.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("UserRole", user.UserRole);
            HttpContext.Session.SetString("UserEmail", user.Email);

            // ✅ اسحب AppUserID وخزنه بالـ Session
            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(a => a.Email == user.Email);

            if (appUser != null)
                HttpContext.Session.SetString("AppUserID", appUser.Id.ToString());

            user.LastLoginDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("UserRole") != null)
                return RedirectToAction("Index", "Home");
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var exists = await _context.Users
                .AnyAsync(u => u.Username == model.Username || u.Email == model.Email);

            if (exists)
            {
                ModelState.AddModelError("", "❌ Username or Email already exists");
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Password = model.Password,
                UserRole = "Receptionist",
                IsActive = 0,
                CreatedDate = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["RegisterSuccess"] = "✅ Registration successful! Please wait for Manager approval.";
            return RedirectToAction("Login");
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        // GET: /Account/PendingUsers
        public async Task<IActionResult> PendingUsers()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin" && role != "Manager")
                return RedirectToAction("Login");

            var pending = await _context.Users
                .Where(u => u.IsActive == 0)
                .ToListAsync();

            return View(pending);
        }

        // POST: /Account/ApproveUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin" && role != "Manager")
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = 1;
            user.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"✅ {user.FullName} has been approved!";
            return RedirectToAction("PendingUsers");
        }

        // POST: /Account/RejectUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUser(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin" && role != "Manager")
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "🗑️ User has been rejected and removed.";
            return RedirectToAction("PendingUsers");
        }
    }
}