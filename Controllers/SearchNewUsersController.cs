using System;
using System.Collections.Generic;
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
        // Roles helper
        // =====================================================
        private async Task<List<string>> GetAvailableRolesAsync(
            string? currentRole = null)
        {
            var standardRoles = new List<string>
            {
                "Admin",
                "Fulltime Supervisor",
                "Parttime Supervisor",
                "Receptionist",
                "Student"
            };

            var databaseRoles = await _context.Users
                .AsNoTracking()
                .Where(user =>
                    user.UserRole != null &&
                    user.UserRole != "")
                .Select(user => user.UserRole)
                .Distinct()
                .ToListAsync();

            var roles = standardRoles
                .Concat(databaseRoles);

            if (!string.IsNullOrWhiteSpace(currentRole))
            {
                roles = roles.Append(currentRole.Trim());
            }

            return roles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(role => role)
                .ToList();
        }

        // =====================================================
        // Search Users
        // This page reads directly from dbo.Users.
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> SearchUsers(
            string? search,
            string? roleFilter,
            int? statusFilter)
        {
            var query = _context.Users
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string searchValue = search.Trim();

                query = query.Where(user =>
                    user.Username.Contains(searchValue) ||
                    user.FullName.Contains(searchValue) ||
                    user.Email.Contains(searchValue) ||
                    user.PhoneNumber.Contains(searchValue) ||
                    user.UserRole.Contains(searchValue));
            }

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                string selectedRole = roleFilter.Trim();

                query = query.Where(user =>
                    user.UserRole == selectedRole);
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(user =>
                    user.IsActive == statusFilter.Value);
            }

            var model = new SearchUserViewModel
            {
                SearchTerm = search,
                RoleFilter = roleFilter,
                StatusFilter = statusFilter,

                Results = await query
                    .OrderByDescending(user => user.UserID)
                    .ToListAsync(),

                Roles = await GetAvailableRolesAsync()
            };

            return View(model);
        }

        // =====================================================
        // Details
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.UserID == id);

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
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            ViewBag.Roles =
                await GetAvailableRolesAsync(
                    user.UserRole
                );

            return View(user);
        }

        // =====================================================
        // Edit - POST
        //
        // Active   => IsActive = 1
        // UnActive => IsActive = 0
        //
        // Password is not changed by this page.
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int UserID,
            string Username,
            string FullName,
            string Email,
            string PhoneNumber,
            string UserRole,
            int IsActive)
        {
            Username =
                (Username ?? string.Empty).Trim();

            FullName =
                (FullName ?? string.Empty).Trim();

            Email =
                (Email ?? string.Empty).Trim();

            PhoneNumber =
                (PhoneNumber ?? string.Empty).Trim();

            UserRole =
                (UserRole ?? string.Empty).Trim();

            if (UserID <= 0)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(Username))
            {
                ModelState.AddModelError(
                    "Username",
                    "Username is required."
                );
            }

            if (string.IsNullOrWhiteSpace(FullName))
            {
                ModelState.AddModelError(
                    "FullName",
                    "Full name is required."
                );
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Email is required."
                );
            }

            if (string.IsNullOrWhiteSpace(UserRole))
            {
                ModelState.AddModelError(
                    "UserRole",
                    "User role is required."
                );
            }

            if (IsActive != 0 && IsActive != 1)
            {
                ModelState.AddModelError(
                    "IsActive",
                    "Please select Active or UnActive."
                );
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(item =>
                    item.UserID == UserID);

            if (user == null)
            {
                return NotFound();
            }

            bool duplicateUsername =
                await _context.Users.AnyAsync(item =>
                    item.UserID != UserID &&
                    item.Username == Username);

            if (duplicateUsername)
            {
                ModelState.AddModelError(
                    "Username",
                    "This username is already used by another account."
                );
            }

            if (!ModelState.IsValid)
            {
                var editModel = new User
                {
                    UserID = UserID,
                    Username = Username,
                    Password = user.Password,
                    FullName = FullName,
                    Email = Email,
                    PhoneNumber = PhoneNumber,
                    UserRole = UserRole,
                    IsActive = IsActive,
                    CreatedDate = user.CreatedDate,
                    LastLoginDate = user.LastLoginDate,
                    ModifiedDate = user.ModifiedDate
                };

                ViewBag.Roles =
                    await GetAvailableRolesAsync(
                        UserRole
                    );

                TempData["Errors"] = string.Join(
                    " | ",
                    ModelState.Values
                        .SelectMany(value => value.Errors)
                        .Select(error => error.ErrorMessage)
                );

                return View(editModel);
            }

            user.Username = Username;
            user.FullName = FullName;
            user.Email = Email;
            user.PhoneNumber = PhoneNumber;
            user.UserRole = UserRole;

            /*
             * This is the actual login status stored in dbo.Users.
             */
            user.IsActive = IsActive;
            user.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                IsActive == 1
                    ? "✅ User updated and activated successfully."
                    : "✅ User updated and deactivated successfully.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = UserID
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
            var user = await _context.Users
                .FirstOrDefaultAsync(item =>
                    item.UserID == id);

            if (user == null)
            {
                return NotFound();
            }

            string currentUsername =
                HttpContext.Session.GetString("Username")
                ?? string.Empty;

            if (
                string.Equals(
                    currentUsername,
                    user.Username,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                TempData["Error"] =
                    "You cannot delete the account currently signed in.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id
                    }
                );
            }

            try
            {
                _context.Users.Remove(user);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "🗑️ User deleted successfully.";

                return RedirectToAction(
                    nameof(SearchUsers)
                );
            }
            catch (DbUpdateException)
            {
                /*
                 * Some users may be linked to competencies or other
                 * records. In this case deactivating is safer.
                 */
                TempData["Error"] =
                    "This user cannot be deleted because related records exist. "
                    + "Change the status to UnActive instead.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id
                    }
                );
            }
        }
    }
}
