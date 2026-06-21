using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [DentalCollegeManagementSystem_AAU.Filters.AuthFilter]
    public class VisitsController : Controller
    {
        private readonly AppDbContext _context;

        public VisitsController(AppDbContext context)
        {
            _context = context;
        }

        // POST: Visits/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Visit visit, string? AppointmentPeriod)
        {
            visit.AppointmentPeriod = AppointmentPeriod;
            visit.CreatedDate = DateTime.Now;

            ModelState.Remove("Patient");
            ModelState.Remove("ChiefComplaint");
            ModelState.Remove("ProceduresPerformed");
            ModelState.Remove("MaterialsUsed");
            ModelState.Remove("Complications");
            ModelState.Remove("StudentNotes");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill in all required fields.";
                return RedirectToAction("Details", "Patients", new { id = visit.PatientID, tab = "todays-visit" });
            }

            visit.Attended = false;

            // ✅ لو Student — تضاف كـ Pending للـ Admin
            var userRole = HttpContext.Session.GetString("UserRole");
            visit.AdminApprovalStatus = userRole == "Student" ? "Pending" : "Approved";

            _context.Visits.Add(visit);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Patients",
                new { id = visit.PatientID, tab = "todays-visit" });
        }

        // POST: Visits/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int visitId, int patientId)
        {
            var visit = await _context.Visits.FindAsync(visitId);
            if (visit != null)
            {
                _context.Visits.Remove(visit);
                await _context.SaveChangesAsync();
                TempData["VisitSuccess"] = "Visit deleted.";
            }
            return RedirectToAction("Details", "Patients",
                new { id = patientId, tab = "todays-visit" });
        }

        // POST: Visits/Approve (Supervisor Approval)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int visitId, int patientId,
            string? approvedBy, string? supervisorComments)
        {
            var visit = await _context.Visits.FindAsync(visitId);
            if (visit != null)
            {
                visit.IsApproved = true;
                visit.ApprovedBy = approvedBy;
                visit.ApprovedDate = DateTime.Now;
                visit.SupervisorComments = supervisorComments;

                bool alreadyExists = await _context.Appointments
                    .AnyAsync(a => a.PatientID == patientId
                                && a.AppointmentDate.Date == visit.VisitDate.Date);

                if (!alreadyExists)
                {
                    var appointment = new Appointment
                    {
                        PatientID = patientId,
                        ClinicName = "General",
                        AppointmentDate = visit.VisitDate,
                        AppointmentDay = visit.VisitDate.DayOfWeek.ToString(),
                        TimeFrom = TimeSpan.Zero,
                        TimeTo = TimeSpan.Zero,
                        AppointmentStatus = "Approved",
                        CreatedDate = DateTime.Now
                    };
                    _context.Appointments.Add(appointment);
                }

                await _context.SaveChangesAsync();
                TempData["VisitSuccess"] = "Visit approved successfully!";
            }
            return RedirectToAction("Details", "Patients",
                new { id = patientId, tab = "todays-visit" });
        }

        // ✅ POST: Visits/AdminApprove — Admin يوافق على Visit الطالب
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminApprove(int visitId, int patientId)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
                return RedirectToAction("Login", "Account");

            var visit = await _context.Visits.FindAsync(visitId);
            if (visit != null)
            {
                visit.AdminApprovalStatus = "Approved";
                visit.AdminApprovedBy = HttpContext.Session.GetString("FullName");
                visit.AdminApprovedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ Visit approved by Admin!";
            }

            return RedirectToAction("Index", "AdminApprovals");
        }

        // ✅ POST: Visits/AdminReject — Admin يرفض Visit الطالب
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminReject(int visitId, int patientId)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
                return RedirectToAction("Login", "Account");

            var visit = await _context.Visits.FindAsync(visitId);
            if (visit != null)
            {
                _context.Visits.Remove(visit);
                await _context.SaveChangesAsync();
                TempData["Success"] = "🗑️ Visit rejected and removed.";
            }

            return RedirectToAction("Index", "AdminApprovals");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAttendance(int visitId, int patientId)
        {
            var visit = await _context.Visits.FindAsync(visitId);

            if (visit != null)
            {
                if (!visit.Attended)
                {
                    bool hasDetails = !string.IsNullOrWhiteSpace(visit.ChiefComplaint)
                                   || !string.IsNullOrWhiteSpace(visit.ProceduresPerformed)
                                   || !string.IsNullOrWhiteSpace(visit.MaterialsUsed)
                                   || !string.IsNullOrWhiteSpace(visit.Complications)
                                   || !string.IsNullOrWhiteSpace(visit.StudentNotes);

                    if (!hasDetails)
                    {
                        TempData["VisitError"] = "⚠️ Please fill in visit details before marking as attended.";
                        return RedirectToAction("Details", "Patients",
                            new { id = patientId, tab = "todays-visit" });
                    }
                }

                visit.Attended = !visit.Attended;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", "Patients",
                new { id = patientId, tab = "todays-visit" });
        }

        // POST: Visits/SaveDetails
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDetails(Visit model, bool Attended = false)
        {
            var visit = await _context.Visits.FindAsync(model.VisitID);

            if (visit == null)
            {
                TempData["Error"] = "Visit not found.";
                return RedirectToAction("Details", "Patients", new { id = model.PatientID, tab = "todays-visit" });
            }

            visit.VisitDate = model.VisitDate;
            visit.ChiefComplaint = model.ChiefComplaint;
            visit.ProceduresPerformed = model.ProceduresPerformed;
            visit.MaterialsUsed = model.MaterialsUsed;
            visit.Complications = model.Complications;
            visit.StudentNotes = model.StudentNotes;
            visit.Attended = Attended;

            await _context.SaveChangesAsync();

            TempData["VisitSuccess"] = "Visit details saved successfully!";
            return RedirectToAction("Details", "Patients",
                new { id = model.PatientID, tab = "todays-visit" });
        }

        // POST: Visits/SetAttendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetAttendance(int visitId, int patientId, bool attended)
        {
            var visit = await _context.Visits.FindAsync(visitId);

            if (visit != null)
            {
                visit.Attended = attended;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", "Patients",
                new { id = patientId, tab = "todays-visit" });
        }
    }
}