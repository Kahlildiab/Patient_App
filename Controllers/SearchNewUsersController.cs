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

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string search)
        {
            var model = new SearchUserViewModel { SearchTerm = search };

            var query = _context.AppUsers
                .Include(u => u.UserType)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(u =>
                    u.NameEn.Contains(search) ||
                    u.Email.Contains(search) ||
                    u.UserLog.Contains(search));

            model.Results = await query.OrderByDescending(u => u.Id).ToListAsync();
            return View(model);
        }

        // ✅ Details
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.AppUsers
                .Include(u => u.UserType)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();
            return View(user);
        }

        // ✅ Edit GET
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null) return NotFound();

            ViewBag.UserTypes = await _context.UserTypes.ToListAsync();
            return View(user);
        }

        // ✅ Edit POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AppUser model)
        {
            ModelState.Remove("UserType");
            ModelState.Remove("Status");

            if (!ModelState.IsValid)
            {
                ViewBag.UserTypes = await _context.UserTypes.ToListAsync();
                return View(model);
            }

            var user = await _context.AppUsers.FindAsync(model.Id);
            if (user == null) return NotFound();

            user.UserLog = model.UserLog;
            user.Email = model.Email;
            user.NameEn = model.NameEn;
            user.NameAr = model.NameAr;
            user.Mobile = model.Mobile;
            user.UserTypeId = model.UserTypeId;
            user.Notes = model.Notes;

            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ User updated successfully!";
            return RedirectToAction("Details", new { id = model.Id });
        }

        // ✅ Delete POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null) return NotFound();

            _context.AppUsers.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "🗑️ User deleted successfully!";
            return RedirectToAction("SearchUsers");
        }
    }
}