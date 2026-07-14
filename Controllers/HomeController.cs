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

            DateTime today =
                DateTime.Today;

            /*
             * Next Appointment يعرض الموعد الفعلي المجدول فقط.
             * بعد تعديل الموعد من Patients/Edit سيتم قراءة
             * التاريخ والوقت الجديدين مباشرة من Appointments.
             */
            var nextAppts =
                await _context.Appointments
                    .AsNoTracking()
                    .Where(
                        a =>
                            patientIds.Contains(
                                a.PatientID
                            )
                            &&
                            a.AppointmentDate.Date
                                >= today
                            &&
                            a.AppointmentStatus
                                == "Scheduled"
                    )
                    .OrderBy(
                        a => a.AppointmentDate
                    )
                    .ThenBy(
                        a => a.TimeFrom
                    )
                    .ToListAsync();

            var nextApptMap =
                nextAppts
                    .GroupBy(a => a.PatientID)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First()
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
                await _context.Appointments
                    .CountAsync(
                        a =>
                            a.AppointmentStatus == "Scheduled"
                            &&
                            patientIds.Contains(a.PatientID)
                    );

            ViewBag.DischargedPatients =
                patients.Count(
                    p => p.PatientStatus == "Discharged"
                );

            return View(patients);
        }

        /*
         * يتم استدعاء هذه الدالة عند الضغط على Yes.
         * لا يتم تسجيل Attended ولا فتح البروفايل إلا إذا كان
         * للمريض موعد فعلي في جدول Appointments بتاريخ اليوم.
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AttendAndOpen(
            int id,
            string? returnUrl)
        {
            if (HttpContext.Session.GetString("UserRole") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            IActionResult ReturnToPatientsList()
            {
                if (
                    !string.IsNullOrWhiteSpace(returnUrl)
                    &&
                    Url.IsLocalUrl(returnUrl)
                )
                {
                    return LocalRedirect(returnUrl);
                }

                return RedirectToAction(nameof(Index));
            }

            var userRole =
                HttpContext.Session.GetString("UserRole") ?? "";

            var userEmail =
                HttpContext.Session.GetString("UserEmail") ?? "";

            /*
             * موظف الاستقبال لا يملك صلاحية فتح تفاصيل المريض.
             * التحقق موجود في السيرفر ولا نعتمد فقط على JavaScript.
             */
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
                        p => p.PatientID == id
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
                            a => a.Email == userEmail
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
                            a =>
                                a.AppUserId == appUser.Id
                                &&
                                a.PatientID == id
                        );

                if (!patientIsAllocatedToStudent)
                {
                    TempData["Error"] =
                        "You are not allowed to access this patient.";

                    return ReturnToPatientsList();
                }
            }

            DateTime today =
                DateTime.Today;

            DateTime tomorrow =
                today.AddDays(1);

            /*
             * البحث عن موعد فعلي بتاريخ اليوم فقط.
             *
             * لا نعتمد على Patient.AttendanceStatus،
             * ولا نعتمد على Patient.AppointmentDate.
             *
             * لذلك حتى لو كانت حالة المريض Attended بالخطأ،
             * لن يفتح البروفايل إذا كان موعده الحقيقي في يوم آخر.
             */
            var todayAppointment =
                await _context.Appointments
                    .Where(
                        a =>
                            a.PatientID == id
                            &&
                            a.AppointmentDate >= today
                            &&
                            a.AppointmentDate < tomorrow
                            &&
                            (
                                a.AppointmentStatus == null
                                ||
                                a.AppointmentStatus != "Cancelled"
                            )
                    )
                    .OrderBy(a => a.AppointmentDate)
                    .FirstOrDefaultAsync();

            /*
             * لا يوجد موعد اليوم:
             * نبحث عن أقرب موعد قادم لعرض تاريخه في الرسالة.
             */
            if (todayAppointment == null)
            {
                var nextAppointment =
                    await _context.Appointments
                        .Where(
                            a =>
                                a.PatientID == id
                                &&
                                a.AppointmentDate >= tomorrow
                                &&
                                (
                                    a.AppointmentStatus == null
                                    ||
                                    a.AppointmentStatus != "Cancelled"
                                )
                        )
                        .OrderBy(a => a.AppointmentDate)
                        .FirstOrDefaultAsync();

                if (nextAppointment != null)
                {
                    TempData["Error"] =
                        "Today is not the patient's appointment date. "
                        +
                        "Please wait until "
                        +
                        nextAppointment.AppointmentDate
                            .ToString("yyyy-MM-dd")
                        +
                        ".";
                }
                else
                {
                    TempData["Error"] =
                        "Today is not the patient's appointment date, "
                        +
                        "and no upcoming appointment was found.";
                }

                return ReturnToPatientsList();
            }

            /*
             * الموعد فعلاً اليوم:
             * الآن فقط نسجل الحضور في الموعد وفي بيانات المريض.
             */
            todayAppointment.AppointmentStatus =
                "Attended";

            patient.AttendanceStatus =
                "Attended";

            await _context.SaveChangesAsync();

            /*
             * فتح بروفايل المريض بعد نجاح فحص التاريخ فقط.
             */
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
         * كذلك لا يتم تسجيل NoShow قبل يوم الموعد.
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNoShow(
            int id,
            string? returnUrl)
        {
            if (HttpContext.Session.GetString("UserRole") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            IActionResult ReturnToPatientsList()
            {
                if (
                    !string.IsNullOrWhiteSpace(returnUrl)
                    &&
                    Url.IsLocalUrl(returnUrl)
                )
                {
                    return LocalRedirect(returnUrl);
                }

                return RedirectToAction(nameof(Index));
            }

            var userRole =
                HttpContext.Session.GetString("UserRole") ?? "";

            var userEmail =
                HttpContext.Session.GetString("UserEmail") ?? "";

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
                        p => p.PatientID == id
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
                            a => a.Email == userEmail
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
                            a =>
                                a.AppUserId == appUser.Id
                                &&
                                a.PatientID == id
                        );

                if (!patientIsAllocatedToStudent)
                {
                    TempData["Error"] =
                        "You are not allowed to update this patient.";

                    return ReturnToPatientsList();
                }
            }

            DateTime today =
                DateTime.Today;

            DateTime tomorrow =
                today.AddDays(1);

            var todayAppointment =
                await _context.Appointments
                    .Where(
                        a =>
                            a.PatientID == id
                            &&
                            a.AppointmentDate >= today
                            &&
                            a.AppointmentDate < tomorrow
                            &&
                            (
                                a.AppointmentStatus == null
                                ||
                                a.AppointmentStatus != "Cancelled"
                            )
                    )
                    .OrderBy(a => a.AppointmentDate)
                    .FirstOrDefaultAsync();

            if (todayAppointment == null)
            {
                var nextAppointment =
                    await _context.Appointments
                        .Where(
                            a =>
                                a.PatientID == id
                                &&
                                a.AppointmentDate >= tomorrow
                                &&
                                (
                                    a.AppointmentStatus == null
                                    ||
                                    a.AppointmentStatus != "Cancelled"
                                )
                        )
                        .OrderBy(a => a.AppointmentDate)
                        .FirstOrDefaultAsync();

                if (nextAppointment != null)
                {
                    TempData["Error"] =
                        "Today is not the patient's appointment date. "
                        +
                        "Please wait until "
                        +
                        nextAppointment.AppointmentDate
                            .ToString("yyyy-MM-dd")
                        +
                        ".";
                }
                else
                {
                    TempData["Error"] =
                        "Today is not the patient's appointment date, "
                        +
                        "and no upcoming appointment was found.";
                }

                return ReturnToPatientsList();
            }

            todayAppointment.AppointmentStatus =
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
