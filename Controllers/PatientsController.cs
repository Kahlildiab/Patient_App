using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [DentalCollegeManagementSystem_AAU.Filters.AuthFilter]
    public class PatientsController : Controller
    {
        private readonly AppDbContext _context;

        public PatientsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var patients = await _context.Patients
                .Include(p => p.Status)
                .Where(p => p.StatusID != 6)
                .ToListAsync();

            ViewBag.Clinics = new SelectList(_context.Clinics, "ClinicID", "ClinicName");

            var patientIds = patients.Select(p => p.PatientID).ToList();
            var today = DateTime.Today;

            /*
             * يعرض الموعد الحالي أو القادم فقط.
             * المواعيد الملغاة، NoShow، والمكتملة لا تظهر كموعد قادم.
             */
            var nextAppts = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a =>
                    patientIds.Contains(a.PatientID)
                    && a.AppointmentDate.Date >= today
                    && a.AppointmentStatus != "Cancelled"
                    && a.AppointmentStatus != "NoShow"
                    && a.AppointmentStatus != "Completed")
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.TimeFrom)
                .ToListAsync();

            var nextApptMap = nextAppts
                .GroupBy(a => a.PatientID)
                .ToDictionary(g => g.Key, g => g.First());

            ViewBag.NextAppointments = nextApptMap;

            ViewBag.TotalPatients = await _context.Patients.CountAsync();
            ViewBag.AcceptedPatients = await _context.Patients.CountAsync(p => p.StatusID == 2);
            ViewBag.ScheduledAppointments = await _context.Appointments.CountAsync();
            ViewBag.RejectedPatients = await _context.Patients.CountAsync(p => p.StatusID == 6);

            return View(patients);
        }

        // ─── Public Form ──────────────────────────────────────────────────────────
        [AllowAnonymous]
        public IActionResult CreatePublic() => View();

        [AllowAnonymous]
        public IActionResult CreateSuccess() => View();

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePublic(
            [Bind("PatientID,FirstName,SecondName,ThirdName,FourthName,NationalID_PassportNumber,Nationality,Gender,DateOfBirth,PhoneNumber,Address,FatherName,FatherPhone,MotherName,MotherPhone,AppointmentDate,AppointmentTime")]
            Patient patient, IFormFile? ProfilePhotoFile)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                TempData["Error"] = "Validation errors: " + string.Join(" | ", errors);
                return View("CreatePublic", patient);
            }

            var duplicate = await _context.Patients.FirstOrDefaultAsync(p =>
                p.NationalID_PassportNumber == patient.NationalID_PassportNumber ||
                (p.FirstName == patient.FirstName &&
                 p.FourthName == patient.FourthName &&
                 p.DateOfBirth.HasValue && patient.DateOfBirth.HasValue &&
                 p.DateOfBirth.Value.Year == patient.DateOfBirth.Value.Year &&
                 p.DateOfBirth.Value.Month == patient.DateOfBirth.Value.Month &&
                 p.DateOfBirth.Value.Day == patient.DateOfBirth.Value.Day));

            if (duplicate != null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { isDuplicate = true });
                ModelState.AddModelError(string.Empty, "هذا المريض مسجل مسبقاً في النظام.");
                return View("CreatePublic", patient);
            }

            if (patient.AppointmentDate != default && !string.IsNullOrEmpty(patient.AppointmentTime))
            {
                string normalizedAppointmentTime =
                    NormalizeAppointmentTime(patient.AppointmentTime);

                int slotCount = await _context.Patients.CountAsync(p =>
                    p.StatusID != 6 &&
                    p.AppointmentDate.Date == patient.AppointmentDate.Date &&
                    p.AppointmentTime == normalizedAppointmentTime);

                if (slotCount >= 10)
                {
                    ModelState.AddModelError(string.Empty,
                        $"لا تتوفر أماكن في هذه الفترة ({patient.AppointmentTime}) بتاريخ {patient.AppointmentDate:yyyy-MM-dd}. الطاقة ممتلئة (10/10).");
                    return View("CreatePublic", patient);
                }
            }

            await SavePatientAndAppointment(patient, ProfilePhotoFile);
            return RedirectToAction("CreateSuccess");
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePublicAppointments(
            [Bind("PatientID,FirstName,SecondName,ThirdName,FourthName,NationalID_PassportNumber,Nationality,Gender,DateOfBirth,PhoneNumber,Address,FatherName,FatherPhone,MotherName,MotherPhone,AppointmentDate,AppointmentTime")]
            Patient patient, IFormFile? ProfilePhotoFile)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                TempData["Error"] = "Validation errors: " + string.Join(" | ", errors);
                return View("CreatePublic", patient);
            }

            var duplicate = await _context.Patients.FirstOrDefaultAsync(p =>
                p.NationalID_PassportNumber == patient.NationalID_PassportNumber ||
                (p.FirstName == patient.FirstName &&
                 p.FourthName == patient.FourthName &&
                 p.DateOfBirth.HasValue && patient.DateOfBirth.HasValue &&
                 p.DateOfBirth.Value.Year == patient.DateOfBirth.Value.Year &&
                 p.DateOfBirth.Value.Month == patient.DateOfBirth.Value.Month &&
                 p.DateOfBirth.Value.Day == patient.DateOfBirth.Value.Day));

            if (duplicate != null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { isDuplicate = true });
                ModelState.AddModelError(string.Empty, "هذا المريض مسجل مسبقاً في النظام.");
                return View("CreatePublic", patient);
            }

            if (patient.AppointmentDate != default && !string.IsNullOrEmpty(patient.AppointmentTime))
            {
                string normalizedAppointmentTime =
                    NormalizeAppointmentTime(patient.AppointmentTime);

                int slotCount = await _context.Patients.CountAsync(p =>
                    p.StatusID != 6 &&
                    p.AppointmentDate.Date == patient.AppointmentDate.Date &&
                    p.AppointmentTime == normalizedAppointmentTime);

                if (slotCount >= 10)
                {
                    ModelState.AddModelError(string.Empty,
                        $"لا تتوفر أماكن في هذه الفترة ({patient.AppointmentTime}) بتاريخ {patient.AppointmentDate:yyyy-MM-dd}. الطاقة ممتلئة (10/10).");
                    return View("CreatePublic", patient);
                }
            }

            await SavePatientAndAppointment(patient, ProfilePhotoFile);
            return RedirectToAction("Index", "Appointments");
        }

        // ─── Internal Staff Form ──────────────────────────────────────────────────
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("PatientID,FirstName,SecondName,ThirdName,FourthName,NationalID_PassportNumber,Nationality,Gender,DateOfBirth,PhoneNumber,Address,FatherName,FatherPhone,MotherName,MotherPhone,AppointmentDate,AppointmentTime")]
            Patient patient, IFormFile? ProfilePhotoFile)
        {
            if (ModelState.IsValid)
            {
                var duplicate = await _context.Patients.FirstOrDefaultAsync(p =>
                    p.NationalID_PassportNumber == patient.NationalID_PassportNumber ||
                    (p.FirstName == patient.FirstName &&
                     p.FourthName == patient.FourthName &&
                     p.DateOfBirth.HasValue && patient.DateOfBirth.HasValue &&
                     p.DateOfBirth.Value.Year == patient.DateOfBirth.Value.Year &&
                     p.DateOfBirth.Value.Month == patient.DateOfBirth.Value.Month &&
                     p.DateOfBirth.Value.Day == patient.DateOfBirth.Value.Day));

                if (duplicate != null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { isDuplicate = true });
                    ModelState.AddModelError(string.Empty, "هذا المريض مسجل مسبقاً في النظام.");
                    return View(patient);
                }

                if (patient.AppointmentDate != default && !string.IsNullOrEmpty(patient.AppointmentTime))
                {
                    string normalizedAppointmentTime =
                        NormalizeAppointmentTime(patient.AppointmentTime);

                    int slotCount = await _context.Patients.CountAsync(p =>
                        p.StatusID != 6 &&
                        p.AppointmentDate.Date == patient.AppointmentDate.Date &&
                        p.AppointmentTime == normalizedAppointmentTime);

                    if (slotCount >= 10)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"لا تتوفر أماكن في هذه الفترة ({patient.AppointmentTime}) بتاريخ {patient.AppointmentDate:yyyy-MM-dd}. الطاقة ممتلئة (10/10).");
                        return View(patient);
                    }
                }

                await SavePatientAndAppointment(patient, ProfilePhotoFile);

                TempData["Success"] = $"✅ Patient '{patient.FirstName} {patient.FourthName}' added successfully and appointment scheduled for {patient.AppointmentDate:yyyy-MM-dd} ({patient.AppointmentTime}).";
                return RedirectToAction("Index");
            }
            return View(patient);
        }

        // ✅ Helper
        private async Task SavePatientAndAppointment(Patient patient, IFormFile? ProfilePhotoFile)
        {
            if (ProfilePhotoFile != null && ProfilePhotoFile.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
                if (allowedTypes.Contains(ProfilePhotoFile.ContentType) && ProfilePhotoFile.Length <= 2 * 1024 * 1024)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "patients");
                    Directory.CreateDirectory(uploadsFolder);
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ProfilePhotoFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await ProfilePhotoFile.CopyToAsync(stream);
                    patient.ProfilePhotoPath = $"/uploads/patients/{fileName}";
                }
            }

            patient.StatusID = 2;
            patient.PatientStatus = "Screening";
            _context.Add(patient);
            await _context.SaveChangesAsync();

            if (patient.PatientID == 0)
                throw new Exception("PatientID was not generated after SaveChangesAsync!");

            var appointmentDate = (patient.AppointmentDate != default && patient.AppointmentDate.Year > 2000)
                ? patient.AppointmentDate
                : DateTime.Today;

            /*
             * الوقت القادم من الصفحة أصبح واحداً من قيمتين فقط:
             *
             * 09:00 = الفترة الصباحية
             * 13:00 = الفترة المسائية
             *
             * كل موعد مدته ساعتان.
             */
            TimeSpan timeFrom;

            if (
                string.Equals(
                    patient.AppointmentTime,
                    "13:00",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    patient.AppointmentTime,
                    "PM",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    patient.AppointmentTime,
                    "PM|13:00",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                timeFrom =
                    new TimeSpan(13, 0, 0);

                patient.AppointmentTime =
                    "13:00";
            }
            else
            {
                timeFrom =
                    new TimeSpan(9, 0, 0);

                patient.AppointmentTime =
                    "09:00";
            }

            TimeSpan timeTo =
                timeFrom.Add(
                    TimeSpan.FromHours(2)
                );

            var appointment = new Appointment
            {
                PatientID = patient.PatientID,
                ClinicName = "General",
                AppointmentDate = appointmentDate,
                AppointmentDay = appointmentDate.DayOfWeek.ToString(),
                TimeFrom = timeFrom,
                TimeTo = timeTo,
                AppointmentStatus = "Scheduled",
                CreatedDate = DateTime.Now
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
        }

        // ─── Rejected Patients ────────────────────────────────────────────────────
        public async Task<IActionResult> RejectedPatients()
        {
            var patients = await _context.Patients
                .Include(p => p.Status)
                .Where(p => p.StatusID == 6)
                .ToListAsync();
            return View(patients);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();
            patient.StatusID = 6;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(RejectedPatients));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetInTreatment(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();
            patient.StatusID = 4;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCompleted(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();
            patient.StatusID = 5;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ─── Details ──────────────────────────────────────────────────────────────
        public async Task<IActionResult> Details(int? id, string tab = "medical-history")
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.Status)
                .FirstOrDefaultAsync(m => m.PatientID == id);

            if (patient == null) return NotFound();

            /*
             * حماية السيرفر:
             * الطالب لا يستطيع فتح ملف المريض قبل يوم الموعد.
             * لا نعتمد على تعطيل زر Details فقط لأن الرابط يمكن
             * فتحه مباشرة من المتصفح.
             */
            string currentUserRole =
                HttpContext.Session.GetString("UserRole")
                ?? string.Empty;

            bool currentUserIsStudent =
                string.Equals(
                    currentUserRole,
                    "Student",
                    StringComparison.OrdinalIgnoreCase
                );

            DateTime today = DateTime.Today;

            /*
             * دورة حياة الزيارة من دون أي تعديل على قاعدة البيانات:
             *
             * 1- Patient يبقى ملفاً واحداً دائماً.
             * 2- كل موعد جديد يبقى سجلاً مستقلاً في Appointments.
             * 3- لا ننشئ Visit جديدة إذا كانت هناك Visit مفتوحة.
             * 4- الزيارة الجديدة تنشأ فقط بعد تسجيل حضور موعد اليوم.
             * 5- إعادة تحميل صفحة Details لا تنشئ Visit إضافية.
             */
            var currentOpenVisitForAccess =
                await _context.Visits
                    .Where(
                        visit =>
                            visit.PatientID == id.Value
                            && !visit.IsClosed
                    )
                    .OrderByDescending(visit => visit.VisitDate)
                    .ThenByDescending(visit => visit.VisitID)
                    .FirstOrDefaultAsync();

            var todayAttendedAppointment =
                await _context.Appointments
                    .Where(
                        appointment =>
                            appointment.PatientID == id.Value
                            && appointment.AppointmentDate.Date == today
                            && appointment.AppointmentStatus == "Attended"
                    )
                    .OrderBy(appointment => appointment.TimeFrom)
                    .ThenBy(appointment => appointment.AppointmentID)
                    .FirstOrDefaultAsync();

            var accessAppointment =
                await _context.Appointments
                    .Where(
                        appointment =>
                            appointment.PatientID == id.Value
                            && appointment.AppointmentDate.Date >= today
                            && appointment.AppointmentStatus != "Cancelled"
                            && appointment.AppointmentStatus != "NoShow"
                            && appointment.AppointmentStatus != "Completed"
                    )
                    .OrderBy(appointment => appointment.AppointmentDate)
                    .ThenBy(appointment => appointment.TimeFrom)
                    .FirstOrDefaultAsync();

            bool hasClosedVisit =
                await _context.Visits.AnyAsync(
                    visit =>
                        visit.PatientID == id.Value
                        && visit.IsClosed
                );

            DateTime? detailsAccessDate =
                currentOpenVisitForAccess != null
                    ? currentOpenVisitForAccess.VisitDate.Date
                    : todayAttendedAppointment != null
                        ? todayAttendedAppointment.AppointmentDate.Date
                        : accessAppointment != null
                            ? accessAppointment.AppointmentDate.Date
                            : patient.AppointmentDate != default
                                ? patient.AppointmentDate.Date
                                : null;

            /*
             * الطالب:
             * - يفتح الزيارة المفتوحة الحالية في أي وقت.
             * - يبدأ زيارة جديدة فقط بعد تسجيل حضور موعد اليوم.
             * - يستطيع مشاهدة ملف زيارة سابقة مغلقة إذا لم يكن لديه
             *   موعد قادم ينتظر الحضور.
             */
            if (
                currentUserIsStudent
                && currentOpenVisitForAccess == null
                && todayAttendedAppointment == null
            )
            {
                if (accessAppointment != null)
                {
                    TempData["Error"] =
                        accessAppointment.AppointmentDate.Date > today
                            ? "Patient details can be opened starting from "
                              + "the appointment date: "
                              + $"{accessAppointment.AppointmentDate:yyyy-MM-dd}."
                            : "Please record the patient's attendance before opening a new visit.";

                    return RedirectToAction(nameof(Index));
                }

                if (!hasClosedVisit)
                {
                    TempData["Error"] =
                        "Patient details cannot be opened because there is no active visit or valid appointment.";

                    return RedirectToAction(nameof(Index));
                }
            }

            /*
             * حماية إضافية للرابط المباشر:
             * Home/AttendAndOpen هو المسار الأساسي لإنشاء الزيارة،
             * لكن إذا تم فتح Details مباشرة بعد تسجيل Attended،
             * ننشئ الزيارة هنا مرة واحدة فقط.
             *
             * بما أنه لا يوجد AppointmentID داخل Visit، نربط منطقياً
             * بواسطة PatientID + VisitDate + AM/PM + CreatedDate.
             */
            if (
                currentOpenVisitForAccess == null
                && todayAttendedAppointment != null
            )
            {
                string appointmentPeriod =
                    todayAttendedAppointment.TimeFrom.Hours >= 12
                        ? "PM"
                        : "AM";

                DateTime appointmentCreationBoundary =
                    todayAttendedAppointment.CreatedDate.AddMinutes(-1);

                var visitForThisAppointment =
                    await _context.Visits
                        .Where(
                            visit =>
                                visit.PatientID == id.Value
                                && visit.VisitDate.Date
                                    == todayAttendedAppointment.AppointmentDate.Date
                                && visit.AppointmentPeriod == appointmentPeriod
                                && visit.CreatedDate >= appointmentCreationBoundary
                        )
                        .OrderByDescending(visit => visit.VisitID)
                        .FirstOrDefaultAsync();

                if (visitForThisAppointment == null)
                {
                    var newVisit = new Visit
                    {
                        PatientID = id.Value,
                        VisitDate = todayAttendedAppointment.AppointmentDate.Date,
                        AppointmentPeriod = appointmentPeriod,

                        Attended = false,

                        IsApproved = false,
                        ApprovedBy = null,
                        ApprovedDate = null,
                        SupervisorComments = null,

                        AdminApprovalStatus = "Pending",
                        AdminApprovedBy = null,
                        AdminApprovedDate = null,

                        CaseComplexity = null,

                        CreatedDate = DateTime.Now,
                        IsClosed = false,
                        ClosedDate = null,
                        ClosedBy = null
                    };

                    _context.Visits.Add(newVisit);
                    await _context.SaveChangesAsync();

                    currentOpenVisitForAccess = newVisit;
                }
                else if (!visitForThisAppointment.IsClosed)
                {
                    currentOpenVisitForAccess = visitForThisAppointment;
                }
            }

            var medicalHistory = await _context.MedicalHistories.FirstOrDefaultAsync(m => m.PatientID == id);
            var conditions = await _context.Conditions.Where(c => c.PatientID == id).OrderByDescending(c => c.CreatedDate).ToListAsync();
            var medications = await _context.Medications.Where(m => m.PatientID == id).ToListAsync();
            var visits = await _context.Visits.Where(v => v.PatientID == id).OrderByDescending(v => v.VisitDate).ToListAsync();
            var dentalHistory = await _context.DentalHistories.FirstOrDefaultAsync(d => d.PatientID == id);
            var socialHistory = await _context.SocialHistories.FirstOrDefaultAsync(s => s.PatientID == id);
            var extraoralexam = await _context.ExtraoralExams.FirstOrDefaultAsync(s => s.PatientID == id);
            var intraoralexam = await _context.IntraoralExams.FirstOrDefaultAsync(s => s.PatientID == id);
            var notes = await _context.Notes.Where(n => n.PatientId == id).OrderByDescending(n => n.CreatedAt).ToListAsync();
            var treatmentProcedures = await _context.TreatmentProcedures
                .Where(p => p.PatientId == id)
                .ToListAsync();

            var treatmentPlanDiagnosis =
                await _context.TreatmentPlanDiagnoses
                    .FirstOrDefaultAsync(
                        d => d.PatientId == id.Value
                    );

            ViewBag.MedicalHistory = medicalHistory;
            ViewBag.Conditions = conditions;
            ViewBag.Medications = medications;
            ViewBag.SocialHistory = socialHistory;
            ViewBag.DentalHistory = dentalHistory;
            ViewBag.ExtraoralExam = extraoralexam;
            ViewBag.IntraoralExam = intraoralexam;
            ViewBag.TreatmentProcedures = treatmentProcedures;

            ViewBag.TreatmentPlanDiagnosis =
                treatmentPlanDiagnosis;

            string? patientCaseComplexity =
                visits
                    .Where(
                        v =>
                            !string.IsNullOrWhiteSpace(
                                v.CaseComplexity
                            )
                    )
                    .OrderBy(v => v.VisitDate)
                    .ThenBy(v => v.VisitID)
                    .Select(v => v.CaseComplexity)
                    .FirstOrDefault();

            ViewBag.CaseComplexity =
                string.IsNullOrWhiteSpace(
                    patientCaseComplexity
                )
                    ? "---"
                    : patientCaseComplexity;

            ViewBag.Visits = visits;
            ViewBag.Notes = notes;

            var orders = await _context.Orders
                .Where(o => o.PatientID == id)
                .Include(o => o.CreatedByUser)
                .Include(o => o.ConsentDetail)
                .Include(o => o.ReferralDetail)
                .Include(o => o.MedicationDetail)
                .Include(o => o.XRayDetail)
                .Include(o => o.DischargeDetail)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            ViewBag.Orders = orders;
            ViewBag.Patient = patient;
            ViewBag.PatientId = id;
            ViewBag.ActiveTab = tab;

            ViewBag.Pending = orders.Count(o => o.Status == "Pending");
            ViewBag.Completed = orders.Count(o => o.Status == "Completed");
            ViewBag.XRays = orders.Count(o => o.OrderType == "XRay");
            ViewBag.Referrals = orders.Count(o => o.OrderType == "Referral");

            ViewBag.ExtraoralPhotos = await _context.ExtraoralExamPhotos
                .Where(p => p.ExtraoralExamID == (extraoralexam != null ? extraoralexam.ExtraoralExamID : 0))
                .ToListAsync();

            ViewBag.IntraoralPhotos = await _context.IntraoralExamPhotos
                .Where(p => p.IntraoralExamID == (intraoralexam != null ? intraoralexam.IntraoralExamID : 0))
                .ToListAsync();

            ViewBag.Radiographs = await _context.Radiographs
                .Where(r => r.PatientID == id)
                .OrderByDescending(r => r.UploadedDate)
                .ToListAsync();

            ViewBag.PatientPhotos = await _context.PatientPhotos
                .Where(p => p.PatientID == id)
                .OrderByDescending(p => p.UploadedDate)
                .ToListAsync();

            /*
             * الموعد القادم:
             * - يستثني المواعيد الملغاة.
             * - يستثني مواعيد اليوم التي انتهى وقتها.
             * - يرتب حسب التاريخ ثم وقت البداية.
             */

            TimeSpan currentTime = DateTime.Now.TimeOfDay;

            var nextAppointment =
                await _context.Appointments
                    .Where(
                        a =>
                            a.PatientID == id
                            &&
                            a.AppointmentStatus != "Cancelled"
                            &&
                            a.AppointmentStatus != "NoShow"
                            &&
                            a.AppointmentStatus != "Completed"
                            &&
                            (
                                a.AppointmentDate.Date > today
                                ||
                                (
                                    a.AppointmentDate.Date == today
                                    &&
                                    a.TimeTo >= currentTime
                                )
                            )
                    )
                    .OrderBy(a => a.AppointmentDate)
                    .ThenBy(a => a.TimeFrom)
                    .FirstOrDefaultAsync();

            ViewBag.NextAppointment =
                nextAppointment;

            /*
             * الطلاب المسندون لهذا المريض.
             * يتم جلب أسماء المستخدمين النشطين فقط،
             * وترتيبهم حسب تاريخ الإسناد.
             */
            var assignedStudentNames =
                await (
                    from allocation
                        in _context.AllocatedStudents

                    join appUser
                        in _context.AppUsers
                        on allocation.AppUserId
                        equals appUser.Id

                    where
                        allocation.PatientID == id.Value
                        &&
                        appUser.Status == "Active"

                    orderby allocation.AssignedDate

                    select appUser.NameEn
                )
                .Where(
                    name =>
                        name != null
                        &&
                        name != ""
                )
                .Distinct()
                .ToListAsync();

            ViewBag.AssignedStudentNames =
                assignedStudentNames;

            ViewBag.AssignedStudentText =
                assignedStudentNames.Any()
                    ? string.Join(
                        ", ",
                        assignedStudentNames
                    )
                    : "No student assigned";

            var allAppointments = await _context.Appointments
                .Where(a => a.PatientID == id)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.TimeFrom)
                .ToListAsync();

            ViewBag.AllAppointments =
                allAppointments;

            /*
             * Visit Review:
             *
             * الزيارة الأولى لا يظهر فيها Visit Review.
             * ابتداءً من الزيارة الثانية يظهر القسم.
             *
             * يتم احتساب الزيارات التي:
             * 1- تاريخها اليوم أو قبل اليوم.
             * 2- حالتها Attended فقط.
             *
             * لأن المريض يتم تحويل موعده إلى Attended
             * قبل الدخول إلى صفحة Details.
             */
            int attendedVisitCount =
                allAppointments.Count(
                    appointment =>
                        appointment.AppointmentDate.Date
                            <= DateTime.Today
                        &&
                        string.Equals(
                            appointment.AppointmentStatus,
                            "Attended",
                            StringComparison.OrdinalIgnoreCase
                        )
                );

            ViewBag.AttendedVisitCount =
                attendedVisitCount;

            ViewBag.ShowVisitReview =
                attendedVisitCount >= 2;

            var latestVisit = await _context.Visits
                .Where(v => v.PatientID == id.Value)
                .OrderByDescending(v => v.VisitDate)
                .ThenByDescending(v => v.VisitID)
                .FirstOrDefaultAsync();

            /*
             * The open visit is used by the Notes tab.
             * Every newly added note is linked to this exact visit.
             */
            var currentOpenVisit =
                await _context.Visits
                    .Where(
                        v =>
                            v.PatientID == id.Value
                            &&
                            !v.IsClosed
                    )
                    .OrderByDescending(v => v.VisitDate)
                    .ThenByDescending(v => v.VisitID)
                    .FirstOrDefaultAsync();

            bool isAdmin =
                string.Equals(
                    HttpContext.Session.GetString("UserRole"),
                    "Admin",
                    StringComparison.OrdinalIgnoreCase
                );

            ViewBag.IsVisitClosed =
                latestVisit != null &&
                latestVisit.IsClosed;

            ViewBag.IsAdmin = isAdmin;

            ViewBag.IsReadOnly =
                latestVisit != null &&
                latestVisit.IsClosed &&
                !isAdmin;

            /*
             * Required by the Notes page:
             * - CurrentVisitId enables Add Note only when a visit is open.
             * - A student can end the visit after adding at least one note.
             * - The note does not need to be approved before End Visit.
             */
            ViewBag.CurrentVisitId =
                currentOpenVisit?.VisitID;

            string currentNotesUserName =
                HttpContext.Session.GetString("FullName")
                ??
                HttpContext.Session.GetString("UserName")
                ??
                HttpContext.Session.GetString("Username")
                ??
                "Unknown";

            bool hasCurrentStudentNote =
                currentOpenVisit != null
                &&
                await _context.Notes.AnyAsync(
                    note =>
                        note.VisitId == currentOpenVisit.VisitID
                        &&
                        note.CreatedByRole == "Student"
                        &&
                        note.CreatedBy == currentNotesUserName
                );

            ViewBag.HasCurrentStudentNote =
                hasCurrentStudentNote;

            ViewBag.CanEndCurrentVisit =
                !currentUserIsStudent
                ||
                hasCurrentStudentNote;

            return View(patient);
        }

        // ─── Edit ─────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient =
                await _context.Patients
                    .FirstOrDefaultAsync(
                        p => p.PatientID == id.Value
                    );

            if (patient == null)
            {
                return NotFound();
            }

            var latestVisit =
                await _context.Visits
                    .Where(
                        v => v.PatientID == id.Value
                    )
                    .OrderByDescending(
                        v => v.VisitDate
                    )
                    .ThenByDescending(
                        v => v.VisitID
                    )
                    .FirstOrDefaultAsync();

            bool isAdmin =
                string.Equals(
                    HttpContext.Session.GetString(
                        "UserRole"
                    ),
                    "Admin",
                    StringComparison.OrdinalIgnoreCase
                );

            bool isReadOnly =
                latestVisit != null
                &&
                latestVisit.IsClosed
                &&
                !isAdmin;

            ViewBag.IsVisitClosed =
                latestVisit != null
                &&
                latestVisit.IsClosed;

            ViewBag.IsAdmin =
                isAdmin;

            ViewBag.IsReadOnly =
                isReadOnly;

            if (isReadOnly)
            {
                TempData["Error"] =
                    "This visit is closed. Only an Administrator can edit patient information.";

                return RedirectToAction(
                    "Details",
                    new
                    {
                        id = id.Value,
                        tab = "medical-history"
                    }
                );
            }

            /*
             * نعكس الموعد الحقيقي الموجود في Appointments
             * داخل شاشة Edit بدلاً من الاعتماد على نسخة قديمة
             * مخزنة في Patient فقط.
             */
            var scheduledAppointment =
                await _context.Appointments
                    .Where(
                        a =>
                            a.PatientID == id.Value
                            &&
                            a.AppointmentStatus == "Scheduled"
                    )
                    .OrderBy(a => a.AppointmentDate)
                    .ThenBy(a => a.TimeFrom)
                    .FirstOrDefaultAsync();

            if (scheduledAppointment != null)
            {
                patient.AppointmentDate =
                    scheduledAppointment
                        .AppointmentDate
                        .Date;

                patient.AppointmentTime =
                    scheduledAppointment.TimeFrom.Hours >= 12
                        ? "PM"
                        : "AM";
            }
            else
            {
                string storedTime =
                    (
                        patient.AppointmentTime
                        ??
                        string.Empty
                    )
                    .Trim()
                    .ToUpperInvariant();

                patient.AppointmentTime =
                    storedTime == "PM"
                    ||
                    storedTime == "13:00"
                    ||
                    storedTime == "PM|13:00"
                        ? "PM"
                        : "AM";
            }

            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("PatientID,FirstName,SecondName,ThirdName,FourthName,NationalID_PassportNumber,Nationality,Gender,DateOfBirth,PhoneNumber,Address,FatherName,FatherPhone,MotherName,MotherPhone")]
            Patient patient,
            IFormFile? ProfilePhotoFile,
            string? RemovePhoto)
        {
            if (id != patient.PatientID)
            {
                return NotFound();
            }

            /*
             * نحافظ على نفس حماية الزيارة المغلقة الموجودة في النظام.
             * الطالب/المستخدم العادي لا يستطيع تعديل بيانات المريض
             * بعد إغلاق الزيارة، بينما الـ Admin يستطيع ذلك.
             */
            var latestVisit =
                await _context.Visits
                    .Where(v => v.PatientID == id)
                    .OrderByDescending(v => v.VisitDate)
                    .ThenByDescending(v => v.VisitID)
                    .FirstOrDefaultAsync();

            bool isAdmin =
                string.Equals(
                    HttpContext.Session.GetString("UserRole"),
                    "Admin",
                    StringComparison.OrdinalIgnoreCase
                );

            if (
                latestVisit != null
                && latestVisit.IsClosed
                && !isAdmin
            )
            {
                TempData["Error"] =
                    "This visit is closed. Only an Administrator can edit patient information.";

                return RedirectToAction(
                    "Details",
                    new
                    {
                        id = patient.PatientID,
                        tab = "medical-history"
                    }
                );
            }

            /*
             * مهم جداً:
             * نقرأ السجل الحقيقي من قاعدة البيانات ثم نعدل فقط
             * الحقول الموجودة فعلياً في شاشة Edit.
             *
             * لا نستخدم _context.Update(patient)
             * حتى لا تتأثر StatusID / PatientStatus / AttendanceStatus
             * أو بيانات الموعد أو أي أعمدة أخرى غير موجودة في الصفحة.
             */
            var existingPatient =
                await _context.Patients
                    .FirstOrDefaultAsync(
                        p => p.PatientID == id
                    );

            if (existingPatient == null)
            {
                return NotFound();
            }

            /*
             * شاشة Edit الحالية لا تعدل الموعد.
             * لذلك لا نسمح لأي Validation خاص بالموعد أن يمنع
             * تعديل معلومات المريض الأساسية.
             */
            ModelState.Remove(nameof(Patient.AppointmentDate));
            ModelState.Remove(nameof(Patient.AppointmentTime));
            ModelState.Remove(nameof(Patient.ProfilePhotoPath));

            /*
             * منع استخدام نفس National ID / Passport
             * لمريض آخر.
             */
            if (
                !string.IsNullOrWhiteSpace(
                    patient.NationalID_PassportNumber
                )
            )
            {
                string enteredId =
                    patient.NationalID_PassportNumber.Trim();

                bool duplicateId =
                    await _context.Patients.AnyAsync(
                        p =>
                            p.PatientID != id
                            && p.NationalID_PassportNumber == enteredId
                    );

                if (duplicateId)
                {
                    ModelState.AddModelError(
                        nameof(Patient.NationalID_PassportNumber),
                        "This National ID / Passport Number is already used by another patient."
                    );
                }
            }

            bool removePhotoRequested =
                string.Equals(
                    RemovePhoto,
                    "true",
                    StringComparison.OrdinalIgnoreCase
                );

            /*
             * التحقق من الصورة قبل تعديل أي شيء في قاعدة البيانات.
             */
            if (
                !removePhotoRequested
                && ProfilePhotoFile != null
                && ProfilePhotoFile.Length > 0
            )
            {
                string extension =
                    Path.GetExtension(
                        ProfilePhotoFile.FileName
                    )
                    .ToLowerInvariant();

                string[] allowedExtensions =
                {
                    ".jpg",
                    ".jpeg",
                    ".png"
                };

                string[] allowedContentTypes =
                {
                    "image/jpeg",
                    "image/png",
                    "image/jpg"
                };

                if (
                    !allowedExtensions.Contains(extension)
                    || !allowedContentTypes.Contains(
                        ProfilePhotoFile.ContentType
                    )
                )
                {
                    ModelState.AddModelError(
                        "ProfilePhotoFile",
                        "Only JPG or PNG files are supported."
                    );
                }

                if (
                    ProfilePhotoFile.Length
                    > 2 * 1024 * 1024
                )
                {
                    ModelState.AddModelError(
                        "ProfilePhotoFile",
                        "Image size must be less than 2MB."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                /*
                 * نرجع الصورة الأصلية للـ View فقط.
                 * لا يتم حفظ أي تعديل في قاعدة البيانات.
                 */
                patient.ProfilePhotoPath =
                    existingPatient.ProfilePhotoPath;

                ViewBag.IsVisitClosed =
                    latestVisit != null
                    && latestVisit.IsClosed;

                ViewBag.IsAdmin =
                    isAdmin;

                ViewBag.IsReadOnly =
                    false;

                return View(patient);
            }

            string? oldPhotoPath =
                existingPatient.ProfilePhotoPath;

            string? newPhotoRelativePath = null;
            string? newPhotoFullPath = null;

            try
            {
                /*
                 * إذا تم رفع صورة جديدة:
                 * نحفظ الصورة الجديدة أولاً.
                 * الصورة القديمة لا نحذفها إلا بعد نجاح SaveChangesAsync.
                 */
                if (
                    !removePhotoRequested
                    && ProfilePhotoFile != null
                    && ProfilePhotoFile.Length > 0
                )
                {
                    string extension =
                        Path.GetExtension(
                            ProfilePhotoFile.FileName
                        )
                        .ToLowerInvariant();

                    string uploadsFolder =
                        Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            "uploads",
                            "patients"
                        );

                    Directory.CreateDirectory(
                        uploadsFolder
                    );

                    string fileName =
                        $"{Guid.NewGuid()}{extension}";

                    newPhotoFullPath =
                        Path.Combine(
                            uploadsFolder,
                            fileName
                        );

                    await using (
                        var stream =
                            new FileStream(
                                newPhotoFullPath,
                                FileMode.Create
                            )
                    )
                    {
                        await ProfilePhotoFile
                            .CopyToAsync(stream);
                    }

                    newPhotoRelativePath =
                        $"/uploads/patients/{fileName}";
                }

                /*
                 * ================================
                 * تحديث الحقول الموجودة في الصفحة فقط
                 * ================================
                 */
                existingPatient.FirstName =
                    patient.FirstName;

                existingPatient.SecondName =
                    patient.SecondName;

                existingPatient.ThirdName =
                    patient.ThirdName;

                existingPatient.FourthName =
                    patient.FourthName;

                existingPatient.NationalID_PassportNumber =
                    patient.NationalID_PassportNumber?
                        .Trim();

                existingPatient.Nationality =
                    patient.Nationality;

                existingPatient.Gender =
                    patient.Gender;

                existingPatient.DateOfBirth =
                    patient.DateOfBirth;

                existingPatient.PhoneNumber =
                    patient.PhoneNumber;

                existingPatient.Address =
                    patient.Address;

                existingPatient.FatherName =
                    patient.FatherName;

                existingPatient.FatherPhone =
                    patient.FatherPhone;

                existingPatient.MotherName =
                    patient.MotherName;

                existingPatient.MotherPhone =
                    patient.MotherPhone;

                /*
                 * ================================
                 * Profile Photo
                 * ================================
                 *
                 * 1- RemovePhoto = true:
                 *    امسح مسار الصورة من السجل.
                 *
                 * 2- تم رفع صورة جديدة:
                 *    استبدل المسار بالصورة الجديدة.
                 *
                 * 3- لا حذف ولا رفع:
                 *    اترك الصورة القديمة كما هي.
                 */
                if (removePhotoRequested)
                {
                    existingPatient.ProfilePhotoPath =
                        null;
                }
                else if (
                    !string.IsNullOrWhiteSpace(
                        newPhotoRelativePath
                    )
                )
                {
                    existingPatient.ProfilePhotoPath =
                        newPhotoRelativePath;
                }

                /*
                 * SaveChanges واحد فقط.
                 *
                 * لا يتم هنا تعديل:
                 * - AppointmentDate
                 * - AppointmentTime
                 * - Appointments table
                 * - StatusID
                 * - PatientStatus
                 * - AttendanceStatus
                 * - أي بيانات أخرى للمريض
                 */
                await _context.SaveChangesAsync();

                /*
                 * بعد نجاح الحفظ فقط نحذف الصورة القديمة
                 * إذا طلب المستخدم حذفها أو استبدالها.
                 */
                bool oldPhotoShouldBeDeleted =
                    !string.IsNullOrWhiteSpace(
                        oldPhotoPath
                    )
                    &&
                    (
                        removePhotoRequested
                        ||
                        !string.IsNullOrWhiteSpace(
                            newPhotoRelativePath
                        )
                    )
                    &&
                    !string.Equals(
                        oldPhotoPath,
                        newPhotoRelativePath,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (oldPhotoShouldBeDeleted)
                {
                    try
                    {
                        string oldFilePath =
                            Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot",
                                oldPhotoPath!
                                    .TrimStart('/')
                                    .Replace(
                                        '/',
                                        Path.DirectorySeparatorChar
                                    )
                            );

                        if (
                            System.IO.File.Exists(
                                oldFilePath
                            )
                        )
                        {
                            System.IO.File.Delete(
                                oldFilePath
                            );
                        }
                    }
                    catch (Exception fileDeleteEx)
                    {
                        /*
                         * لا نفشل عملية تعديل بيانات المريض
                         * فقط لأن حذف الملف القديم فشل.
                         */
                        Console.WriteLine(
                            "Old patient photo delete warning: "
                            + fileDeleteEx
                        );
                    }
                }

                TempData["Success"] =
                    "Patient information updated successfully.";

                /*
                 * نحافظ على نفس وجهة الرجوع الموجودة في الكنترولر السابق.
                 */
                return RedirectToAction(
                    "Index",
                    "Home"
                );
            }
            catch (DbUpdateConcurrencyException ex)
            {
                /*
                 * إذا فشل حفظ قاعدة البيانات والصورة الجديدة
                 * كانت قد تم إنشاؤها، نحذفها حتى لا يبقى ملف يتيم.
                 */
                if (
                    !string.IsNullOrWhiteSpace(
                        newPhotoFullPath
                    )
                    && System.IO.File.Exists(
                        newPhotoFullPath
                    )
                )
                {
                    try
                    {
                        System.IO.File.Delete(
                            newPhotoFullPath
                        );
                    }
                    catch
                    {
                        // لا نغطي خطأ الـ DB بخطأ حذف ملف.
                    }
                }

                if (!PatientExists(id))
                {
                    return NotFound();
                }

                Console.WriteLine(
                    "Patient edit concurrency error: "
                    + ex
                );

                ModelState.AddModelError(
                    string.Empty,
                    "The patient record was modified by another user. "
                    + "Please refresh the page and try again."
                );

                patient.ProfilePhotoPath =
                    oldPhotoPath;

                ViewBag.IsVisitClosed =
                    latestVisit != null
                    && latestVisit.IsClosed;

                ViewBag.IsAdmin =
                    isAdmin;

                ViewBag.IsReadOnly =
                    false;

                return View(patient);
            }
            catch (DbUpdateException ex)
            {
                if (
                    !string.IsNullOrWhiteSpace(
                        newPhotoFullPath
                    )
                    && System.IO.File.Exists(
                        newPhotoFullPath
                    )
                )
                {
                    try
                    {
                        System.IO.File.Delete(
                            newPhotoFullPath
                        );
                    }
                    catch
                    {
                        // لا نغطي خطأ الـ DB بخطأ حذف ملف.
                    }
                }

                string databaseError =
                    ex.InnerException?.Message
                    ?? ex.Message;

                Console.WriteLine(
                    "Patient edit database error: "
                    + ex
                );

                ModelState.AddModelError(
                    string.Empty,
                    "Database error while updating the patient information: "
                    + databaseError
                );

                patient.ProfilePhotoPath =
                    oldPhotoPath;

                ViewBag.IsVisitClosed =
                    latestVisit != null
                    && latestVisit.IsClosed;

                ViewBag.IsAdmin =
                    isAdmin;

                ViewBag.IsReadOnly =
                    false;

                return View(patient);
            }
            catch (Exception ex)
            {
                if (
                    !string.IsNullOrWhiteSpace(
                        newPhotoFullPath
                    )
                    && System.IO.File.Exists(
                        newPhotoFullPath
                    )
                )
                {
                    try
                    {
                        System.IO.File.Delete(
                            newPhotoFullPath
                        );
                    }
                    catch
                    {
                        // لا نغطي الخطأ الأصلي بخطأ حذف ملف.
                    }
                }

                Console.WriteLine(
                    "Patient edit error: "
                    + ex
                );

                ModelState.AddModelError(
                    string.Empty,
                    "An error occurred while updating the patient: "
                    + ex.Message
                );

                patient.ProfilePhotoPath =
                    oldPhotoPath;

                ViewBag.IsVisitClosed =
                    latestVisit != null
                    && latestVisit.IsClosed;

                ViewBag.IsAdmin =
                    isAdmin;

                ViewBag.IsReadOnly =
                    false;

                return View(patient);
            }
        }

        // ─── Delete ───────────────────────────────────────────────────────────────
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var patient = await _context.Patients
                .Include(p => p.Status)
                .FirstOrDefaultAsync(m => m.PatientID == id);
            if (patient == null) return NotFound();
            return View(patient);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private static string NormalizeAppointmentTime(string? appointmentTime)
        {
            string value =
                (appointmentTime ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            return value == "PM"
                || value == "13:00"
                || value == "13:00:00"
                || value == "PM|13:00"
                    ? "13:00"
                    : "09:00";
        }

        private bool PatientExists(int id) =>
            _context.Patients.Any(e => e.PatientID == id);

        // ─── API Endpoints ────────────────────────────────────────────────────────
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> CheckSlot(
            string date,
            string time,
            int? excludePatientId = null)
        {
            const int maxCapacity = 10;

            if (
                !DateTime.TryParse(date, out DateTime parsedDate)
                || string.IsNullOrWhiteSpace(time)
            )
            {
                return Json(new
                {
                    available = false,
                    count = 0,
                    capacity = maxCapacity,
                    message = "بيانات الموعد غير مكتملة."
                });
            }

            if (parsedDate.Date < DateTime.Today)
            {
                return Json(new
                {
                    available = false,
                    count = 0,
                    capacity = maxCapacity,
                    message = "لا يمكن اختيار تاريخ سابق. يرجى اختيار اليوم أو تاريخ لاحق."
                });
            }

            string normalizedTime =
                NormalizeAppointmentTime(time);

            int count = await _context.Patients.CountAsync(patient =>
                patient.StatusID != 6
                && patient.AppointmentDate.Date == parsedDate.Date
                && patient.AppointmentTime == normalizedTime
                && (
                    !excludePatientId.HasValue
                    || patient.PatientID != excludePatientId.Value
                ));

            return Json(new
            {
                available = count < maxCapacity,
                count,
                capacity = maxCapacity,
                message = count >= maxCapacity
                    ? "هذا الموعد ممتلئ (10/10). يرجى اختيار موعد آخر."
                    : string.Empty
            });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> CheckDuplicate(string idNumber, string firstName, string lastName, string dob)
        {
            bool duplicateByID = !string.IsNullOrEmpty(idNumber) &&
                await _context.Patients.AnyAsync(p => p.NationalID_PassportNumber == idNumber);

            bool duplicateByName = false;
            if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName) &&
                DateTime.TryParse(dob, out DateTime parsedDob))
            {
                duplicateByName = await _context.Patients.AnyAsync(p =>
                    p.FirstName == firstName &&
                    p.FourthName == lastName &&
                    p.DateOfBirth.HasValue &&
                    p.DateOfBirth.Value.Year == parsedDob.Year &&
                    p.DateOfBirth.Value.Month == parsedDob.Month &&
                    p.DateOfBirth.Value.Day == parsedDob.Day);
            }

            return Json(new { isDuplicate = duplicateByID || duplicateByName });
        }

        public IActionResult DentalCharting() => View("_DentalCharting");

        [HttpGet]
        public async Task<IActionResult> SearchPatient(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(new List<object>());

            q = q.ToLower().Trim();

            var patients = await _context.Patients
                .Where(p =>
                    (p.FirstName + " " + p.SecondName + " " + p.ThirdName + " " + p.FourthName)
                        .ToLower().Contains(q) ||
                    (p.NationalID_PassportNumber != null &&
                     p.NationalID_PassportNumber.ToLower().Contains(q)))
                .Select(p => new {
                    p.PatientID,
                    FullName = p.FirstName + " " + p.SecondName + " " + p.ThirdName + " " + p.FourthName,
                    p.NationalID_PassportNumber
                })
                .Take(10)
                .ToListAsync();

            return Json(patients);
        }
    }
}