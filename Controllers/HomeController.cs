using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Filters;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [AuthFilter("Admin", "Fulltime Supervisor", "Parttime Supervisor", "Receptionist", "Student")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserRole") == null)
                return RedirectToAction("Login", "Account");

            var userRole = HttpContext.Session.GetString("UserRole");
            var userEmail = HttpContext.Session.GetString("UserEmail");

            List<Patient> patients;

            if (userRole == "Student")
            {
                var appUser = await _context.AppUsers
                    .FirstOrDefaultAsync(a => a.Email == userEmail);

                if (appUser == null)
                {
                    patients = new List<Patient>();
                }
                else
                {
                    var assignedPatientIds = await _context.AllocatedStudents
                        .Where(a => a.AppUserId == appUser.Id)
                        .Select(a => a.PatientID)
                        .ToListAsync();

                    patients = await _context.Patients
                        .Include(p => p.Status)
                        .Where(p => assignedPatientIds.Contains(p.PatientID)
                                 && p.PatientStatus == "Allocated")
                        .ToListAsync();
                }
            }
            else
            {
                patients = await _context.Patients
                    .Include(p => p.Status)
                    .Where(p => p.StatusID != 6)
                    .ToListAsync();
            }

            var patientIds = patients.Select(p => p.PatientID).ToList();
            var today = DateTime.Today;

            var nextAppts = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => patientIds.Contains(a.PatientID) && a.AppointmentDate.Date >= today)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            var nextApptMap = nextAppts
                .GroupBy(a => a.PatientID)
                .ToDictionary(g => g.Key, g => g.First());

            ViewBag.NextAppointments = nextApptMap;
            ViewBag.TotalPatients = patients.Count;
            ViewBag.AcceptedPatients = patients.Count(p => p.StatusID == 2);
            ViewBag.ScheduledAppointments = await _context.Appointments
                .CountAsync(a => a.AppointmentStatus == "Scheduled" && patientIds.Contains(a.PatientID));
            ViewBag.DischargedPatients = patients.Count(p => p.PatientStatus == "Discharged");

            return View(patients);
        }

        public IActionResult Privacy()
        {
            if (HttpContext.Session.GetString("UserRole") == null)
                return RedirectToAction("Login", "Account");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}