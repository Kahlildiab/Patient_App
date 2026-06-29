using System;
using System.Threading.Tasks;
using DentalCollegeManagementSystem_AAU.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    /// <summary>
    /// Opens the Dental Chart page.
    /// Example:
    /// /DentalChartPage/Index?patientId=5
    /// </summary>
    [DentalCollegeManagementSystem_AAU.Filters.AuthFilter]
    public class DentalChartPageController : Controller
    {
        private readonly AppDbContext _context;

        public DentalChartPageController(
            AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int patientId)
        {
            if (patientId <= 0)
            {
                return BadRequest(
                    "patientId مطلوب وأكبر من صفر."
                );
            }

            bool patientExists = await _context.Patients
                .AnyAsync(
                    p => p.PatientID == patientId
                );

            if (!patientExists)
            {
                return NotFound(
                    "Patient was not found."
                );
            }

            var latestVisit = await _context.Visits
                .Where(
                    v => v.PatientID == patientId
                )
                .OrderByDescending(
                    v => v.VisitDate
                )
                .ThenByDescending(
                    v => v.VisitID
                )
                .FirstOrDefaultAsync();

            bool isAdmin =
                string.Equals(
                    HttpContext.Session
                        .GetString("UserRole"),
                    "Admin",
                    StringComparison.OrdinalIgnoreCase
                );

            bool isVisitClosed =
                latestVisit != null &&
                latestVisit.IsClosed;

            ViewBag.PatientId = patientId;
            ViewBag.IsVisitClosed =
                isVisitClosed;

            ViewBag.IsAdmin =
                isAdmin;

            ViewBag.IsReadOnly =
                isVisitClosed &&
                !isAdmin;

            return View(
                "~/Views/DentalCharting/Index.cshtml"
            );
        }
    }
}