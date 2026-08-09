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
                HttpContext.Session.GetString("UserName")
                ??
                HttpContext.Session.GetString("Username")
                ??
                "Responsible Authority";
        }

        /*
         * نتيجة فحص وقت الحضور.
         */
        private sealed class AttendanceTimeCheckResult
        {
            public bool IsAllowed { get; set; }

            public DateTime ScheduledStart { get; set; }

            public string ErrorMessage { get; set; }
                = string.Empty;
        }

        /*
         * يدعم القيم المخزنة مثل:
         * AM|09:00
         * AM|11:00
         * PM|13:00
         * PM|15:00
         * أو قيمة وقت مباشرة مثل 09:00.
         */
        private static TimeSpan? ParseAppointmentTime(
            string? storedValue)
        {
            if (string.IsNullOrWhiteSpace(storedValue))
            {
                return null;
            }

            string normalized =
                storedValue.Trim();

            if (normalized.Contains("|"))
            {
                string[] parts =
                    normalized.Split(
                        '|',
                        StringSplitOptions
                            .RemoveEmptyEntries
                    );

                if (
                    parts.Length > 0
                    &&
                    TimeSpan.TryParse(
                        parts[^1].Trim(),
                        out TimeSpan parsedFromSlot
                    )
                )
                {
                    return parsedFromSlot;
                }
            }

            if (
                TimeSpan.TryParse(
                    normalized,
                    out TimeSpan parsedTime
                )
            )
            {
                return parsedTime;
            }

            if (
                normalized.Equals(
                    "AM",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return new TimeSpan(8, 0, 0);
            }

            if (
                normalized.Equals(
                    "PM",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return new TimeSpan(13, 0, 0);
            }

            return null;
        }

        /*
         * يمنع تسجيل Attended:
         *
         * 1) قبل يوم الموعد.
         * 2) في يوم آخر غير يوم الموعد.
         * 3) في نفس يوم الموعد ولكن قبل ساعة البداية.
         *
         * يبدأ بالوقت المحفوظ في Patient.AppointmentTime لأنه
         * قد يحتوي على الوقت الدقيق مثل AM|09:00.
         * وإذا لم يتوفر يستخدم Appointment.TimeFrom.
         */
        private async Task<AttendanceTimeCheckResult>
            CheckAttendanceTimeAsync(
                Visit visit)
        {
            DateTime now =
                DateTime.Now;

            DateTime scheduledDate =
                visit.VisitDate.Date;

            TimeSpan? scheduledTime =
                null;

            var patientAppointmentData =
                await _context.Patients
                    .AsNoTracking()
                    .Where(
                        patient =>
                            patient.PatientID
                            == visit.PatientID
                    )
                    .Select(
                        patient => new
                        {
                            patient.AppointmentDate,
                            patient.AppointmentTime
                        }
                    )
                    .FirstOrDefaultAsync();

            /*
             * نستخدم وقت Patient فقط إذا كان تاريخ الموعد
             * المسجل لديه هو نفس تاريخ هذه الزيارة.
             */
            if (
                patientAppointmentData != null
                &&
                patientAppointmentData
                    .AppointmentDate != default
                &&
                patientAppointmentData
                    .AppointmentDate.Date
                    == visit.VisitDate.Date
            )
            {
                scheduledDate =
                    patientAppointmentData
                        .AppointmentDate.Date;

                scheduledTime =
                    ParseAppointmentTime(
                        patientAppointmentData
                            .AppointmentTime
                    );
            }

            /*
             * البحث عن الموعد الموافق لتاريخ الزيارة.
             */
            var appointment =
                await _context.Appointments
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.PatientID
                                == visit.PatientID
                            &&
                            item.AppointmentStatus
                                != "Cancelled"
                            &&
                            item.AppointmentDate.Date
                                == visit.VisitDate.Date
                    )
                    .OrderBy(
                        item => item.TimeFrom
                    )
                    .FirstOrDefaultAsync();

            if (appointment != null)
            {
                scheduledDate =
                    appointment.AppointmentDate.Date;

                /*
                 * لا نستبدل الوقت الدقيق الموجود في Patient
                 * إذا تمكنا من قراءته.
                 */
                if (!scheduledTime.HasValue)
                {
                    scheduledTime =
                        appointment.TimeFrom;
                }
            }

            /*
             * Fallback إذا لم يوجد وقت دقيق.
             */
            if (!scheduledTime.HasValue)
            {
                if (
                    string.Equals(
                        visit.AppointmentPeriod,
                        "PM",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    scheduledTime =
                        new TimeSpan(13, 0, 0);
                }
                else
                {
                    scheduledTime =
                        new TimeSpan(8, 0, 0);
                }
            }

            DateTime scheduledStart =
                scheduledDate.Add(
                    scheduledTime.Value
                );

            if (now.Date < scheduledDate)
            {
                return new AttendanceTimeCheckResult
                {
                    IsAllowed = false,
                    ScheduledStart = scheduledStart,
                    ErrorMessage =
                        "لسا ما إجا موعدك. "
                        + "موعد المريض بتاريخ "
                        + scheduledStart
                            .ToString("yyyy-MM-dd")
                        + " الساعة "
                        + scheduledStart
                            .ToString("HH:mm")
                        + "."
                };
            }

            if (now.Date > scheduledDate)
            {
                return new AttendanceTimeCheckResult
                {
                    IsAllowed = false,
                    ScheduledStart = scheduledStart,
                    ErrorMessage =
                        "لا يمكن تسجيل الحضور لأن "
                        + "موعد هذه الزيارة ليس اليوم. "
                        + "تاريخ الموعد: "
                        + scheduledStart
                            .ToString("yyyy-MM-dd")
                        + "."
                };
            }

            if (now < scheduledStart)
            {
                return new AttendanceTimeCheckResult
                {
                    IsAllowed = false,
                    ScheduledStart = scheduledStart,
                    ErrorMessage =
                        "لسا ما إجا موعدك. "
                        + "موعد المريض اليوم الساعة "
                        + scheduledStart
                            .ToString("HH:mm")
                        + "."
                };
            }

            return new AttendanceTimeCheckResult
            {
                IsAllowed = true,
                ScheduledStart = scheduledStart
            };
        }

        /*
         * إذا لم يأتِ موعد المريض:
         * - لا ندخل إلى ملف المريض.
         * - لا نغيّر Attended.
         * - نرجع إلى صفحة المواعيد ونظهر رسالة واضحة.
         */
        private IActionResult AttendanceTimeBlockedRedirect(
            int patientId,
            string message)
        {
            /*
             * نضع المفتاحين حتى تظهر الرسالة سواء كانت
             * صفحة المواعيد تستخدم Error أو VisitError.
             */
            TempData["Error"] =
                message;

            TempData["VisitError"] =
                message;

            return RedirectToAction(
                "Index",
                "Appointments"
            );
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

            bool hasOpenVisit =
                await _context.Visits.AnyAsync(
                    currentVisit =>
                        currentVisit.PatientID == visit.PatientID
                        && !currentVisit.IsClosed
                );

            if (hasOpenVisit)
            {
                TempData["VisitError"] =
                    "This patient already has an open visit. "
                    + "End the current visit before creating another one.";

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
        /*
         * يغلق Visit الحالية ويحوّل الموعد المرتبط بها إلى Completed.
         *
         * بعد الحفظ:
         * - لا تبقى Visit مفتوحة.
         * - يصبح بإمكان المريض أخذ موعد جديد.
         * - Patient نفسه لا يتكرر ولا ينشأ ملف جديد.
         */
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
                            !visit.IsClosed
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

            string currentRole =
                HttpContext.Session.GetString(
                    "UserRole"
                )
                ??
                string.Empty;

            bool isStudent =
                string.Equals(
                    currentRole,
                    "Student",
                    StringComparison.OrdinalIgnoreCase
                );

            /*
             * الطالب يجب أن يضيف ملاحظة واحدة على الأقل
             * لنفس الزيارة قبل End Visit.
             *
             * No Comment تعتبر ملاحظة صحيحة.
             */
            if (isStudent)
            {
                string currentUserName =
                    GetCurrentUserName();

                bool hasStudentNote =
                    await _context.Notes.AnyAsync(
                        note =>
                            note.VisitId
                                ==
                                currentVisit.VisitID
                            &&
                            note.CreatedByRole
                                ==
                                "Student"
                            &&
                            note.CreatedBy
                                ==
                                currentUserName
                    );

                if (!hasStudentNote)
                {
                    TempData["VisitError"] =
                        "You must add at least one note before ending "
                        +
                        "the visit. If there is nothing to add, "
                        +
                        "enter \"No Comment\" as the note.";

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
            }

            currentVisit.IsClosed =
                true;

            currentVisit.ClosedDate =
                DateTime.Now;

            currentVisit.ClosedBy =
                GetCurrentUserName();

            /*
             * لا يوجد AppointmentID داخل Visit لأننا لا نريد
             * تغيير قاعدة البيانات.
             *
             * نحدد الموعد من:
             * PatientID + VisitDate + AM/PM + الحالة Attended.
             */
            bool visitIsEvening =
                string.Equals(
                    currentVisit.AppointmentPeriod,
                    "PM",
                    StringComparison.OrdinalIgnoreCase
                );

            DateTime latestAllowedAppointmentCreation =
                currentVisit.CreatedDate.AddMinutes(1);

            var appointmentToComplete =
                await _context.Appointments
                    .Where(
                        appointment =>
                            appointment.PatientID == patientId
                            &&
                            appointment.AppointmentDate.Date
                                ==
                                currentVisit.VisitDate.Date
                            &&
                            appointment.AppointmentStatus
                                ==
                                "Attended"
                            &&
                            appointment.CreatedDate
                                <=
                                latestAllowedAppointmentCreation
                            &&
                            (
                                visitIsEvening
                                    ? appointment.TimeFrom.Hours >= 12
                                    : appointment.TimeFrom.Hours < 12
                            )
                    )
                    .OrderByDescending(
                        appointment =>
                            appointment.CreatedDate
                    )
                    .ThenByDescending(
                        appointment =>
                            appointment.AppointmentID
                    )
                    .FirstOrDefaultAsync();

            /*
             * Fallback للبيانات القديمة التي قد لا تحتوي
             * AppointmentPeriod أو CreatedDate بشكل متناسق.
             */
            if (appointmentToComplete == null)
            {
                appointmentToComplete =
                    await _context.Appointments
                        .Where(
                            appointment =>
                                appointment.PatientID == patientId
                                &&
                                appointment.AppointmentDate.Date
                                    ==
                                    currentVisit.VisitDate.Date
                                &&
                                appointment.AppointmentStatus
                                    ==
                                    "Attended"
                        )
                        .OrderByDescending(
                            appointment =>
                                appointment.CreatedDate
                        )
                        .ThenByDescending(
                            appointment =>
                                appointment.AppointmentID
                        )
                        .FirstOrDefaultAsync();
            }

            if (appointmentToComplete != null)
            {
                appointmentToComplete.AppointmentStatus =
                    "Completed";
            }

            await _context.SaveChangesAsync();

            TempData["VisitSuccess"] =
                "Visit ended successfully. "
                +
                "The appointment is completed and the patient "
                +
                "can now book a new appointment.";

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

            if (visit == null)
            {
                TempData["VisitError"] =
                    "Visit not found.";

                return RedirectToPatientVisits(
                    patientId
                );
            }

            /*
             * يتم فحص الموعد فقط عند محاولة تحويل
             * Attended من false إلى true.
             */
            if (!visit.Attended)
            {
                AttendanceTimeCheckResult timeCheck =
                    await CheckAttendanceTimeAsync(
                        visit
                    );

                if (!timeCheck.IsAllowed)
                {
                    return AttendanceTimeBlockedRedirect(
                        patientId,
                        timeCheck.ErrorMessage
                    );
                }
            }

            if (!visit.IsApproved)
            {
                TempData["VisitError"] =
                    "The visit must be approved "
                    + "before attendance can be updated.";

                return RedirectToPatientVisits(
                    patientId
                );
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

                /*
                 * لا نستخدم toggle حتى لا يتم إلغاء الحضور
                 * بالضغط مرة أخرى أو بتغيير الطلب.
                 */
                visit.Attended = true;

                await _context.SaveChangesAsync();

                TempData["VisitSuccess"] =
                    "Patient attendance was recorded successfully.";
            }

            return RedirectToPatientVisits(
                patientId
            );
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

            string userRole =
                HttpContext.Session.GetString("UserRole")
                ?? string.Empty;

            bool isStudent =
                string.Equals(
                    userRole,
                    "Student",
                    StringComparison.OrdinalIgnoreCase
                );

            /*
             * إذا كان النموذج يحاول تسجيل الحضور لأول مرة،
             * نتحقق من يوم الموعد وساعة بدايته.
             */
            if (
                Attended
                &&
                !visit.Attended
            )
            {
                AttendanceTimeCheckResult timeCheck =
                    await CheckAttendanceTimeAsync(
                        visit
                    );

                if (!timeCheck.IsAllowed)
                {
                    return AttendanceTimeBlockedRedirect(
                        model.PatientID,
                        timeCheck.ErrorMessage
                    );
                }
            }

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

            /*
             * الطالب لا يستطيع تغيير تاريخ الزيارة
             * لتجاوز شرط الموعد.
             */
            if (!isStudent)
            {
                visit.VisitDate =
                    model.VisitDate;
            }

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

            /*
             * إذا تم تسجيل الحضور سابقاً لا نسمح بإلغائه
             * من خلال تغيير قيمة checkbox أو الطلب.
             */
            if (!visit.Attended)
            {
                visit.Attended =
                    Attended;
            }

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

            if (visit == null)
            {
                TempData["VisitError"] =
                    "Visit not found.";

                return RedirectToPatientVisits(
                    patientId
                );
            }

            /*
             * يتم فحص الموعد فقط عند تسجيل الحضور لأول مرة.
             */
            if (
                attended
                &&
                !visit.Attended
            )
            {
                AttendanceTimeCheckResult timeCheck =
                    await CheckAttendanceTimeAsync(
                        visit
                    );

                if (!timeCheck.IsAllowed)
                {
                    return AttendanceTimeBlockedRedirect(
                        patientId,
                        timeCheck.ErrorMessage
                    );
                }
            }

            if (!visit.IsApproved)
            {
                TempData["VisitError"] =
                    "The visit must be approved "
                    + "before attendance can be updated.";

                return RedirectToPatientVisits(
                    patientId
                );
            }

            /*
             * نسمح فقط بالتحويل إلى Attended.
             * لا نسمح بإلغاء الحضور بعد تسجيله.
             */
            if (
                attended
                &&
                !visit.Attended
            )
            {
                visit.Attended = true;

                await _context.SaveChangesAsync();

                TempData["VisitSuccess"] =
                    "Patient attendance was recorded successfully.";
            }

            return RedirectToPatientVisits(
                patientId
            );
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
