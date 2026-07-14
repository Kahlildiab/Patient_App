using System;
using System.Linq;
using System.Threading.Tasks;
using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Filters;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [AuthFilter(
        "Admin",
        "Fulltime Supervisor",
        "Parttime Supervisor"
    )]
    public class AdminApprovalsController : Controller
    {
        private readonly AppDbContext _context;

        public AdminApprovalsController(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            /*
             * نجلب أي زيارة لم تتم الموافقة الموحدة عليها بعد.
             *
             * الشرط الثاني (!v.IsApproved) يلتقط أيضاً البيانات
             * القديمة التي قد تكون AdminApprovalStatus فيها Approved
             * بينما IsApproved ما زالت false.
             */
            var pendingVisits =
                await _context.Visits
                    .Include(v => v.Patient)
                    .Where(
                        v =>
                            v.AdminApprovalStatus == "Pending"
                            ||
                            !v.IsApproved
                    )
                    .OrderByDescending(
                        v => v.CreatedDate
                    )
                    .ToListAsync();

            var pendingDiagnosisTreatmentPlans =
                await _context.TreatmentPlanDiagnoses
                    .Include(d => d.Patient)
                    .Where(
                        d =>
                            d.AdminApprovalStatus
                            == "Pending"
                    )
                    .OrderByDescending(
                        d => d.UpdatedDate
                             ?? d.CreatedDate
                    )
                    .ToListAsync();

            var pendingProcedures =
                await _context.TreatmentProcedures
                    .Include(p => p.Patient)
                    .Where(
                        p =>
                            p.AdminApprovalStatus
                            == "Pending"
                    )
                    .OrderByDescending(
                        p => p.Id
                    )
                    .ToListAsync();

            var pendingOrders =
                await _context.Orders
                    .Include(o => o.Patient)
                    .Include(o => o.CreatedByUser)
                    .Include(o => o.ConsentDetail)
                    .Include(o => o.ReferralDetail)
                    .Include(o => o.MedicationDetail)
                    .Include(o => o.XRayDetail)
                    .Include(o => o.DischargeDetail)
                    .Where(
                        o =>
                            o.AdminApprovalStatus
                            == "Pending"
                    )
                    .OrderByDescending(
                        o => o.CreatedDate
                    )
                    .ToListAsync();

            /*
             * Student notes waiting for Admin / Fulltime Supervisor /
             * Parttime Supervisor approval.
             */
            var pendingStudentNotes =
                await _context.Notes
                    .Include(note => note.Patient)
                    .Include(note => note.Visit)
                    .Where(
                        note =>
                            note.CreatedByRole == "Student"
                            &&
                            note.ApprovalStatus == "Pending"
                    )
                    .OrderByDescending(
                        note => note.CreatedAt
                    )
                    .ToListAsync();

            /*
             * تحميل Case Complexity المحفوظة للمرضى
             * الذين لديهم زيارات Pending.
             */
            var pendingVisitPatientIds =
                pendingVisits
                    .Select(v => v.PatientID)
                    .Distinct()
                    .ToList();

            var visitsForPendingPatients =
                await _context.Visits
                    .Where(
                        v =>
                            pendingVisitPatientIds
                                .Contains(v.PatientID)
                    )
                    .Select(
                        v => new
                        {
                            v.PatientID,
                            v.VisitID,
                            v.VisitDate,
                            v.CaseComplexity
                        }
                    )
                    .ToListAsync();

            var caseComplexityByPatient =
                visitsForPendingPatients
                    .Where(
                        v =>
                            !string.IsNullOrWhiteSpace(
                                v.CaseComplexity
                            )
                    )
                    .GroupBy(v => v.PatientID)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .OrderBy(v => v.VisitDate)
                            .ThenBy(v => v.VisitID)
                            .Select(v => v.CaseComplexity!)
                            .First()
                    );

            ViewBag.CaseComplexityByPatient =
                caseComplexityByPatient;

            ViewBag.PendingVisits =
                pendingVisits;

            ViewBag.PendingDiagnosisTreatmentPlans =
                pendingDiagnosisTreatmentPlans;

            ViewBag.PendingProcedures =
                pendingProcedures;

            ViewBag.PendingOrders =
                pendingOrders;

            ViewBag.PendingStudentNotes =
                pendingStudentNotes;

            ViewBag.TotalPending =
                pendingVisits.Count
                + pendingStudentNotes.Count
                + pendingDiagnosisTreatmentPlans.Count
                + pendingProcedures.Count
                + pendingOrders.Count;

            return View();
        }

        /*
         * Bulk approval is kept as a safety endpoint, but it only
         * approves Treatment Plans whose patient already has a saved
         * Case Complexity. Plans without Case Complexity must be
         * approved individually from the page.
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            ApproveAllDiagnosisTreatmentPlans()
        {
            string adminName =
                HttpContext.Session.GetString("FullName")
                ?? "Admin";

            var pending =
                await _context.TreatmentPlanDiagnoses
                    .Where(
                        d =>
                            d.AdminApprovalStatus
                            == "Pending"
                    )
                    .ToListAsync();

            var patientIds =
                pending
                    .Select(d => d.PatientId)
                    .Distinct()
                    .ToList();

            var patientsWithComplexity =
                await _context.Visits
                    .Where(
                        v =>
                            patientIds.Contains(v.PatientID)
                            &&
                            v.CaseComplexity != null
                            &&
                            v.CaseComplexity != ""
                    )
                    .Select(v => v.PatientID)
                    .Distinct()
                    .ToListAsync();

            var approvable =
                pending
                    .Where(
                        item =>
                            patientsWithComplexity
                                .Contains(item.PatientId)
                    )
                    .ToList();

            foreach (var item in approvable)
            {
                item.AdminApprovalStatus =
                    "Approved";

                item.AdminApprovedBy =
                    adminName;

                item.AdminApprovedDate =
                    DateTime.Now;
            }

            await _context.SaveChangesAsync();

            int skipped =
                pending.Count - approvable.Count;

            TempData["Success"] =
                skipped > 0
                    ? $"✅ {approvable.Count} plan(s) approved. "
                      + $"{skipped} plan(s) skipped because "
                      + "Case Complexity must be selected individually."
                    : $"✅ {approvable.Count} Diagnoses and "
                      + "Treatment-plan note(s) approved successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            ApproveAllProcedures()
        {
            string adminName =
                HttpContext.Session.GetString("FullName")
                ?? "Admin";

            var pending =
                await _context.TreatmentProcedures
                    .Where(
                        p =>
                            p.AdminApprovalStatus
                            == "Pending"
                    )
                    .ToListAsync();

            foreach (var item in pending)
            {
                item.AdminApprovalStatus =
                    "Approved";

                item.AdminApprovedBy =
                    adminName;

                item.AdminApprovedDate =
                    DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"✅ {pending.Count} procedure(s) "
                + "approved successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            ApproveAllVisits()
        {
            string approverName =
                HttpContext.Session.GetString("FullName")
                ?? "Responsible Authority";

            var pending =
                await _context.Visits
                    .Where(
                        v =>
                            v.AdminApprovalStatus == "Pending"
                            ||
                            !v.IsApproved
                    )
                    .ToListAsync();

            var patientIds =
                pending
                    .Select(v => v.PatientID)
                    .Distinct()
                    .ToList();

            /*
             * لا تتم الموافقة الجماعية على أول زيارة
             * ما لم تكن Case Complexity محفوظة مسبقاً.
             */
            var patientsWithComplexity =
                await _context.Visits
                    .Where(
                        v =>
                            patientIds.Contains(v.PatientID)
                            &&
                            v.CaseComplexity != null
                            &&
                            v.CaseComplexity != ""
                    )
                    .Select(v => v.PatientID)
                    .Distinct()
                    .ToListAsync();

            var approvable =
                pending
                    .Where(
                        v =>
                            patientsWithComplexity
                                .Contains(v.PatientID)
                    )
                    .ToList();

            DateTime approvalDate =
                DateTime.Now;

            foreach (var item in approvable)
            {
                item.IsApproved = true;
                item.ApprovedBy = approverName;
                item.ApprovedDate = approvalDate;

                item.AdminApprovalStatus =
                    "Approved";

                item.AdminApprovedBy =
                    approverName;

                item.AdminApprovedDate =
                    approvalDate;
            }

            await _context.SaveChangesAsync();

            int skipped =
                pending.Count - approvable.Count;

            TempData["Success"] =
                skipped > 0
                    ? $"{approvable.Count} visit(s) approved. "
                      + $"{skipped} visit(s) skipped because "
                      + "Case Complexity must be selected individually."
                    : $"{approvable.Count} visit(s) "
                      + "approved successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            ApproveAllOrders()
        {
            string adminName =
                HttpContext.Session.GetString("FullName")
                ?? "Admin";

            var pending =
                await _context.Orders
                    .Where(
                        o =>
                            o.AdminApprovalStatus
                            == "Pending"
                    )
                    .ToListAsync();

            foreach (var item in pending)
            {
                item.AdminApprovalStatus =
                    "Approved";

                item.AdminApprovedBy =
                    adminName;

                item.AdminApprovedDate =
                    DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"✅ {pending.Count} order(s) "
                + "approved successfully.";

            return RedirectToAction(nameof(Index));
        }
        // =====================================================
        // Approve a student note
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveStudentNote(
            int noteId)
        {
            var note =
                await _context.Notes
                    .FirstOrDefaultAsync(
                        currentNote =>
                            currentNote.Id == noteId
                            &&
                            currentNote.CreatedByRole == "Student"
                    );

            if (note == null)
            {
                TempData["Error"] =
                    "Student note was not found.";

                return RedirectToAction(nameof(Index));
            }

            if (
                !string.Equals(
                    note.ApprovalStatus,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                TempData["Error"] =
                    "Only a pending student note can be approved.";

                return RedirectToAction(nameof(Index));
            }

            string approverName =
                HttpContext.Session.GetString("FullName")
                ?? HttpContext.Session.GetString("UserName")
                ?? HttpContext.Session.GetString("Username")
                ?? "Responsible Authority";

            note.ApprovalStatus = "Approved";
            note.ApprovedBy = approverName;
            note.ApprovedDate = DateTime.Now;
            note.RejectionReason = null;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Student note approved successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // Reject a student note and return it for correction
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectStudentNote(
            int noteId,
            string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["Error"] =
                    "Please enter a rejection reason.";

                return RedirectToAction(nameof(Index));
            }

            var note =
                await _context.Notes
                    .FirstOrDefaultAsync(
                        currentNote =>
                            currentNote.Id == noteId
                            &&
                            currentNote.CreatedByRole == "Student"
                    );

            if (note == null)
            {
                TempData["Error"] =
                    "Student note was not found.";

                return RedirectToAction(nameof(Index));
            }

            if (
                !string.Equals(
                    note.ApprovalStatus,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                TempData["Error"] =
                    "Only a pending student note can be rejected.";

                return RedirectToAction(nameof(Index));
            }

            note.ApprovalStatus = "Rejected";
            note.ApprovedBy = null;
            note.ApprovedDate = null;
            note.RejectionReason =
                rejectionReason.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Student note rejected and returned for correction.";

            return RedirectToAction(nameof(Index));
        }

    }
}
