using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Filters;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [AuthFilter("Admin")]
    public class UserNewTypesController : Controller
    {
        private readonly AppDbContext _context;

        public UserNewTypesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> UserTypes()
        {
            var types = await _context.UserTypes.OrderByDescending(x => x.Id).ToListAsync();
            return View(types);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserTypes(UserType model)
        {
            if (!ModelState.IsValid)
                return View(await _context.UserTypes.ToListAsync());

            _context.UserTypes.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("UserTypes");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var type = await _context.UserTypes.FindAsync(id);
            if (type != null)
            {
                _context.UserTypes.Remove(type);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("UserTypes");
        }
    }
}