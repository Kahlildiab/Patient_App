using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Filters;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [AuthFilter("Admin")]
    public class AddNewUsersController : Controller
    {
        private readonly AppDbContext _context;

        public AddNewUsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> AddUser()
        {
            ViewBag.UserTypes = await _context.UserTypes.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUser(AppUser model)
        {
            // تجاهل validation للـ UserType object لأنه navigation property
            ModelState.Remove("UserType");
            ModelState.Remove("Status");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                TempData["Errors"] = string.Join(" | ", errors);
                ViewBag.UserTypes = await _context.UserTypes.ToListAsync();
                return View(model);
            }

            model.Status = "Active";
            _context.AppUsers.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ User added successfully!";
            return RedirectToAction("SearchUsers", "SearchNewUsers");
        }
    }
}