using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Filters;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [AuthFilter]
    public class VisitsController : Controller
    {
        private readonly AppDbContext _context;

        public VisitsController(AppDbContext context)
        {
            _context = context;
        }

        private static bool IsApprovalRole(
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

        private string GetCurrentUserName()
        {
            return
                HttpContext.Session.GetString("FullName")
                ??
                HttpContext.Session.GetString("Username")
                ??
                "Responsible Authority";
        }

        private static string? NormalizeCaseComplexity(
            string? value)
        {
            string normalized =
                (value ?? string.Empty).Trim();

            if (
                normalized.Equals(
                    "Mild",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return "Mild";
            }

            if (
                normalized.Equals(
                    "Moderate",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return "Moderate";
            }

            if (
                normalized.Equals(
                    "Advance",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return "Advance";
            }

            return null;
        }

        // =====================================================
        // Save new visit
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            Visit visit,
            string? AppointmentPeriod)
        {
            visit.AppointmentPeriod =
                AppointmentPeriod;

            visit.CreatedDate =
                DateTime.Now;

            ModelState.Remove("Patient");
            ModelState.Remove("ChiefComplaint");
            ModelState.Remove("ProceduresPerformed");
            ModelState.Remove("MaterialsUsed");
            ModelState.Remove("Complications");
            ModelState.Remove("StudentNotes");

            if (!ModelState.IsValid)
            {
                TempData["VisitError"] =
                    "Please fill in all required fields.";

                return RedirectToPatientVisits(
                    visit.PatientID
                );
            }

            /*
             * نظام موافقة واحد:
             * كل زيارة جديدة تبدأ Pending.
             *
             * ويمكن اعتمادها بواسطة:
             * Admin
             * Fulltime Supervisor
             * Parttime Supervisor
             */
            visit.Attended = false;

            visit.IsApproved = false;
            visit.ApprovedBy = null;
            visit.ApprovedDate = null;
            visit.SupervisorComments = null;

            visit.AdminApprovalStatus =
                "Pending";

            visit.AdminApprovedBy = null;
            visit.AdminApprovedDate = null;

            _context.Visits.Add(visit);

            await _context.SaveChangesAsync();

            TempData["VisitSuccess"] =
                "Visit added successfully and sent for approval.";

            return RedirectToPatientVisits(
                visit.PatientID
            );
        }

        // =====================================================
        // End current visit
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndCurrentVisit(
            int patientId)
        {
            var currentVisit =
                await _context.Visits
                    .Where(
                        visit =>
                            visit.PatientID == patientId
                            &&
                            visit.IsClosed == false
                    )
                    .OrderByDescending(
                        visit =>
                            visit.VisitDate
                    )
                    .ThenByDescending(
                        visit =>
                            visit.VisitID
                    )
                    .FirstOrDefaultAsync();

            if (currentVisit == null)
            {
                TempData["VisitError"] =
                    "There is no active visit for this patient.";

                return RedirectToAction(
                    "Details",
                    "Patients",
                    new
                    {
                        id = patientId,
                        tab = "notes"
                    }
                );
            }

            currentVisit.IsClosed = true;
            currentVisit.ClosedDate = DateTime.Now;

            currentVisit.ClosedBy =
                HttpContext.Session.GetString("Username")
                ??
                HttpContext.Session.GetString("FullName")
                ??
                "Unknown";

            await _context.SaveChangesAsync();

            TempData["VisitSuccess"] =
                "Visit ended successfully. "
                + "All information is now view only.";

            return RedirectToAction(
                "Details",
                "Patients",
                new
                {
                    id = patientId,
                    tab = "notes"
                }
            );
        }

        // =====================================================
        // Delete visit
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int visitId,
            int patientId)
        {
            if (await IsReadOnlyAsync(patientId))
            {
                return ClosedVisitRedirect(patientId);
            }

            var visit =
                await _context.Visits
                    .FirstOrDefaultAsync(
                        currentVisit =>
                            currentVisit.VisitID == visitId
                            &&
                            currentVisit.PatientID == patientId
                    );

            if (visit != null)
            {
                _context.Visits.Remove(visit);

                await _context.SaveChangesAsync();

                TempData["VisitSuccess"] =
                    "Visit deleted.";
            }

            return RedirectToPatientVisits(patientId);
        }

        // =====================================================
        // Direct approval is disabled.
        // Approval must be completed from Admin Approvals.
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthFilter(
            "Admin",
            "Fulltime Supervisor",
            "Parttime Supervisor"
        )]
        public IActionResult Approve(
            int visitId,
            int patientId,
            string? supervisorComments)
        {
            TempData["VisitError"] =
                "Visit approval must be completed from "
                + "the Pending Approvals page so that "
                + "Case Complexity can be selected.";

            return RedirectToAction(
                "Index",
                "AdminApprovals"
            );
        }

        // =====================================================
        // Approval from Admin Approvals page
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthFilter(
            "Admin",
            "Fulltime Supervisor",
            "Parttime Supervisor"
        )]
        public async Task<IActionResult> AdminApprove(
            int visitId,
            int patientId,
            string? caseComplexity)
        {
            var visit =
                await _context.Visits
                    .FirstOrDefaultAsync(
                        currentVisit =>
                            currentVisit.VisitID == visitId
                            &&
                            currentVisit.PatientID == patientId
                    );

            if (visit == null)
            {
                TempData["Error"] =
                    "Visit was not found.";

                return RedirectToAction(
                    "Index",
                    "AdminApprovals"
                );
            }

            /*
             * Case Complexity هي قيمة واحدة للمريض،
             * ويتم تخزينها في أول زيارة فقط.
             */
            string? savedComplexity =
                await _context.Visits
                    .Where(
                        currentVisit =>
                            currentVisit.PatientID == patientId
                            &&
                            currentVisit.CaseComplexity != null
                            &&
                            currentVisit.CaseComplexity != ""
                    )
                    .OrderBy(
                        currentVisit =>
                            currentVisit.VisitDate
                    )
                    .ThenBy(
                        currentVisit =>
                            currentVisit.VisitID
                    )
                    .Select(
                        currentVisit =>
                            currentVisit.CaseComplexity
                    )
                    .FirstOrDefaultAsync();

            bool complexityAddedNow = false;

            if (string.IsNullOrWhiteSpace(savedComplexity))
            {
                string? normalizedComplexity =
                    NormalizeCaseComplexity(
                        caseComplexity
                    );

                if (
                    string.IsNullOrWhiteSpace(
                        normalizedComplexity
                    )
                )
                {
                    TempData["Error"] =
                        "Please select Case Complexity "
                        + "before approving the patient's first visit.";

                    return RedirectToAction(
                        "Index",
                        "AdminApprovals"
                    );
                }

                var firstVisit =
                    await _context.Visits
                        .Where(
                            currentVisit =>
                                currentVisit.PatientID == patientId
                        )
                        .OrderBy(
                            currentVisit =>
                                currentVisit.VisitDate
                        )
                        .ThenBy(
                            currentVisit =>
                                currentVisit.VisitID
                        )
                        .FirstOrDefaultAsync();

                if (firstVisit == null)
                {
                    TempData["Error"] =
                        "The patient's first visit was not found.";

                    return RedirectToAction(
                        "Index",
                        "AdminApprovals"
                    );
                }

                firstVisit.CaseComplexity =
                    normalizedComplexity;

                savedComplexity =
                    normalizedComplexity;

                complexityAddedNow = true;
            }

            await ApproveVisitAsync(
                visit,
                null
            );

            TempData["Success"] =
                complexityAddedNow
                    ? "Visit approved successfully. "
                      + $"Case Complexity ({savedComplexity}) was saved."
                    : "Visit approved successfully. "
                      + $"Case Complexity: {savedComplexity}.";

            return RedirectToAction(
                "Index",
                "AdminApprovals"
            );
        }

        // =====================================================
        // Reject visit from Admin Approvals
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthFilter(
            "Admin",
            "Fulltime Supervisor",
            "Parttime Supervisor"
        )]
        public async Task<IActionResult> AdminReject(
            int visitId,
            int patientId)
        {
            var visit =
                await _context.Visits
                    .FirstOrDefaultAsync(
                        currentVisit =>
                            currentVisit.VisitID == visitId
                            &&
                            currentVisit.PatientID == patientId
                    );

            if (visit != null)
            {
                _context.Visits.Remove(visit);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Visit rejected and removed.";
            }

            return RedirectToAction(
                "Index",
                "AdminApprovals"
            );
        }

        // =====================================================
        // Mark attendance
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAttendance(
            int visitId,
            int patientId)
        {
            if (await IsReadOnlyAsync(patientId))
            {
                return ClosedVisitRedirect(patientId);
            }

            var visit =
                await _context.Visits
                    .FirstOrDefaultAsync(
                        currentVisit =>
                            currentVisit.VisitID == visitId
                            &&
                            currentVisit.PatientID == patientId
                    );

            if (visit != null)
            {
                if (!visit.IsApproved)
                {
                    TempData["VisitError"] =
                        "The visit must be approved before attendance can be updated.";

                    return RedirectToPatientVisits(patientId);
                }

                if (!visit.Attended)
                {
                    bool hasDetails =
                        !string.IsNullOrWhiteSpace(
                            visit.ChiefComplaint
                        )
                        ||
                        !string.IsNullOrWhiteSpace(
                            visit.ProceduresPerformed
                        )
                        ||
                        !string.IsNullOrWhiteSpace(
                            visit.MaterialsUsed
                        )
                        ||
                        !string.IsNullOrWhiteSpace(
                            visit.Complications
                        )
                        ||
                        !string.IsNullOrWhiteSpace(
                            visit.StudentNotes
                        );

                    if (!hasDetails)
                    {
                        TempData["VisitError"] =
                            "Please fill in visit details "
                            + "before marking as attended.";

                        return RedirectToPatientVisits(
                            patientId
                        );
                    }
                }

                visit.Attended =
                    !visit.Attended;

                await _context.SaveChangesAsync();
            }

            return RedirectToPatientVisits(patientId);
        }

        // =====================================================
        // Save visit details
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDetails(
            Visit model,
            bool Attended = false)
        {
            if (await IsReadOnlyAsync(model.PatientID))
            {
                return ClosedVisitRedirect(
                    model.PatientID
                );
            }

            var visit =
                await _context.Visits
                    .FirstOrDefaultAsync(
                        currentVisit =>
                            currentVisit.VisitID
                                == model.VisitID
                            &&
                            currentVisit.PatientID
                                == model.PatientID
                    );

            if (visit == null)
            {
                TempData["VisitError"] =
                    "Visit not found.";

                return RedirectToPatientVisits(
                    model.PatientID
                );
            }

            /*
             * الطالب لا يستطيع إدخال تفاصيل الزيارة
             * قبل اعتمادها.
             */
            string userRole =
                HttpContext.Session.GetString("UserRole")
                ?? string.Empty;

            bool isStudent =
                string.Equals(
                    userRole,
                    "Student",
                    StringComparison.OrdinalIgnoreCase
                );

            if (
                isStudent
                &&
                !visit.IsApproved
            )
            {
                TempData["VisitError"] =
                    "The visit must be approved "
                    + "before details can be changed.";

                return RedirectToPatientVisits(
                    model.PatientID
                );
            }

            visit.VisitDate =
                model.VisitDate;

            visit.ChiefComplaint =
                model.ChiefComplaint;

            visit.ProceduresPerformed =
                model.ProceduresPerformed;

            visit.MaterialsUsed =
                model.MaterialsUsed;

            visit.Complications =
                model.Complications;

            visit.StudentNotes =
                model.StudentNotes;

            visit.Attended =
                Attended;

            await _context.SaveChangesAsync();

            TempData["VisitSuccess"] =
                "Visit details saved successfully.";

            return RedirectToPatientVisits(
                model.PatientID
            );
        }

        // =====================================================
        // Set attendance
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetAttendance(
            int visitId,
            int patientId,
            bool attended)
        {
            if (await IsReadOnlyAsync(patientId))
            {
                return ClosedVisitRedirect(patientId);
            }

            var visit =
                await _context.Visits
                    .FirstOrDefaultAsync(
                        currentVisit =>
                            currentVisit.VisitID == visitId
                            &&
                            currentVisit.PatientID == patientId
                    );

            if (visit != null)
            {
                if (!visit.IsApproved)
                {
                    TempData["VisitError"] =
                        "The visit must be approved "
                        + "before attendance can be updated.";

                    return RedirectToPatientVisits(patientId);
                }

                visit.Attended =
                    attended;

                await _context.SaveChangesAsync();
            }

            return RedirectToPatientVisits(patientId);
        }

        // =====================================================
        // Apply the one unified approval
        // =====================================================
        private async Task ApproveVisitAsync(
            Visit visit,
            string? comments)
        {
            string approverName =
                GetCurrentUserName();

            DateTime approvalDate =
                DateTime.Now;

            /*
             * توحيد حقول الموافقة القديمة والجديدة.
             * بهذه الطريقة لا يظهر:
             * Admin Approved + Supervisor Pending
             * مرة أخرى.
             */
            visit.IsApproved = true;
            visit.ApprovedBy = approverName;
            visit.ApprovedDate = approvalDate;
            visit.SupervisorComments =
                string.IsNullOrWhiteSpace(comments)
                    ? null
                    : comments.Trim();

            visit.AdminApprovalStatus =
                "Approved";

            visit.AdminApprovedBy =
                approverName;

            visit.AdminApprovedDate =
                approvalDate;

            bool appointmentExists =
                await _context.Appointments
                    .AnyAsync(
                        appointment =>
                            appointment.PatientID
                                == visit.PatientID
                            &&
                            appointment.AppointmentDate.Date
                                == visit.VisitDate.Date
                    );

            if (!appointmentExists)
            {
                var appointment =
                    new Appointment
                    {
                        PatientID =
                            visit.PatientID,

                        ClinicName =
                            "General",

                        AppointmentDate =
                            visit.VisitDate,

                        AppointmentDay =
                            visit.VisitDate
                                .DayOfWeek
                                .ToString(),

                        TimeFrom =
                            TimeSpan.Zero,

                        TimeTo =
                            TimeSpan.Zero,

                        AppointmentStatus =
                            "Approved",

                        CreatedDate =
                            DateTime.Now
                    };

                _context.Appointments.Add(
                    appointment
                );
            }

            await _context.SaveChangesAsync();
        }

        private bool IsAdmin()
        {
            return string.Equals(
                HttpContext.Session.GetString("UserRole"),
                "Admin",
                StringComparison.OrdinalIgnoreCase
            );
        }

        private async Task<bool> IsReadOnlyAsync(
            int patientId)
        {
            if (IsAdmin())
            {
                return false;
            }

            var latestVisit =
                await _context.Visits
                    .Where(
                        visit =>
                            visit.PatientID == patientId
                    )
                    .OrderByDescending(
                        visit =>
                            visit.VisitDate
                    )
                    .ThenByDescending(
                        visit =>
                            visit.VisitID
                    )
                    .FirstOrDefaultAsync();

            return
                latestVisit != null
                &&
                latestVisit.IsClosed;
        }

        private IActionResult ClosedVisitRedirect(
            int patientId,
            string tab = "todays-visit")
        {
            TempData["VisitError"] =
                "This visit is closed. "
                + "Only an Administrator can modify it.";

            return RedirectToAction(
                "Details",
                "Patients",
                new
                {
                    id = patientId,
                    tab
                }
            );
        }

        private IActionResult RedirectToPatientVisits(
            int patientId)
        {
            return RedirectToAction(
                "Details",
                "Patients",
                new
                {
                    id = patientId,
                    tab = "todays-visit"
                }
            );
        }
    }
}
