using System;
using System.Linq;
using System.Threading.Tasks;
using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Filters;
using DentalCollegeManagementSystem_AAU.Models;
using DentalCollegeManagementSystem_AAU.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [AuthFilter("Admin")]
    public class SearchNewUsersController : Controller
    {
        private readonly AppDbContext _context;

        public SearchNewUsersController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // Search Users
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string? search)
        {
            var model = new SearchUserViewModel
            {
                SearchTerm = search
            };

            var query = _context.AppUsers
                .Include(user => user.UserType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string searchValue = search.Trim();

                query = query.Where(user =>
                    user.NameEn.Contains(searchValue) ||
                    user.NameAr.Contains(searchValue) ||
                    user.Email.Contains(searchValue) ||
                    user.UserLog.Contains(searchValue));
            }

            model.Results = await query
                .OrderByDescending(user => user.Id)
                .ToListAsync();

            return View(model);
        }

        // =====================================================
        // Details
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.AppUsers
                .Include(item => item.UserType)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // =====================================================
        // Edit - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(item => item.Id == id);

            if (appUser == null)
            {
                return NotFound();
            }

            /*
             * نحاول قراءة الحالة الحقيقية من جدول Users
             * لأن تسجيل الدخول يعتمد على Users.IsActive.
             */
            var loginUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user =>
                    user.Username == appUser.UserLog);

            if (loginUser != null)
            {
                appUser.Status =
                    loginUser.IsActive == 1
                        ? "Active"
                        : "UnActive";
            }

            ViewBag.UserTypes = await _context.UserTypes
                .OrderBy(type => type.NameEn)
                .ToListAsync();

            return View(appUser);
        }

        // =====================================================
        // Edit - POST
        //
        // Active   => Users.IsActive = 1
        // UnActive => Users.IsActive = 0
        //
        // AppUsers.Status is also updated to keep both tables
        // synchronized.
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AppUser model)
        {
            ModelState.Remove("UserType");

            string normalizedStatus =
                (model.Status ?? string.Empty).Trim();

            bool isActiveStatus =
                normalizedStatus.Equals(
                    "Active",
                    StringComparison.OrdinalIgnoreCase
                );

            bool isUnActiveStatus =
                normalizedStatus.Equals(
                    "UnActive",
                    StringComparison.OrdinalIgnoreCase
                );

            if (!isActiveStatus && !isUnActiveStatus)
            {
                ModelState.AddModelError(
                    "Status",
                    "Please select Active or UnActive."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.UserTypes = await _context.UserTypes
                    .OrderBy(type => type.NameEn)
                    .ToListAsync();

                TempData["Errors"] = string.Join(
                    " | ",
                    ModelState.Values
                        .SelectMany(value => value.Errors)
                        .Select(error => error.ErrorMessage)
                );

                return View(model);
            }

            var appUser = await _context.AppUsers
                .FirstOrDefaultAsync(item => item.Id == model.Id);

            if (appUser == null)
            {
                return NotFound();
            }

            string oldUserLog =
                (appUser.UserLog ?? string.Empty).Trim();

            string newUserLog =
                (model.UserLog ?? string.Empty).Trim();

            /*
             * منع تكرار UserLog داخل AppUsers.
             */
            bool duplicateAppUser =
                await _context.AppUsers.AnyAsync(item =>
                    item.Id != model.Id &&
                    item.UserLog == newUserLog);

            if (duplicateAppUser)
            {
                ModelState.AddModelError(
                    "UserLog",
                    "This User Log is already used by another user."
                );

                ViewBag.UserTypes = await _context.UserTypes
                    .OrderBy(type => type.NameEn)
                    .ToListAsync();

                TempData["Errors"] =
                    "This User Log is already used by another user.";

                return View(model);
            }

            /*
             * نبحث عن حساب الدخول أولاً بالرقم القديم،
             * ثم بالرقم الجديد إذا لم يتم العثور عليه.
             */
            var loginUser = await _context.Users
                .FirstOrDefaultAsync(user =>
                    user.Username == oldUserLog);

            if (loginUser == null)
            {
                loginUser = await _context.Users
                    .FirstOrDefaultAsync(user =>
                        user.Username == newUserLog);
            }

            if (loginUser == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The matching login account was not found in the Users table."
                );

                ViewBag.UserTypes = await _context.UserTypes
                    .OrderBy(type => type.NameEn)
                    .ToListAsync();

                TempData["Errors"] =
                    "The matching login account was not found in the Users table.";

                return View(model);
            }

            /*
             * إذا تغير اسم المستخدم، نتأكد أنه غير مستخدم
             * في جدول Users.
             */
            bool duplicateLoginUser =
                await _context.Users.AnyAsync(user =>
                    user.UserID != loginUser.UserID &&
                    user.Username == newUserLog);

            if (duplicateLoginUser)
            {
                ModelState.AddModelError(
                    "UserLog",
                    "This username is already used in the Users table."
                );

                ViewBag.UserTypes = await _context.UserTypes
                    .OrderBy(type => type.NameEn)
                    .ToListAsync();

                TempData["Errors"] =
                    "This username is already used in the Users table.";

                return View(model);
            }

            int newIsActiveValue =
                isActiveStatus
                    ? 1
                    : 0;

            /*
             * تحديث AppUsers.
             */
            appUser.UserLog = newUserLog;
            appUser.Email = (model.Email ?? string.Empty).Trim();
            appUser.NameEn = (model.NameEn ?? string.Empty).Trim();
            appUser.NameAr = (model.NameAr ?? string.Empty).Trim();
            appUser.Mobile = (model.Mobile ?? string.Empty).Trim();
            appUser.UserTypeId = model.UserTypeId;
            appUser.Notes = model.Notes?.Trim();

            appUser.Status =
                newIsActiveValue == 1
                    ? "Active"
                    : "UnActive";

            /*
             * تحديث Users.
             * هذا هو الجدول الذي يعتمد عليه تسجيل الدخول.
             */
            loginUser.Username = newUserLog;
            loginUser.FullName = appUser.NameEn;
            loginUser.Email = appUser.Email;
            loginUser.PhoneNumber = appUser.Mobile;

            loginUser.IsActive =
                newIsActiveValue;

            loginUser.ModifiedDate =
                DateTime.Now;

            /*
             * SaveChanges واحدة تحفظ تعديل الجدولين معاً.
             */
            await _context.SaveChangesAsync();

            TempData["Success"] =
                newIsActiveValue == 1
                    ? "✅ User updated and activated successfully."
                    : "✅ User updated and deactivated successfully.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = model.Id
                }
            );
        }

        // =====================================================
        // Delete
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(item => item.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            _context.AppUsers.Remove(user);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "🗑️ User deleted successfully!";

            return RedirectToAction(
                nameof(SearchUsers)
            );
        }
    }
}
