using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    public class TreatmentPlanController : Controller
    {
        private readonly AppDbContext _context;

        public TreatmentPlanController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProcedure(int PatientId, TreatmentProcedure NewProcedure)
        {
            if (PatientId == 0) return NotFound();

            NewProcedure.PatientId = PatientId;
            NewProcedure.Status = ProcedureStatus.Planned;

            var userRole = HttpContext.Session.GetString("UserRole");
            var fullName = HttpContext.Session.GetString("FullName") ?? "Admin";

            if (userRole == "Admin" ||
                userRole == "Fulltime Supervisor" ||
                userRole == "Parttime Supervisor")
            {
                // ✅ Approved مباشرة + خزّن اسم من أضافها
                NewProcedure.AdminApprovalStatus = "Approved";
                NewProcedure.AdminApprovedBy = fullName;
                NewProcedure.AdminApprovedDate = DateTime.Now;
            }
            else
            {
                // ✅ Student → Pending
                NewProcedure.AdminApprovalStatus = "Pending";
            }

            _context.TreatmentProcedures.Add(NewProcedure);
            await _context.SaveChangesAsync();

            var sectionId = NewProcedure.Category switch
            {
                TreatmentCategory.EmergencyTreatment => "emergency-treatment",
                TreatmentCategory.StabilizationTreatment => "stabilization-treatment",
                TreatmentCategory.DefinitiveTreatment => "definitive-treatment",
                TreatmentCategory.MaintenanceTreatment => "maintenance-treatment",
                _ => "emergency-treatment"
            };

            return Redirect($"/Patients/Details/{PatientId}?tab=treatment-plan#{sectionId}");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProcedure(int id, int patientId, string sectionId)
        {
            var procedure = await _context.TreatmentProcedures.FindAsync(id);

            if (procedure != null)
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (userRole == "Student" && procedure.AdminApprovalStatus == "Pending")
                {
                    TempData["Error"] = "⚠️ Cannot delete a pending procedure.";
                    return Redirect($"/Patients/Details/{patientId}?tab=treatment-plan#{sectionId}");
                }

                _context.TreatmentProcedures.Remove(procedure);
                await _context.SaveChangesAsync();
            }

            return Redirect($"/Patients/Details/{patientId}?tab=treatment-plan#{sectionId}");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ProcedureStatus status, int patientId, string sectionId)
        {
            var procedure = await _context.TreatmentProcedures.FindAsync(id);

            if (procedure != null)
            {
                if (procedure.AdminApprovalStatus == "Pending")
                {
                    TempData["Error"] = "⚠️ Cannot update status until approved.";
                    return Redirect($"/Patients/Details/{patientId}?tab=treatment-plan#{sectionId}");
                }

                procedure.Status = status;
                procedure.CompletionDate = status == ProcedureStatus.Completed ? DateTime.Now : null;
                await _context.SaveChangesAsync();
            }

            return Redirect($"/Patients/Details/{patientId}?tab=treatment-plan#{sectionId}");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminApprove(int id, int patientId)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin" &&
                userRole != "Fulltime Supervisor" &&
                userRole != "Parttime Supervisor")
                return RedirectToAction("Login", "Account");

            var procedure = await _context.TreatmentProcedures.FindAsync(id);
            if (procedure != null)
            {
                procedure.AdminApprovalStatus = "Approved";
                procedure.AdminApprovedBy = HttpContext.Session.GetString("FullName") ?? "Admin";
                procedure.AdminApprovedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ Procedure approved successfully!";
            }

            return RedirectToAction("Index", "AdminApprovals");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminReject(int id, int patientId)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin" &&
                userRole != "Fulltime Supervisor" &&
                userRole != "Parttime Supervisor")
                return RedirectToAction("Login", "Account");

            var procedure = await _context.TreatmentProcedures.FindAsync(id);
            if (procedure != null)
            {
                _context.TreatmentProcedures.Remove(procedure);
                await _context.SaveChangesAsync();
                TempData["Success"] = "🗑️ Procedure rejected and removed.";
            }

            return RedirectToAction("Index", "AdminApprovals");
        }
    }
}