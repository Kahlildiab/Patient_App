using System;
using System.Linq;
using System.Threading.Tasks;
using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [DentalCollegeManagementSystem_AAU.Filters.AuthFilter]
    public class TreatmentPlanController : Controller
    {
        private readonly AppDbContext _context;

        public TreatmentPlanController(
            AppDbContext context)
        {
            _context = context;
        }

        private static bool IsApproverRole(
            string? userRole)
        {
            return
                string.Equals(
                    userRole,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    userRole,
                    "Fulltime Supervisor",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    userRole,
                    "Parttime Supervisor",
                    StringComparison.OrdinalIgnoreCase
                );
        }

        private bool IsAdmin()
        {
            return string.Equals(
                HttpContext.Session.GetString("UserRole"),
                "Admin",
                StringComparison.OrdinalIgnoreCase
            );
        }

        private async Task<bool> IsClosedVisitReadOnlyAsync(
            int patientId)
        {
            /*
             * بعد إغلاق الزيارة:
             * الأدمن فقط يستطيع التعديل.
             */
            if (IsAdmin())
            {
                return false;
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
                .Select(
                    v => new
                    {
                        v.IsClosed
                    }
                )
                .FirstOrDefaultAsync();

            return
                latestVisit != null &&
                latestVisit.IsClosed;
        }

        private IActionResult RedirectToTreatmentPlan(
            int patientId,
            string? sectionId = null)
        {
            string url =
                $"/Patients/Details/{patientId}"
                + "?tab=treatment-plan";

            if (!string.IsNullOrWhiteSpace(sectionId))
            {
                url += $"#{sectionId}";
            }

            return Redirect(url);
        }

        // =========================================================
        // GENERAL DIAGNOSES AND TREATMENT-PLAN NOTE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            SaveDiagnosisTreatmentPlan(
                int patientId,
                string diagnosisAndTreatmentPlan)
        {
            if (patientId <= 0)
            {
                return NotFound();
            }

            bool patientExists =
                await _context.Patients.AnyAsync(
                    p => p.PatientID == patientId
                );

            if (!patientExists)
            {
                return NotFound();
            }

            if (
                await IsClosedVisitReadOnlyAsync(
                    patientId
                )
            )
            {
                TempData["Error"] =
                    "⚠️ This visit is closed. "
                    + "Only an Administrator can edit "
                    + "Diagnoses and Treatment-plan.";

                return RedirectToTreatmentPlan(patientId);
            }

            string note =
                (diagnosisAndTreatmentPlan ?? string.Empty)
                .Trim();

            if (string.IsNullOrWhiteSpace(note))
            {
                TempData["Error"] =
                    "⚠️ Diagnoses and Treatment-plan "
                    + "note is required.";

                return RedirectToTreatmentPlan(patientId);
            }

            if (note.Length > 4000)
            {
                TempData["Error"] =
                    "⚠️ The note cannot exceed "
                    + "4000 characters.";

                return RedirectToTreatmentPlan(patientId);
            }

            string userRole =
                HttpContext.Session.GetString("UserRole")
                ?? string.Empty;

            string fullName =
                HttpContext.Session.GetString("FullName")
                ?? "Unknown User";

            bool isApprover =
                IsApproverRole(userRole);

            var record =
                await _context.TreatmentPlanDiagnoses
                    .FirstOrDefaultAsync(
                        d => d.PatientId == patientId
                    );

            if (record == null)
            {
                record = new TreatmentPlanDiagnosis
                {
                    PatientId = patientId,
                    DiagnosisAndTreatmentPlan = note,
                    CreatedBy = fullName,
                    CreatedDate = DateTime.Now
                };

                _context.TreatmentPlanDiagnoses.Add(
                    record
                );
            }
            else
            {
                record.DiagnosisAndTreatmentPlan =
                    note;

                record.UpdatedBy = fullName;
                record.UpdatedDate = DateTime.Now;
            }

            if (isApprover)
            {
                record.AdminApprovalStatus =
                    "Approved";

                record.AdminApprovedBy =
                    fullName;

                record.AdminApprovedDate =
                    DateTime.Now;
            }
            else
            {
                /*
                 * إذا عدّل الطالب النص بعد الموافقة،
                 * ترجع الملاحظة Pending للموافقة من جديد.
                 */
                record.AdminApprovalStatus =
                    "Pending";

                record.AdminApprovedBy = null;
                record.AdminApprovedDate = null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                isApprover
                    ? "✅ Diagnoses and Treatment-plan "
                      + "saved and approved successfully."
                    : "✅ Diagnoses and Treatment-plan "
                      + "submitted for approval.";

            return RedirectToTreatmentPlan(patientId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            AdminApproveDiagnosisTreatmentPlan(
                int id,
                int patientId)
        {
            string userRole =
                HttpContext.Session.GetString("UserRole")
                ?? string.Empty;

            if (!IsApproverRole(userRole))
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var record =
                await _context.TreatmentPlanDiagnoses
                    .FindAsync(id);

            if (record != null)
            {
                record.AdminApprovalStatus =
                    "Approved";

                record.AdminApprovedBy =
                    HttpContext.Session
                        .GetString("FullName")
                    ?? "Admin";

                record.AdminApprovedDate =
                    DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "✅ Diagnoses and Treatment-plan "
                    + "approved successfully.";
            }

            return RedirectToAction(
                "Index",
                "AdminApprovals"
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            AdminRejectDiagnosisTreatmentPlan(
                int id,
                int patientId)
        {
            string userRole =
                HttpContext.Session.GetString("UserRole")
                ?? string.Empty;

            if (!IsApproverRole(userRole))
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var record =
                await _context.TreatmentPlanDiagnoses
                    .FindAsync(id);

            if (record != null)
            {
                _context.TreatmentPlanDiagnoses.Remove(
                    record
                );

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "🗑️ Diagnoses and Treatment-plan "
                    + "rejected and removed.";
            }

            return RedirectToAction(
                "Index",
                "AdminApprovals"
            );
        }

        // =========================================================
        // TREATMENT PROCEDURES
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProcedure(
            int PatientId,
            TreatmentProcedure NewProcedure)
        {
            if (PatientId == 0)
            {
                return NotFound();
            }

            if (
                await IsClosedVisitReadOnlyAsync(
                    PatientId
                )
            )
            {
                TempData["Error"] =
                    "⚠️ This visit is closed. "
                    + "Only an Administrator can "
                    + "add a treatment procedure.";

                return RedirectToTreatmentPlan(
                    PatientId
                );
            }

            NewProcedure.PatientId = PatientId;
            NewProcedure.Status =
                ProcedureStatus.Planned;

            string userRole =
                HttpContext.Session.GetString("UserRole")
                ?? string.Empty;

            string fullName =
                HttpContext.Session.GetString("FullName")
                ?? "Admin";

            if (IsApproverRole(userRole))
            {
                NewProcedure.AdminApprovalStatus =
                    "Approved";

                NewProcedure.AdminApprovedBy =
                    fullName;

                NewProcedure.AdminApprovedDate =
                    DateTime.Now;
            }
            else
            {
                NewProcedure.AdminApprovalStatus =
                    "Pending";
            }

            _context.TreatmentProcedures.Add(
                NewProcedure
            );

            await _context.SaveChangesAsync();

            string sectionId =
                NewProcedure.Category switch
                {
                    TreatmentCategory.EmergencyTreatment
                        => "emergency-treatment",

                    TreatmentCategory.StabilizationTreatment
                        => "stabilization-treatment",

                    TreatmentCategory.DefinitiveTreatment
                        => "definitive-treatment",

                    TreatmentCategory.MaintenanceTreatment
                        => "maintenance-treatment",

                    _ => "emergency-treatment"
                };

            return RedirectToTreatmentPlan(
                PatientId,
                sectionId
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProcedure(
            int id,
            int patientId,
            string sectionId)
        {
            if (
                await IsClosedVisitReadOnlyAsync(
                    patientId
                )
            )
            {
                TempData["Error"] =
                    "⚠️ This visit is closed. "
                    + "Only an Administrator can delete.";

                return RedirectToTreatmentPlan(
                    patientId,
                    sectionId
                );
            }

            var procedure =
                await _context.TreatmentProcedures
                    .FindAsync(id);

            if (procedure != null)
            {
                string userRole =
                    HttpContext.Session
                        .GetString("UserRole")
                    ?? string.Empty;

                if (
                    string.Equals(
                        userRole,
                        "Student",
                        StringComparison.OrdinalIgnoreCase
                    )
                    &&
                    procedure.AdminApprovalStatus
                        == "Pending"
                )
                {
                    TempData["Error"] =
                        "⚠️ Cannot delete a "
                        + "pending procedure.";

                    return RedirectToTreatmentPlan(
                        patientId,
                        sectionId
                    );
                }

                _context.TreatmentProcedures.Remove(
                    procedure
                );

                await _context.SaveChangesAsync();
            }

            return RedirectToTreatmentPlan(
                patientId,
                sectionId
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int id,
            ProcedureStatus status,
            int patientId,
            string sectionId)
        {
            if (
                await IsClosedVisitReadOnlyAsync(
                    patientId
                )
            )
            {
                TempData["Error"] =
                    "⚠️ This visit is closed. "
                    + "Only an Administrator can "
                    + "update the status.";

                return RedirectToTreatmentPlan(
                    patientId,
                    sectionId
                );
            }

            var procedure =
                await _context.TreatmentProcedures
                    .FindAsync(id);

            if (procedure != null)
            {
                if (
                    procedure.AdminApprovalStatus
                    == "Pending"
                )
                {
                    TempData["Error"] =
                        "⚠️ Cannot update status "
                        + "until approved.";

                    return RedirectToTreatmentPlan(
                        patientId,
                        sectionId
                    );
                }

                procedure.Status = status;

                procedure.CompletionDate =
                    status == ProcedureStatus.Completed
                        ? DateTime.Now
                        : null;

                await _context.SaveChangesAsync();
            }

            return RedirectToTreatmentPlan(
                patientId,
                sectionId
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminApprove(
            int id,
            int patientId)
        {
            string userRole =
                HttpContext.Session.GetString("UserRole")
                ?? string.Empty;

            if (!IsApproverRole(userRole))
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var procedure =
                await _context.TreatmentProcedures
                    .FindAsync(id);

            if (procedure != null)
            {
                procedure.AdminApprovalStatus =
                    "Approved";

                procedure.AdminApprovedBy =
                    HttpContext.Session
                        .GetString("FullName")
                    ?? "Admin";

                procedure.AdminApprovedDate =
                    DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "✅ Procedure approved successfully!";
            }

            return RedirectToAction(
                "Index",
                "AdminApprovals"
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminReject(
            int id,
            int patientId)
        {
            string userRole =
                HttpContext.Session.GetString("UserRole")
                ?? string.Empty;

            if (!IsApproverRole(userRole))
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var procedure =
                await _context.TreatmentProcedures
                    .FindAsync(id);

            if (procedure != null)
            {
                _context.TreatmentProcedures.Remove(
                    procedure
                );

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "🗑️ Procedure rejected and removed.";
            }

            return RedirectToAction(
                "Index",
                "AdminApprovals"
            );
        }
    }
}
