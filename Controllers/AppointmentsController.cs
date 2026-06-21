using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Filters;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{

    [AuthFilter("Admin", "Fulltime Supervisor", "Parttime Supervisor", "Receptionist", "Student")]
    public class AppointmentsController : Controller
    {
        private readonly AppDbContext _context;

        public AppointmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Appointments
        public async Task<IActionResult> Index()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            var userEmail = HttpContext.Session.GetString("UserEmail");

            List<Appointment> appointments;

            if (userRole == "Student")
            {
                var appUser = await _context.AppUsers
                    .FirstOrDefaultAsync(a => a.Email == userEmail);

                if (appUser == null)
                {
                    appointments = new List<Appointment>();
                }
                else
                {
                    var assignedPatientIds = await _context.AllocatedStudents
                        .Where(a => a.AppUserId == appUser.Id)
                        .Select(a => a.PatientID)
                        .ToListAsync();

                    appointments = await _context.Appointments
                        .Include(a => a.Patient)
                        .Where(a => assignedPatientIds.Contains(a.PatientID)
                        && a.Patient.PatientStatus == "Allocated")
                        .ToListAsync();
                }
            }
            else
            {
                appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.Patient.StatusID != 6)
                    .ToListAsync();
            }

            return View(appointments);
        }

        // GET: Appointments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.AppointmentID == id);

            if (appointment == null) return NotFound();
            return View(appointment);
        }

        // GET: Appointments/Create
        public IActionResult Create()
        {
            ViewData["PatientID"] = new SelectList(_context.Patients, "PatientID", "FirstName");
            return View();
        }

        // POST: Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AppointmentID,PatientID,ClinicName,AppointmentDate,AppointmentDay,TimeFrom,TimeTo,AppointmentStatus,CreatedDate")] Appointment appointment)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToList();

                foreach (var error in errors)
                {
                    TempData["Error"] += $"❌ {error.Key}: {string.Join(", ", error.Errors.Select(e => e.ErrorMessage))} | ";
                }

                return RedirectToAction("Index", "Patients");
            }

            appointment.CreatedDate = DateTime.Now;
            _context.Add(appointment);
            await _context.SaveChangesAsync();
            TempData["Success"] = "✅ Appointment booked successfully!";
            return RedirectToAction("Index", "Appointments");
        }

        // GET: Appointments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AppointmentID == id);

            if (appointment == null) return NotFound();
            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Appointment appointment)
        {
            if (id != appointment.AppointmentID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingAppointment = await _context.Appointments.FindAsync(id);
                    if (existingAppointment == null) return NotFound();

                    existingAppointment.ClinicName = appointment.ClinicName;
                    existingAppointment.AppointmentDate = appointment.AppointmentDate;
                    existingAppointment.AppointmentDay = appointment.AppointmentDay;
                    existingAppointment.AppointmentStatus = appointment.AppointmentStatus;

                    var patient = await _context.Patients.FindAsync(appointment.PatientID);
                    if (patient != null)
                    {
                        patient.AppointmentDate = appointment.AppointmentDate;
                        patient.AppointmentTime = appointment.TimeFrom.Hours < 12 ? "AM" : "PM";
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.AppointmentID)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(appointment);
        }

        // GET: Appointments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.AppointmentID == id);

            if (appointment == null) return NotFound();
            return View(appointment);
        }

        // POST: Appointments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.AppointmentID == id);
        }

        // GET: Appointments/SearchPatient?q=...
        [HttpGet]
        public async Task<IActionResult> SearchPatient(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(new List<object>());

            var userRole = HttpContext.Session.GetString("UserRole");
            var userEmail = HttpContext.Session.GetString("UserEmail");

            q = q.ToLower().Trim();

            IQueryable<Patient> query = _context.Patients.Where(p => p.StatusID != 6);

            if (userRole == "Student")
            {
                var appUser = await _context.AppUsers
                    .FirstOrDefaultAsync(a => a.Email == userEmail);

                if (appUser != null)
                {
                    var assignedPatientIds = await _context.AllocatedStudents
                        .Where(a => a.AppUserId == appUser.Id)
                        .Select(a => a.PatientID)
                        .ToListAsync();

                    query = query.Where(p => assignedPatientIds.Contains(p.PatientID)
                                          && p.PatientStatus == "Allocated");
                }
                else
                {
                    return Json(new List<object>());
                }
            }

            var patients = await query
                .Where(p =>
                    (p.FirstName + " " + p.SecondName + " " + p.ThirdName + " " + p.FourthName)
                        .ToLower().Contains(q) ||
                    (p.NationalID_PassportNumber != null &&
                     p.NationalID_PassportNumber.ToLower().Contains(q)))
                .Select(p => new
                {
                    p.PatientID,
                    FullName = p.FirstName + " " + p.SecondName + " " + p.ThirdName + " " + p.FourthName,
                    p.NationalID_PassportNumber
                })
                .Take(10)
                .ToListAsync();

            return Json(patients);
        }

        // GET: Appointments/GetPatientAttendance?patientId=...
        [HttpGet]
        public async Task<IActionResult> GetPatientAttendance(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
                return Json(new { canBook = false, message = "Patient not found." });

            var hasAppointments = await _context.Appointments
                .AnyAsync(a => a.PatientID == patientId);

            if (hasAppointments && string.IsNullOrEmpty(patient.AttendanceStatus))
            {
                return Json(new
                {
                    canBook = false,
                    message = "This patient already has a previous appointment with no attendance record. " +
                              "Please open their Details page and mark their attendance (Attended or No-Show) before booking a new appointment."
                });
            }

            return Json(new { canBook = true, message = "" });
        }

        // ✅ POST: Appointments/Save
        // ✅ يحفظ AppointmentTime بصيغة "AM|09:00" أو "PM|13:00"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int PatientID, DateTime AppointmentDate, string AppointmentPeriod, string AppointmentTime)
        {
            if (PatientID == 0)
            {
                TempData["Error"] = "Please select a patient.";
                return RedirectToAction("Index");
            }

            var patient = await _context.Patients.FindAsync(PatientID);
            if (patient == null)
            {
                TempData["Error"] = "Patient not found.";
                return RedirectToAction("Index");
            }

            var hasExistingAppointments = await _context.Appointments
                .AnyAsync(a => a.PatientID == PatientID);

            if (hasExistingAppointments && string.IsNullOrEmpty(patient.AttendanceStatus))
            {
                TempData["Error"] = "⚠️ Cannot book a new appointment. " +
                                    "This patient has a previous appointment with no attendance record. " +
                                    "Please mark their attendance from the Details page first.";
                return RedirectToAction("Index");
            }

            // ✅ احفظ الفترة + الوقت معاً بصيغة "AM|09:00"
            patient.AppointmentDate = AppointmentDate;
            patient.AppointmentTime = !string.IsNullOrEmpty(AppointmentTime)
                                        ? $"{AppointmentPeriod}|{AppointmentTime}"
                                        : AppointmentPeriod;
            patient.AttendanceStatus = null;
            _context.Update(patient);

            // ✅ تحديد TimeFrom / TimeTo بناءً على الوقت الفعلي
            TimeSpan timeFrom;
            TimeSpan timeTo;

            if (!string.IsNullOrEmpty(AppointmentTime) && TimeSpan.TryParse(AppointmentTime, out TimeSpan parsedTime))
            {
                timeFrom = parsedTime;
                timeTo = parsedTime.Add(TimeSpan.FromHours(2));
            }
            else
            {
                timeFrom = AppointmentPeriod == "AM" ? new TimeSpan(8, 0, 0) : new TimeSpan(13, 0, 0);
                timeTo = AppointmentPeriod == "AM" ? new TimeSpan(12, 0, 0) : new TimeSpan(17, 0, 0);
            }

            var appointment = new Appointment
            {
                PatientID = PatientID,
                AppointmentDate = AppointmentDate,
                AppointmentDay = AppointmentDate.DayOfWeek.ToString(),
                ClinicName = "General",
                TimeFrom = timeFrom,
                TimeTo = timeTo,
                AppointmentStatus = "Scheduled",
                CreatedDate = DateTime.Now,
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"✅ Appointment saved for {patient.FirstName} {patient.FourthName} on {AppointmentDate:yyyy-MM-dd} ({AppointmentPeriod} - {AppointmentTime}).";
            return RedirectToAction("Index");
        }

        // POST: Appointments/MarkAttendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAttendance(int patientId, string status)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            patient.AttendanceStatus = status;

            if (status == "NoShow")
            {
                var appointment = await _context.Appointments
                    .Where(a => a.PatientID == patientId && a.AppointmentStatus == "Scheduled")
                    .OrderByDescending(a => a.AppointmentDate)
                    .FirstOrDefaultAsync();

                if (appointment != null)
                    appointment.AppointmentStatus = "Cancelled";
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminApprove(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.AppointmentStatus = "Approved";
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ Appointment approved!";
            }
            return RedirectToAction("Index", "AdminApprovals");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminReject(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "🗑️ Appointment rejected.";
            }
            return RedirectToAction("Index", "AdminApprovals");
        }
    }
}
