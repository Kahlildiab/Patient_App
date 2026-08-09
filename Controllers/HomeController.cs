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
        /*
         * فترات السماح الرسمية لفتح زيارة جديدة:
         *
         * Morning:
         * من 09:00 صباح تاريخ الموعد
         * إلى قبل 13:00 من نفس اليوم.
         *
         * Evening:
         * من 13:00 تاريخ الموعد
         * إلى قبل 09:00 صباح اليوم التالي.
         */
        private static readonly TimeSpan MorningShiftStart =
            new TimeSpan(9, 0, 0);

        private static readonly TimeSpan EveningShiftStart =
            new TimeSpan(13, 0, 0);

        /*
         * جميع عمليات التحقق من وقت الموعد تستخدم توقيت الأردن،
         * حتى لو كان السيرفر مستضافاً في منطقة زمنية مختلفة.
         */
        private static DateTime GetJordanNow()
        {
            string timeZoneId = OperatingSystem.IsWindows()
                ? "Jordan Standard Time"
                : "Asia/Amman";

            try
            {
                TimeZoneInfo jordanTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

                return TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    jordanTimeZone
                );
            }
            catch (TimeZoneNotFoundException)
            {
                /*
                 * احتياطياً في حال لم تتوفر بيانات المنطقة الزمنية
                 * على السيرفر، نستخدم فرق الأردن الحالي UTC+03:00.
                 */
                return DateTime.UtcNow.AddHours(3);
            }
            catch (InvalidTimeZoneException)
            {
                return DateTime.UtcNow.AddHours(3);
            }
        }

        private static bool IsEveningAppointment(
            Appointment appointment)
        {
            return appointment.TimeFrom.Hours >= 12;
        }

        private static DateTime GetVisitOpeningTime(
            Appointment appointment)
        {
            return appointment
                .AppointmentDate
                .Date
                .Add(
                    IsEveningAppointment(appointment)
                        ? EveningShiftStart
                        : MorningShiftStart
                );
        }

        private static DateTime GetVisitClosingTime(
            Appointment appointment)
        {
            if (IsEveningAppointment(appointment))
            {
                /*
                 * موعد المساء يبقى صالحاً حتى قبل الساعة
                 * 09:00 صباح اليوم التالي.
                 */
                return appointment
                    .AppointmentDate
                    .Date
                    .AddDays(1)
                    .Add(MorningShiftStart);
            }

            /*
             * موعد الصباح ينتهي عند الساعة 13:00
             * من نفس تاريخ الموعد.
             */
            return appointment
                .AppointmentDate
                .Date
                .Add(EveningShiftStart);
        }

        private static bool AppointmentCanOpenNow(
            Appointment appointment,
            DateTime now)
        {
            DateTime opensAt =
                GetVisitOpeningTime(appointment);

            DateTime closesAt =
                GetVisitClosingTime(appointment);

            return now >= opensAt
                   &&
                   now < closesAt;
        }

        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserRole") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userRole =
                HttpContext.Session.GetString("UserRole");

            var userEmail =
                HttpContext.Session.GetString("UserEmail");

            List<Patient> patients;

            if (userRole == "Student")
            {
                var appUser = await _context.AppUsers
                    .FirstOrDefaultAsync(
                        a => a.Email == userEmail
                    );

                if (appUser == null)
                {
                    patients = new List<Patient>();
                }
                else
                {
                    var assignedPatientIds =
                        await _context.AllocatedStudents
                            .Where(
                                a => a.AppUserId == appUser.Id
                            )
                            .Select(
                                a => a.PatientID
                            )
                            .ToListAsync();

                    patients = await _context.Patients
                        .Include(p => p.Status)
                        .Where(
                            p =>
                                assignedPatientIds.Contains(
                                    p.PatientID
                                )
                                &&
                                p.PatientStatus == "Allocated"
                        )
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

            var patientIds =
                patients
                    .Select(p => p.PatientID)
                    .ToList();

            DateTime now =
                GetJordanNow();

            DateTime today =
                now.Date;

            /*
             * قبل الساعة 09:00 يجب أن نضم موعد المساء
             * الخاص باليوم السابق، لأنه يبقى صالحاً حتى
             * الساعة 09:00 صباح اليوم الحالي.
             */
            DateTime earliestRelevantDate =
                now.TimeOfDay < MorningShiftStart
                    ? today.AddDays(-1)
                    : today;

            /*
             * نجلب المواعيد النشطة أولاً، ثم نطبق نافذة
             * الشفت في الذاكرة لأن موعد Evening يمتد
             * إلى صباح اليوم التالي.
             */
            var activeAppointmentCandidates =
                await _context.Appointments
                    .AsNoTracking()
                    .Where(
                        appointment =>
                            patientIds.Contains(
                                appointment.PatientID
                            )
                            &&
                            appointment.AppointmentDate.Date
                                >=
                                earliestRelevantDate
                            &&
                            (
                                appointment.AppointmentStatus == null
                                ||
                                appointment.AppointmentStatus == "Scheduled"
                                ||
                                appointment.AppointmentStatus == "Approved"
                                ||
                                appointment.AppointmentStatus == "Attended"
                            )
                    )
                    .ToListAsync();

            var nextAppts =
                activeAppointmentCandidates
                    .Where(
                        appointment =>
                            GetVisitClosingTime(
                                appointment
                            )
                            >
                            now
                    )
                    .OrderBy(
                        appointment =>
                            GetVisitOpeningTime(
                                appointment
                            )
                    )
                    .ThenBy(
                        appointment =>
                            appointment.AppointmentID
                    )
                    .ToList();

            var nextApptMap =
                nextAppts
                    .GroupBy(
                        appointment =>
                            appointment.PatientID
                    )
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group.First()
                    );

            ViewBag.NextAppointments =
                nextApptMap;

            ViewBag.TotalPatients =
                patients.Count;

            ViewBag.AcceptedPatients =
                patients.Count(
                    p => p.StatusID == 2
                );

            ViewBag.ScheduledAppointments =
                nextAppts.Count(
                    appointment =>
                        appointment.AppointmentStatus == null
                        ||
                        appointment.AppointmentStatus == "Scheduled"
                        ||
                        appointment.AppointmentStatus == "Approved"
                );

            ViewBag.DischargedPatients =
                patients.Count(
                    p => p.PatientStatus == "Discharged"
                );

            return View(patients);
        }

        /*
         * نقطة الدخول الوحيدة لفتح ملف المريض من Home/Index.
         *
         * يجب ألا تفتح صفحة Home/Index رابط Patients/Details مباشرة
         * اعتماداً على Patient.AttendanceStatus، لأن الحالة قد تكون
         * مرتبطة بزيارة قديمة. جميع حالات Attended تمر من هنا.
         *
         * يتم استدعاء هذه الدالة عند الضغط على Yes، أو عند الضغط
         * على Details لمريض كانت حالته القديمة Attended.
         *
         * الفلو:
         * - Patient يبقى نفس الملف دائماً.
         * - إذا توجد Visit مفتوحة: نفتح نفس الملف ونفس الزيارة.
         * - إذا لا توجد Visit مفتوحة: يجب أن يكون هناك موعد
         *   Scheduled داخل نافذة الشفت الرسمية.
         * - Morning يفتح من 09:00 حتى قبل 13:00.
         * - Evening يفتح من 13:00 حتى قبل 09:00 صباح اليوم التالي.
         * - بعدها يتحول الموعد إلى Attended وتنشأ Visit جديدة واحدة.
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttendAndOpen(
            int id,
            string? returnUrl)
        {
            if (
                HttpContext.Session.GetString(
                    "UserRole"
                )
                ==
                null
            )
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            IActionResult ReturnToPatientsList()
            {
                if (
                    !string.IsNullOrWhiteSpace(
                        returnUrl
                    )
                    &&
                    Url.IsLocalUrl(
                        returnUrl
                    )
                )
                {
                    return LocalRedirect(
                        returnUrl
                    );
                }

                return RedirectToAction(
                    nameof(Index)
                );
            }

            string userRole =
                HttpContext.Session.GetString(
                    "UserRole"
                )
                ??
                string.Empty;

            string userEmail =
                HttpContext.Session.GetString(
                    "UserEmail"
                )
                ??
                string.Empty;

            if (
                string.Equals(
                    userRole,
                    "Receptionist",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                TempData["Error"] =
                    "You don't have permission to view patient details.";

                return ReturnToPatientsList();
            }

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(
                        item =>
                            item.PatientID == id
                    );

            if (patient == null)
            {
                TempData["Error"] =
                    "Patient was not found.";

                return ReturnToPatientsList();
            }

            /*
             * الطالب يستطيع فتح المرضى المخصصين له فقط.
             */
            if (
                string.Equals(
                    userRole,
                    "Student",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                var appUser =
                    await _context.AppUsers
                        .FirstOrDefaultAsync(
                            user =>
                                user.Email == userEmail
                        );

                if (appUser == null)
                {
                    TempData["Error"] =
                        "Your user account was not found.";

                    return ReturnToPatientsList();
                }

                bool patientIsAllocatedToStudent =
                    await _context.AllocatedStudents
                        .AnyAsync(
                            allocation =>
                                allocation.AppUserId
                                    ==
                                    appUser.Id
                                &&
                                allocation.PatientID
                                    ==
                                    id
                        );

                if (!patientIsAllocatedToStudent)
                {
                    TempData["Error"] =
                        "You are not allowed to access this patient.";

                    return ReturnToPatientsList();
                }
            }

            /*
             * إذا كانت الزيارة ما زالت مفتوحة:
             * نفتح نفس ملف المريض ولا ننشئ زيارة أخرى.
             */
            var openVisit =
                await _context.Visits
                    .Where(
                        visit =>
                            visit.PatientID == id
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

            if (openVisit != null)
            {
                if (
                    !string.Equals(
                        patient.AttendanceStatus,
                        "Attended",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    patient.AttendanceStatus =
                        "Attended";

                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(
                    "Details",
                    "Patients",
                    new
                    {
                        id = patient.PatientID
                    }
                );
            }

            DateTime now =
                GetJordanNow();

            DateTime today =
                now.Date;

            DateTime earliestRelevantDate =
                now.TimeOfDay < MorningShiftStart
                    ? today.AddDays(-1)
                    : today;

            /*
             * نقرأ جميع المواعيد النشطة التي قد تكون:
             *
             * - موعد صباح اليوم.
             * - موعد مساء اليوم.
             * - موعد مساء الأمس قبل الساعة 09:00 اليوم.
             * - موعد قادم لعرض رسالة موعد واضحة.
             */
            var activeAppointments =
                await _context.Appointments
                    .Where(
                        appointment =>
                            appointment.PatientID == id
                            &&
                            appointment.AppointmentDate.Date
                                >=
                                earliestRelevantDate
                            &&
                            (
                                appointment.AppointmentStatus == null
                                ||
                                appointment.AppointmentStatus == "Scheduled"
                                ||
                                appointment.AppointmentStatus == "Approved"
                                ||
                                appointment.AppointmentStatus == "Attended"
                            )
                    )
                    .OrderBy(
                        appointment =>
                            appointment.AppointmentDate
                    )
                    .ThenBy(
                        appointment =>
                            appointment.TimeFrom
                    )
                    .ThenBy(
                        appointment =>
                            appointment.AppointmentID
                    )
                    .ToListAsync();

            /*
             * نختار الموعد الذي تقع الساعة الحالية داخل
             * نافذة الشفت الخاصة به.
             */
            var closestActiveAppointment =
                activeAppointments
                    .Where(
                        appointment =>
                            AppointmentCanOpenNow(
                                appointment,
                                now
                            )
                    )
                    .OrderBy(
                        appointment =>
                            GetVisitOpeningTime(
                                appointment
                            )
                    )
                    .ThenBy(
                        appointment =>
                            appointment.AppointmentID
                    )
                    .FirstOrDefault();

            if (closestActiveAppointment == null)
            {
                var nextAppointment =
                    activeAppointments
                        .Where(
                            appointment =>
                                GetVisitOpeningTime(
                                    appointment
                                )
                                >
                                now
                        )
                        .OrderBy(
                            appointment =>
                                GetVisitOpeningTime(
                                    appointment
                                )
                        )
                        .ThenBy(
                            appointment =>
                                appointment.AppointmentID
                        )
                        .FirstOrDefault();

                if (nextAppointment != null)
                {
                    DateTime opensAt =
                        GetVisitOpeningTime(
                            nextAppointment
                        );

                    string nextPeriod =
                        IsEveningAppointment(
                            nextAppointment
                        )
                            ? "Evening"
                            : "Morning";

                    TempData["Error"] =
                        $"The {nextPeriod} visit window has not started yet. "
                        +
                        $"It opens on {opensAt:yyyy-MM-dd} at "
                        +
                        $"{opensAt:HH:mm}.";
                }
                else
                {
                    var latestExpiredAppointment =
                        activeAppointments
                            .Where(
                                appointment =>
                                    GetVisitClosingTime(
                                        appointment
                                    )
                                    <=
                                    now
                            )
                            .OrderByDescending(
                                appointment =>
                                    GetVisitClosingTime(
                                        appointment
                                    )
                            )
                            .ThenByDescending(
                                appointment =>
                                    appointment.AppointmentID
                            )
                            .FirstOrDefault();

                    if (latestExpiredAppointment != null)
                    {
                        DateTime closedAt =
                            GetVisitClosingTime(
                                latestExpiredAppointment
                            );

                        string expiredPeriod =
                            IsEveningAppointment(
                                latestExpiredAppointment
                            )
                                ? "Evening"
                                : "Morning";

                        TempData["Error"] =
                            $"The {expiredPeriod} visit window ended on "
                            +
                            $"{closedAt:yyyy-MM-dd} at {closedAt:HH:mm}. "
                            +
                            "Please edit the appointment date or period "
                            +
                            "and keep it Scheduled.";
                    }
                    else
                    {
                        TempData["Error"] =
                            "No active Scheduled appointment was found "
                            +
                            "for this patient.";
                    }
                }

                return ReturnToPatientsList();
            }

            string appointmentPeriod =
                IsEveningAppointment(
                    closestActiveAppointment
                )
                    ? "PM"
                    : "AM";

            /*
             * لا يوجد AppointmentID داخل Visit حسب طلب عدم تعديل DB.
             *
             * لذلك نربط منطقياً باستخدام:
             * PatientID + VisitDate + AM/PM + CreatedDate.
             *
             * CreatedDate يميّز الزيارة الجديدة عن زيارة قديمة
             * للمريض في نفس التاريخ والفترة.
             */
            DateTime appointmentCreationBoundary =
                closestActiveAppointment
                    .CreatedDate
                    .AddSeconds(-5);

            var visitForThisAppointment =
                await _context.Visits
                    .Where(
                        visit =>
                            visit.PatientID == id
                            &&
                            visit.VisitDate.Date
                                ==
                                closestActiveAppointment
                                    .AppointmentDate
                                    .Date
                            &&
                            visit.AppointmentPeriod
                                ==
                                appointmentPeriod
                            &&
                            visit.CreatedDate
                                >=
                                appointmentCreationBoundary
                    )
                    .OrderByDescending(
                        visit =>
                            visit.VisitID
                    )
                    .FirstOrDefaultAsync();

            /*
             * إذا وجدنا زيارة مغلقة لنفس الموعد،
             * نصلح حالة الموعد ولا ننشئ زيارة مكررة.
             */
            if (
                visitForThisAppointment != null
                &&
                visitForThisAppointment.IsClosed
            )
            {
                closestActiveAppointment.AppointmentStatus =
                    "Completed";

                await _context.SaveChangesAsync();

                TempData["Error"] =
                    "The visit for this appointment is already closed. "
                    +
                    "A new appointment can now be booked.";

                return ReturnToPatientsList();
            }

            if (visitForThisAppointment == null)
            {
                visitForThisAppointment =
                    new Visit
                    {
                        PatientID =
                            id,

                        VisitDate =
                            closestActiveAppointment
                                .AppointmentDate
                                .Date,

                        AppointmentPeriod =
                            appointmentPeriod,

                        Attended =
                            true,

                        IsApproved =
                            false,

                        ApprovedBy =
                            null,

                        ApprovedDate =
                            null,

                        SupervisorComments =
                            null,

                        AdminApprovalStatus =
                            "Pending",

                        AdminApprovedBy =
                            null,

                        AdminApprovedDate =
                            null,

                        CaseComplexity =
                            null,

                        CreatedDate =
                            GetJordanNow(),

                        IsClosed =
                            false,

                        ClosedDate =
                            null,

                        ClosedBy =
                            null
                    };

                _context.Visits.Add(
                    visitForThisAppointment
                );
            }

            closestActiveAppointment.AppointmentStatus =
                "Attended";

            patient.AttendanceStatus =
                "Attended";

            /*
             * نبقي نسخة الموعد الحالي في Patient متزامنة
             * لدعم الصفحات القديمة.
             */
            patient.AppointmentDate =
                closestActiveAppointment
                    .AppointmentDate
                    .Date;

            patient.AppointmentTime =
                appointmentPeriod
                +
                "|"
                +
                closestActiveAppointment
                    .TimeFrom
                    .ToString(
                        @"hh\:mm"
                    );

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Details",
                "Patients",
                new
                {
                    id = patient.PatientID
                }
            );
        }

        /*
         * يتم استدعاء هذه الدالة عند الضغط على No.
         *
         * لا يمكن تسجيل NoShow:
         * - أثناء وجود Visit مفتوحة.
         * - قبل يوم الموعد.
         * - قبل وقت بداية الموعد في نفس اليوم.
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNoShow(
            int id,
            string? returnUrl)
        {
            if (
                HttpContext.Session.GetString(
                    "UserRole"
                )
                ==
                null
            )
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            IActionResult ReturnToPatientsList()
            {
                if (
                    !string.IsNullOrWhiteSpace(
                        returnUrl
                    )
                    &&
                    Url.IsLocalUrl(
                        returnUrl
                    )
                )
                {
                    return LocalRedirect(
                        returnUrl
                    );
                }

                return RedirectToAction(
                    nameof(Index)
                );
            }

            string userRole =
                HttpContext.Session.GetString(
                    "UserRole"
                )
                ??
                string.Empty;

            string userEmail =
                HttpContext.Session.GetString(
                    "UserEmail"
                )
                ??
                string.Empty;

            if (
                string.Equals(
                    userRole,
                    "Receptionist",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                TempData["Error"] =
                    "You don't have permission to update patient attendance.";

                return ReturnToPatientsList();
            }

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(
                        item =>
                            item.PatientID == id
                    );

            if (patient == null)
            {
                TempData["Error"] =
                    "Patient was not found.";

                return ReturnToPatientsList();
            }

            if (
                string.Equals(
                    userRole,
                    "Student",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                var appUser =
                    await _context.AppUsers
                        .FirstOrDefaultAsync(
                            user =>
                                user.Email == userEmail
                        );

                if (appUser == null)
                {
                    TempData["Error"] =
                        "Your user account was not found.";

                    return ReturnToPatientsList();
                }

                bool patientIsAllocatedToStudent =
                    await _context.AllocatedStudents
                        .AnyAsync(
                            allocation =>
                                allocation.AppUserId
                                    ==
                                    appUser.Id
                                &&
                                allocation.PatientID
                                    ==
                                    id
                        );

                if (!patientIsAllocatedToStudent)
                {
                    TempData["Error"] =
                        "You are not allowed to update this patient.";

                    return ReturnToPatientsList();
                }
            }

            bool hasOpenVisit =
                await _context.Visits.AnyAsync(
                    visit =>
                        visit.PatientID == id
                        &&
                        !visit.IsClosed
                );

            if (hasOpenVisit)
            {
                TempData["Error"] =
                    "This patient has an open visit and cannot "
                    +
                    "be marked as No Show. End the current visit first.";

                return ReturnToPatientsList();
            }

            DateTime now =
                GetJordanNow();

            DateTime today =
                now.Date;

            DateTime earliestRelevantDate =
                now.TimeOfDay < MorningShiftStart
                    ? today.AddDays(-1)
                    : today;

            var activeAppointments =
                await _context.Appointments
                    .Where(
                        appointment =>
                            appointment.PatientID == id
                            &&
                            appointment.AppointmentDate.Date
                                >=
                                earliestRelevantDate
                            &&
                            (
                                appointment.AppointmentStatus == null
                                ||
                                appointment.AppointmentStatus == "Scheduled"
                                ||
                                appointment.AppointmentStatus == "Approved"
                            )
                    )
                    .OrderBy(
                        appointment =>
                            appointment.AppointmentDate
                    )
                    .ThenBy(
                        appointment =>
                            appointment.TimeFrom
                    )
                    .ThenBy(
                        appointment =>
                            appointment.AppointmentID
                    )
                    .ToListAsync();

            var closestActiveAppointment =
                activeAppointments
                    .Where(
                        appointment =>
                            GetVisitOpeningTime(
                                appointment
                            )
                            <=
                            now
                    )
                    .OrderByDescending(
                        appointment =>
                            GetVisitOpeningTime(
                                appointment
                            )
                    )
                    .ThenByDescending(
                        appointment =>
                            appointment.AppointmentID
                    )
                    .FirstOrDefault();

            if (closestActiveAppointment == null)
            {
                var nextAppointment =
                    activeAppointments
                        .OrderBy(
                            appointment =>
                                GetVisitOpeningTime(
                                    appointment
                                )
                        )
                        .FirstOrDefault();

                if (nextAppointment != null)
                {
                    DateTime opensAt =
                        GetVisitOpeningTime(
                            nextAppointment
                        );

                    TempData["Error"] =
                        "The patient cannot be marked as No Show "
                        +
                        $"before {opensAt:yyyy-MM-dd} at "
                        +
                        $"{opensAt:HH:mm}.";
                }
                else
                {
                    TempData["Error"] =
                        "No active Scheduled appointment was found "
                        +
                        "for this patient.";
                }

                return ReturnToPatientsList();
            }

            closestActiveAppointment.AppointmentStatus =
                "NoShow";

            patient.AttendanceStatus =
                "NoShow";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Patient attendance was saved as No Show.";

            return ReturnToPatientsList();
        }

        public IActionResult Privacy()
        {
            if (HttpContext.Session.GetString("UserRole") == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id
                        ??
                        HttpContext.TraceIdentifier
                }
            );
        }
    }
}
