using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [DentalCollegeManagementSystem_AAU.Filters.AuthFilter]
    public class CompetenciesController : Controller
    {
        private readonly AppDbContext _context;

        public CompetenciesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int patientId, int competencyId)
        {
            // ✅ Student ما يقدر يعمل Toggle
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole == "Student")
                return Json(new { success = false, message = "Students cannot modify competencies." });

            var record = await _context.Competency
                .FirstOrDefaultAsync(c => c.PatientID == patientId &&
                                         (c.CompetencyID == competencyId ||
                                          c.ItemName == _context.Competency
                                              .Where(x => x.CompetencyID == competencyId)
                                              .Select(x => x.ItemName)
                                              .FirstOrDefault()));

            bool nowCompleted;
            DateTime? completedDate = null;

            if (record == null)
            {
                var template = await _context.Competency
                    .FirstOrDefaultAsync(c => c.CompetencyID == competencyId);

                if (template == null)
                    return Json(new { success = false, message = "Template not found" });

                var newRecord = new Competency
                {
                    CategoryName = template.CategoryName,
                    CategoryOrder = template.CategoryOrder,
                    ItemName = template.ItemName,
                    ItemOrder = template.ItemOrder,
                    PatientID = patientId,
                    IsCompleted = true,
                    CompletedDate = DateTime.Now
                };

                _context.Competency.Add(newRecord);
                nowCompleted = true;
                completedDate = newRecord.CompletedDate;
            }
            else
            {
                record.IsCompleted = !record.IsCompleted;
                record.CompletedDate = record.IsCompleted ? DateTime.Now : null;
                nowCompleted = record.IsCompleted;
                completedDate = record.CompletedDate;
            }

            await _context.SaveChangesAsync();

            int totalItemsCount = await _context.Competency
                .CountAsync(c => c.PatientID == null);

            int completedCount = await _context.Competency
                .CountAsync(c => c.PatientID == patientId && c.IsCompleted);

            int percentage = totalItemsCount > 0
                ? (int)Math.Round((double)completedCount / totalItemsCount * 100)
                : 0;

            return Json(new
            {
                success = true,
                isCompleted = nowCompleted,
                completedDate = completedDate?.ToString("yyyy-MM-dd"),
                totalCompleted = completedCount,
                totalItems = totalItemsCount,
                percentage = percentage
            });
        }
    }
}