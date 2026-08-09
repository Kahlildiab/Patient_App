using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Filters;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [AuthFilter(
        "Admin",
        "Fulltime Supervisor",
        "Parttime Supervisor",
        "Receptionist",
        "Student"
    )]
    public class AppointmentsController : Controller
    {
        private readonly AppDbContext _context;

        private const int MaxAppointmentsPerPeriod = 10;

        private static readonly TimeSpan MorningTime =
            new TimeSpan(9, 0, 0);

        private static readonly TimeSpan EveningTime =
            new TimeSpan(13, 0, 0);

        public AppointmentsController(
            AppDbContext context)
        {
            _context = context;
        }

        // GET: Appointments
        public async Task<IActionResult> Index()
        {
            string? userRole =
                HttpContext.Session.GetString(
                    "UserRole"
                );

            string? userEmail =
                HttpContext.Session.GetString(
                    "UserEmail"
                );

            List<Appointment> appointments;

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
                    appointments =
                        new List<Appointment>();
                }
                else
                {
                    var assignedPatientIds =
                        await _context.AllocatedStudents
                            .Where(
                                a =>
                                    a.AppUserId
                                    ==
                                    appUser.Id
                            )
                            .Select(
                                a => a.PatientID
                            )
                            .ToListAsync();

                    appointments =
                        await _context.Appointments
                            .Include(
                                a => a.Patient
                            )
                            .Where(
                                a =>
                                    assignedPatientIds
                                        .Contains(
                                            a.PatientID
                                        )
                                    &&
                                    a.Patient
                                        .PatientStatus
                                        ==
                                        "Allocated"
                            )
                            .OrderBy(
                                a => a.AppointmentDate
                            )
                            .ThenBy(
                                a => a.TimeFrom
                            )
                            .ToListAsync();
                }
            }
            else
            {
                appointments =
                    await _context.Appointments
                        .Include(
                            a => a.Patient
                        )
                        .Where(
                            a =>
                                a.Patient.StatusID
                                !=
                                6
                        )
                        .OrderBy(
                            a => a.AppointmentDate
                        )
                        .ThenBy(
                            a => a.TimeFrom
                        )
                        .ToListAsync();
            }

            return View(appointments);
        }

        // GET: Appointments/Details/5
        public async Task<IActionResult> Details(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment =
                await _context.Appointments
                    .Include(
                        a => a.Patient
                    )
                    .FirstOrDefaultAsync(
                        a =>
                            a.AppointmentID
                            ==
                            id.Value
                    );

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // GET: Appointments/Create
        public IActionResult Create()
        {
            ViewData["PatientID"] =
                new SelectList(
                    _context.Patients,
                    "PatientID",
                    "FirstName"
                );

            return View();
        }

        // POST: Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                "AppointmentID,PatientID,ClinicName,"
                +
                "AppointmentDate,AppointmentDay,"
                +
                "TimeFrom,TimeTo,AppointmentStatus,"
                +
                "CreatedDate"
            )]
            Appointment appointment)
        {
            if (!ModelState.IsValid)
            {
                var errors =
                    ModelState
                        .Where(
                            x =>
                                x.Value != null
                                &&
                                x.Value.Errors.Count > 0
                        )
                        .Select(
                            x =>
                                new
                                {
                                    x.Key,
                                    Errors =
                                        x.Value!.Errors
                                }
                        )
                        .ToList();

                foreach (var error in errors)
                {
                    TempData["Error"] +=
                        $"❌ {error.Key}: "
                        +
                        string.Join(
                            ", ",
                            error.Errors.Select(
                                e => e.ErrorMessage
                            )
                        )
                        +
                        " | ";
                }

                return RedirectToAction(
                    "Index",
                    "Patients"
                );
            }

            appointment.CreatedDate =
                DateTime.Now;

            _context.Add(appointment);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "✅ Appointment booked successfully!";

            return RedirectToAction(
                nameof(Index)
            );
        }

        // GET: Appointments/Edit/5
        public async Task<IActionResult> Edit(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment =
                await _context.Appointments
                    .Include(
                        a => a.Patient
                    )
                    .FirstOrDefaultAsync(
                        a =>
                            a.AppointmentID
                            ==
                            id.Value
                    );

            if (appointment == null)
            {
                return NotFound();
            }

            /*
             * شاشة التعديل تعرض فترتين فقط:
             *
             * AM = 09:00
             * PM = 13:00
             */
            ViewBag.AppointmentPeriod =
                appointment.TimeFrom.Hours >= 12
                    ? "PM"
                    : "AM";

            /*
             * أي حالة غير Cancelled تظهر Active.
             */
            appointment.AppointmentStatus =
                string.Equals(
                    appointment.AppointmentStatus,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "Cancelled"
                    : "Scheduled";

            return View(appointment);
        }

        // POST: Appointments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Appointment appointment,
            string? AppointmentPeriod,
            string? FirstName,
            string? SecondName,
            string? ThirdName,
            string? FourthName)
        {
            if (
                id
                !=
                appointment.AppointmentID
            )
            {
                return NotFound();
            }

            /*
             * هذه الحقول لا يرسلها المستخدم من شاشة Edit.
             */
            ModelState.Remove(
                nameof(Appointment.ClinicName)
            );

            ModelState.Remove(
                nameof(Appointment.AppointmentDay)
            );

            ModelState.Remove(
                nameof(Appointment.TimeFrom)
            );

            ModelState.Remove(
                nameof(Appointment.TimeTo)
            );

            ModelState.Remove(
                nameof(Appointment.CreatedDate)
            );

            ModelState.Remove(
                nameof(Appointment.Patient)
            );

            bool periodWasSubmitted =
                string.Equals(
                    AppointmentPeriod,
                    "AM",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    AppointmentPeriod,
                    "PM",
                    StringComparison.OrdinalIgnoreCase
                );

            string normalizedPeriod =
                string.Equals(
                    AppointmentPeriod,
                    "PM",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "PM"
                    : "AM";

            string normalizedStatus =
                string.Equals(
                    appointment.AppointmentStatus,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "Cancelled"
                    : "Scheduled";

            bool isCancelled =
                normalizedStatus
                ==
                "Cancelled";

            if (
                !isCancelled
                &&
                !periodWasSubmitted
            )
            {
                ModelState.AddModelError(
                    "AppointmentPeriod",
                    "Please select Morning or Evening."
                );
            }

            TimeSpan selectedTimeFrom =
                normalizedPeriod == "PM"
                    ? EveningTime
                    : MorningTime;

            TimeSpan selectedTimeTo =
                selectedTimeFrom.Add(
                    TimeSpan.FromHours(2)
                );

            bool appointmentDateIsValid =
                appointment.AppointmentDate
                    !=
                    default
                &&
                appointment.AppointmentDate.Year
                    >=
                    2000;

            if (!appointmentDateIsValid)
            {
                ModelState.AddModelError(
                    nameof(
                        Appointment.AppointmentDate
                    ),
                    "Please select a valid appointment date."
                );
            }
            else if (
                !isCancelled
                &&
                appointment.AppointmentDate.Date
                    <
                    DateTime.Today
            )
            {
                ModelState.AddModelError(
                    nameof(
                        Appointment.AppointmentDate
                    ),
                    "You cannot select a past appointment date."
                );
            }
            /*
             * صفحة Edit تسمح بتحديد Morning أو Evening بحرية،
             * حتى إذا بدأ وقت الفترة في نفس اليوم.
             *
             * يبقى التحقق فقط من:
             * - صحة التاريخ.
             * - منع التاريخ السابق للموعد النشط.
             *
             * لا يوجد حد 10 مرضى أثناء تعديل الموعد.
             * حد السعة والوقت يبقيان مطبقين على الحجز الجديد فقط.
             */

            var existingAppointment =
                await _context.Appointments
                    .Include(
                        a => a.Patient
                    )
                    .FirstOrDefaultAsync(
                        a =>
                            a.AppointmentID
                            ==
                            id
                    );

            if (existingAppointment == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                PrepareEditViewAfterError(
                    appointment,
                    existingAppointment,
                    normalizedPeriod,
                    normalizedStatus,
                    selectedTimeFrom,
                    selectedTimeTo,
                    FirstName,
                    SecondName,
                    ThirdName,
                    FourthName
                );

                return View(appointment);
            }

            try
            {
                Patient? patient =
                    existingAppointment.Patient;

                if (patient == null)
                {
                    patient =
                        await _context.Patients
                            .FirstOrDefaultAsync(
                                p =>
                                    p.PatientID
                                    ==
                                    existingAppointment
                                        .PatientID
                            );
                }

                if (patient != null)
                {
                    patient.FirstName =
                        FirstName?.Trim();

                    patient.SecondName =
                        SecondName?.Trim();

                    patient.ThirdName =
                        ThirdName?.Trim();

                    patient.FourthName =
                        FourthName?.Trim();

                    /*
                     * نبقي نسخة الموعد الحالي في Patient متوافقة
                     * مع جدول Appointments.
                     */
                    patient.AppointmentDate =
                        appointment
                            .AppointmentDate
                            .Date;

                    patient.AppointmentTime =
                        normalizedPeriod
                        +
                        "|"
                        +
                        selectedTimeFrom.ToString(
                            @"hh\:mm"
                        );

                    /*
                     * أي تعديل لموعد نشط يعني أنه أصبح موعداً
                     * جديداً ينتظر تسجيل الحضور من Home/Index.
                     *
                     * لذلك نعيد AttendanceStatus إلى Pending.
                     */
                    if (!isCancelled)
                    {
                        patient.AttendanceStatus =
                            null;
                    }
                }

                /*
                 * ClinicName لا تتغير من شاشة Edit.
                 */
                if (
                    string.IsNullOrWhiteSpace(
                        existingAppointment.ClinicName
                    )
                )
                {
                    existingAppointment.ClinicName =
                        "General";
                }

                existingAppointment.AppointmentDate =
                    appointment
                        .AppointmentDate
                        .Date;

                existingAppointment.AppointmentDay =
                    appointment
                        .AppointmentDate
                        .DayOfWeek
                        .ToString();

                existingAppointment.TimeFrom =
                    selectedTimeFrom;

                existingAppointment.TimeTo =
                    selectedTimeTo;

                /*
                 * عند تعديل التاريخ أو الفترة:
                 * Morning <-> Evening
                 * أو تغيير تاريخ الموعد
                 *
                 * يبقى الموعد Scheduled حتى يتم تسجيل الحضور
                 * من Home/Index.
                 */
                existingAppointment.AppointmentStatus =
                    isCancelled
                        ? "Cancelled"
                        : "Scheduled";

                await _context.SaveChangesAsync();

                string periodLabel =
                    normalizedPeriod == "PM"
                        ? "Evening"
                        : "Morning";

                TempData["Success"] =
                    isCancelled
                        ? "Appointment cancelled successfully."
                        : "Appointment updated successfully for "
                          +
                          $"{appointment.AppointmentDate:yyyy-MM-dd} "
                          +
                          $"({periodLabel}). "
                          +
                          "Its status is Scheduled and attendance "
                          +
                          "is Pending.";

                return RedirectToAction(
                    nameof(Index)
                );
            }
            catch (
                DbUpdateConcurrencyException
            )
            {
                if (!AppointmentExists(id))
                {
                    return NotFound();
                }

                ModelState.AddModelError(
                    string.Empty,
                    "The appointment was modified by "
                    +
                    "another user. Please refresh the "
                    +
                    "page and try again."
                );
            }
            catch (
                DbUpdateException ex
            )
            {
                Console.WriteLine(
                    "Appointment edit database error: "
                    +
                    ex
                );

                ModelState.AddModelError(
                    string.Empty,
                    "A database error occurred while "
                    +
                    "updating the appointment."
                );
            }
            catch (
                Exception ex
            )
            {
                Console.WriteLine(
                    "Appointment edit error: "
                    +
                    ex
                );

                ModelState.AddModelError(
                    string.Empty,
                    "An unexpected error occurred while "
                    +
                    "updating the appointment."
                );
            }

            PrepareEditViewAfterError(
                appointment,
                existingAppointment,
                normalizedPeriod,
                normalizedStatus,
                selectedTimeFrom,
                selectedTimeTo,
                FirstName,
                SecondName,
                ThirdName,
                FourthName
            );

            return View(appointment);
        }

        /*
         * إعادة تجهيز بيانات شاشة Edit عند حدوث خطأ.
         */
        private void PrepareEditViewAfterError(
            Appointment appointment,
            Appointment existingAppointment,
            string normalizedPeriod,
            string normalizedStatus,
            TimeSpan selectedTimeFrom,
            TimeSpan selectedTimeTo,
            string? firstName,
            string? secondName,
            string? thirdName,
            string? fourthName)
        {
            appointment.PatientID =
                existingAppointment.PatientID;

            appointment.Patient =
                existingAppointment.Patient;

            if (appointment.Patient != null)
            {
                appointment.Patient.FirstName =
                    firstName
                    ??
                    appointment.Patient.FirstName;

                appointment.Patient.SecondName =
                    secondName
                    ??
                    appointment.Patient.SecondName;

                appointment.Patient.ThirdName =
                    thirdName
                    ??
                    appointment.Patient.ThirdName;

                appointment.Patient.FourthName =
                    fourthName
                    ??
                    appointment.Patient.FourthName;
            }

            appointment.TimeFrom =
                selectedTimeFrom;

            appointment.TimeTo =
                selectedTimeTo;

            appointment.AppointmentStatus =
                normalizedStatus;

            ViewBag.AppointmentPeriod =
                normalizedPeriod;
        }

        // GET: Appointments/Delete/5
        public async Task<IActionResult> Delete(
            int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment =
                await _context.Appointments
                    .Include(
                        a => a.Patient
                    )
                    .FirstOrDefaultAsync(
                        a =>
                            a.AppointmentID
                            ==
                            id.Value
                    );

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // POST: Appointments/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var appointment =
                await _context.Appointments
                    .FindAsync(id);

            if (appointment != null)
            {
                _context.Appointments.Remove(
                    appointment
                );
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index)
            );
        }

        private bool AppointmentExists(
            int id)
        {
            return _context.Appointments.Any(
                e =>
                    e.AppointmentID
                    ==
                    id
            );
        }

        // GET: Appointments/CheckSlot
        /*
         * هذا الفحص خاص بصفحة Edit فقط.
         *
         * في التعديل:
         * - يمكن اختيار AM أو PM بحرية.
         * - لا نفحص هل وقت الفترة بدأ.
         * - لا نفحص حد 10 مرضى.
         * - نمنع فقط التاريخ السابق والقيم غير الصحيحة.
         */
        [HttpGet]
        public IActionResult CheckSlot(
            string date,
            string period,
            int excludeAppointmentId = 0)
        {
            /*
             * نحافظ على الباراميتر لتوافق الروابط القديمة.
             * لا نحتاجه لأن Edit لا يفحص السعة.
             */
            _ = excludeAppointmentId;

            if (
                !DateTime.TryParse(
                    date,
                    out DateTime parsedDate
                )
            )
            {
                return Json(
                    new
                    {
                        available = false,
                        expired = false,
                        unlimited = true,
                        message =
                            "Please select a valid appointment date."
                    }
                );
            }

            if (
                parsedDate.Date
                <
                DateTime.Today
            )
            {
                return Json(
                    new
                    {
                        available = false,
                        expired = false,
                        unlimited = true,
                        message =
                            "You cannot select a past appointment date."
                    }
                );
            }

            bool periodIsValid =
                string.Equals(
                    period,
                    "AM",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    period,
                    "PM",
                    StringComparison.OrdinalIgnoreCase
                );

            if (!periodIsValid)
            {
                return Json(
                    new
                    {
                        available = false,
                        expired = false,
                        unlimited = true,
                        message =
                            "Please select Morning or Evening."
                    }
                );
            }

            string periodLabel =
                string.Equals(
                    period,
                    "PM",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? "Evening"
                    : "Morning";

            return Json(
                new
                {
                    available = true,
                    expired = false,
                    unlimited = true,
                    message =
                        $"The {periodLabel} period is available for editing. "
                        +
                        "There is no time or 10-patient limit "
                        +
                        "when updating an existing appointment."
                }
            );
        }

        // GET: Appointments/SearchPatient?q=...
        [HttpGet]
        public async Task<IActionResult> SearchPatient(
            string q)
        {
            if (
                string.IsNullOrWhiteSpace(q)
            )
            {
                return Json(
                    new List<object>()
                );
            }

            string? userRole =
                HttpContext.Session.GetString(
                    "UserRole"
                );

            string? userEmail =
                HttpContext.Session.GetString(
                    "UserEmail"
                );

            q =
                q.ToLower()
                    .Trim();

            IQueryable<Patient> query =
                _context.Patients
                    .Where(
                        p =>
                            p.StatusID
                            !=
                            6
                    );

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
                            a =>
                                a.Email
                                ==
                                userEmail
                        );

                if (appUser == null)
                {
                    return Json(
                        new List<object>()
                    );
                }

                var assignedPatientIds =
                    await _context.AllocatedStudents
                        .Where(
                            a =>
                                a.AppUserId
                                ==
                                appUser.Id
                        )
                        .Select(
                            a => a.PatientID
                        )
                        .ToListAsync();

                query =
                    query.Where(
                        p =>
                            assignedPatientIds
                                .Contains(
                                    p.PatientID
                                )
                            &&
                            p.PatientStatus
                                ==
                                "Allocated"
                    );
            }

            var patients =
                await query
                    .Where(
                        p =>
                            (
                                p.FirstName
                                +
                                " "
                                +
                                p.SecondName
                                +
                                " "
                                +
                                p.ThirdName
                                +
                                " "
                                +
                                p.FourthName
                            )
                            .ToLower()
                            .Contains(q)
                            ||
                            (
                                p.NationalID_PassportNumber
                                    !=
                                    null
                                &&
                                p.NationalID_PassportNumber
                                    .ToLower()
                                    .Contains(q)
                            )
                    )
                    .Select(
                        p =>
                            new
                            {
                                p.PatientID,

                                FullName =
                                    p.FirstName
                                    +
                                    " "
                                    +
                                    p.SecondName
                                    +
                                    " "
                                    +
                                    p.ThirdName
                                    +
                                    " "
                                    +
                                    p.FourthName,

                                p.NationalID_PassportNumber
                            }
                    )
                    .Take(10)
                    .ToListAsync();

            return Json(patients);
        }

        // GET: Appointments/GetPatientAttendance
        /*
         * هذا الفحص يتم عند اختيار المريض من نافذة إضافة موعد جديد.
         *
         * يسمح للمريض بالحجز المتكرر بشرط:
         * 1- عدم وجود Visit مفتوحة.
         * 2- عدم وجود موعد نشط حالي أو قادم.
         *
         * المواعيد التالية لا تمنع موعداً جديداً:
         * Completed, Cancelled, NoShow
         */
        [HttpGet]
        public async Task<IActionResult> GetPatientAttendance(
            int patientId)
        {
            bool patientExists =
                await _context.Patients.AnyAsync(
                    patient =>
                        patient.PatientID == patientId
                );

            if (!patientExists)
            {
                return Json(
                    new
                    {
                        canBook = false,
                        message = "Patient not found."
                    }
                );
            }

            bool hasOpenVisit =
                await _context.Visits.AnyAsync(
                    visit =>
                        visit.PatientID == patientId
                        &&
                        !visit.IsClosed
                );

            if (hasOpenVisit)
            {
                return Json(
                    new
                    {
                        canBook = false,
                        message =
                            "This patient has an open visit. "
                            +
                            "Please use End Visit before booking "
                            +
                            "another appointment."
                    }
                );
            }

            DateTime today =
                DateTime.Today;

            bool hasActiveAppointment =
                await _context.Appointments.AnyAsync(
                    appointment =>
                        appointment.PatientID == patientId
                        &&
                        appointment.AppointmentDate.Date
                            >=
                            today
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
                );

            if (hasActiveAppointment)
            {
                return Json(
                    new
                    {
                        canBook = false,
                        message =
                            "This patient already has an active appointment. "
                            +
                            "Please edit the existing appointment, or finish "
                            +
                            "its visit before booking another one."
                    }
                );
            }

            return Json(
                new
                {
                    canBook = true,
                    message = string.Empty
                }
            );
        }

        // POST: Appointments/Save
        /*
         * يضيف Appointment جديداً لنفس PatientID.
         *
         * لا ينشئ Patient جديداً.
         * ملف المريض وتاريخه الطبي يبقيان كما هما.
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            int PatientID,
            DateTime AppointmentDate,
            string AppointmentPeriod,
            string AppointmentTime)
        {
            if (PatientID <= 0)
            {
                TempData["Error"] =
                    "Please select a patient.";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(
                        item =>
                            item.PatientID == PatientID
                    );

            if (patient == null)
            {
                TempData["Error"] =
                    "Patient not found.";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            if (
                AppointmentDate == default
                ||
                AppointmentDate.Year < 2000
            )
            {
                TempData["Error"] =
                    "Please select a valid appointment date.";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            if (
                AppointmentDate.Date
                <
                DateTime.Today
            )
            {
                TempData["Error"] =
                    "You cannot select a past appointment date.";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            bool periodIsValid =
                string.Equals(
                    AppointmentPeriod,
                    "AM",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    AppointmentPeriod,
                    "PM",
                    StringComparison.OrdinalIgnoreCase
                );

            if (!periodIsValid)
            {
                TempData["Error"] =
                    "Please select AM or PM.";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            TimeSpan timeFrom;

            if (
                !string.IsNullOrWhiteSpace(
                    AppointmentTime
                )
                &&
                TimeSpan.TryParse(
                    AppointmentTime,
                    out TimeSpan parsedTime
                )
            )
            {
                timeFrom =
                    parsedTime;
            }
            else
            {
                timeFrom =
                    string.Equals(
                        AppointmentPeriod,
                        "PM",
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? EveningTime
                        : MorningTime;
            }

            string normalizedPeriod =
                timeFrom.Hours >= 12
                    ? "PM"
                    : "AM";

            TimeSpan timeTo =
                timeFrom.Add(
                    TimeSpan.FromHours(2)
                );

            DateTime appointmentStart =
                AppointmentDate.Date.Add(
                    timeFrom
                );

            /*
             * لا يمكن حجز فترة وصل وقت بدايتها اليوم.
             */
            if (
                AppointmentDate.Date
                    ==
                    DateTime.Today
                &&
                DateTime.Now
                    >=
                    appointmentStart
            )
            {
                TempData["Error"] =
                    "This appointment time is no longer available today. "
                    +
                    "Please select a later time or another date.";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            /*
             * الشرط الرئيسي:
             * لا موعد جديد أثناء وجود Visit مفتوحة.
             */
            bool hasOpenVisit =
                await _context.Visits.AnyAsync(
                    visit =>
                        visit.PatientID == PatientID
                        &&
                        !visit.IsClosed
                );

            if (hasOpenVisit)
            {
                TempData["Error"] =
                    "Cannot book a new appointment because "
                    +
                    "this patient has an open visit. "
                    +
                    "Please use End Visit first.";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            /*
             * لا نسمح بموعدين نشطين للمريض في الوقت نفسه.
             */
            bool hasActiveAppointment =
                await _context.Appointments.AnyAsync(
                    existingAppointment =>
                        existingAppointment.PatientID
                            ==
                            PatientID
                        &&
                        existingAppointment.AppointmentDate.Date
                            >=
                            DateTime.Today
                        &&
                        (
                            existingAppointment.AppointmentStatus == null
                            ||
                            existingAppointment.AppointmentStatus == "Scheduled"
                            ||
                            existingAppointment.AppointmentStatus == "Approved"
                            ||
                            existingAppointment.AppointmentStatus == "Attended"
                        )
                );

            if (hasActiveAppointment)
            {
                TempData["Error"] =
                    "This patient already has an active appointment. "
                    +
                    "Please edit the existing appointment, or finish "
                    +
                    "its visit before booking another one.";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            /*
             * فحص حد 10 مرضى على نفس التاريخ ونفس الوقت.
             * المواعيد المغلقة أو الملغاة لا تدخل في السعة.
             */
            int slotCount =
                await _context.Appointments.CountAsync(
                    existingAppointment =>
                        existingAppointment.AppointmentDate.Date
                            ==
                            AppointmentDate.Date
                        &&
                        existingAppointment.TimeFrom
                            ==
                            timeFrom
                        &&
                        existingAppointment.AppointmentStatus
                            !=
                            "Cancelled"
                        &&
                        existingAppointment.AppointmentStatus
                            !=
                            "NoShow"
                        &&
                        existingAppointment.AppointmentStatus
                            !=
                            "Completed"
                );

            if (
                slotCount
                >=
                MaxAppointmentsPerPeriod
            )
            {
                TempData["Error"] =
                    $"This appointment slot is full "
                    +
                    $"({MaxAppointmentsPerPeriod}/"
                    +
                    $"{MaxAppointmentsPerPeriod}).";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            /*
             * Patient يحمل نسخة الموعد الحالي فقط لدعم الصفحات القديمة.
             * جميع المواعيد السابقة والجديدة تبقى محفوظة في Appointments.
             */
            patient.AppointmentDate =
                AppointmentDate.Date;

            patient.AppointmentTime =
                normalizedPeriod
                +
                "|"
                +
                timeFrom.ToString(
                    @"hh\:mm"
                );

            patient.AttendanceStatus =
                null;

            var newAppointment =
                new Appointment
                {
                    PatientID =
                        PatientID,

                    AppointmentDate =
                        AppointmentDate.Date,

                    AppointmentDay =
                        AppointmentDate
                            .DayOfWeek
                            .ToString(),

                    ClinicName =
                        "General",

                    TimeFrom =
                        timeFrom,

                    TimeTo =
                        timeTo,

                    AppointmentStatus =
                        "Scheduled",

                    CreatedDate =
                        DateTime.Now
                };

            _context.Appointments.Add(
                newAppointment
            );

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Appointment saved successfully for "
                +
                $"{patient.FirstName} {patient.FourthName} on "
                +
                $"{AppointmentDate:yyyy-MM-dd} "
                +
                $"({normalizedPeriod} - "
                +
                $"{timeFrom:hh\\:mm}).";

            return RedirectToAction(
                nameof(Index)
            );
        }

        // POST: Appointments/MarkAttendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAttendance(
            int patientId,
            string status)
        {
            var patient =
                await _context.Patients
                    .FindAsync(patientId);

            if (patient == null)
            {
                return NotFound();
            }

            patient.AttendanceStatus =
                status;

            if (
                string.Equals(
                    status,
                    "NoShow",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                var appointment =
                    await _context.Appointments
                        .Where(
                            a =>
                                a.PatientID
                                    ==
                                    patientId
                                &&
                                a.AppointmentStatus
                                    ==
                                    "Scheduled"
                        )
                        .OrderByDescending(
                            a => a.AppointmentDate
                        )
                        .FirstOrDefaultAsync();

                if (appointment != null)
                {
                    appointment.AppointmentStatus =
                        "Cancelled";
                }
            }

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminApprove(
            int id)
        {
            var appointment =
                await _context.Appointments
                    .FindAsync(id);

            if (appointment != null)
            {
                appointment.AppointmentStatus =
                    "Approved";

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "✅ Appointment approved!";
            }

            return RedirectToAction(
                "Index",
                "AdminApprovals"
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminReject(
            int id)
        {
            var appointment =
                await _context.Appointments
                    .FindAsync(id);

            if (appointment != null)
            {
                _context.Appointments.Remove(
                    appointment
                );

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "🗑️ Appointment rejected.";
            }

            return RedirectToAction(
                "Index",
                "AdminApprovals"
            );
        }
    }
}
