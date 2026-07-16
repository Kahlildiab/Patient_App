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

            // ✅ يستثني Cancelled
            var nextAppts = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => patientIds.Contains(a.PatientID)
                         && a.AppointmentDate.Date >= today
                         && a.AppointmentStatus != "Cancelled")
                .OrderBy(a => a.AppointmentDate)
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
                int slotCount = await _context.Patients.CountAsync(p =>
                    p.StatusID == 2 &&
                    p.AppointmentDate.Date == patient.AppointmentDate.Date &&
                    p.AppointmentTime == patient.AppointmentTime);

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
                int slotCount = await _context.Patients.CountAsync(p =>
                    p.StatusID == 2 &&
                    p.AppointmentDate.Date == patient.AppointmentDate.Date &&
                    p.AppointmentTime == patient.AppointmentTime);

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
                    int slotCount = await _context.Patients.CountAsync(p =>
                        p.StatusID == 2 &&
                        p.AppointmentDate.Date == patient.AppointmentDate.Date &&
                        p.AppointmentTime == patient.AppointmentTime);

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
             * الموعد القادم غير الملغي هو المرجع الأساسي.
             * إذا لم يوجد، نستخدم تاريخ الموعد المسجل في Patient.
             */
            var accessAppointment =
                await _context.Appointments
                    .Where(
                        appointment =>
                            appointment.PatientID == id.Value
                            &&
                            appointment.AppointmentStatus
                                != "Cancelled"
                            &&
                            appointment.AppointmentDate.Date
                                >= today
                    )
                    .OrderBy(
                        appointment =>
                            appointment.AppointmentDate
                    )
                    .ThenBy(
                        appointment =>
                            appointment.TimeFrom
                    )
                    .FirstOrDefaultAsync();

            DateTime? detailsAccessDate =
                accessAppointment != null
                    ? accessAppointment.AppointmentDate.Date
                    : (
                        patient.AppointmentDate != default
                            ? patient.AppointmentDate.Date
                            : null
                    );

            if (
                currentUserIsStudent
                &&
                (
                    !detailsAccessDate.HasValue
                    ||
                    today < detailsAccessDate.Value
                )
            )
            {
                TempData["Error"] =
                    detailsAccessDate.HasValue
                        ? "Patient details can be opened starting from "
                          + $"the appointment date: "
                          + $"{detailsAccessDate.Value:yyyy-MM-dd}."
                        : "Patient details cannot be opened because "
                          + "there is no valid appointment.";

                return RedirectToAction(
                    nameof(Index)
                );
            }

            /*
             * أول مرة يتم فتح ملف المريض:
             * أنشئ زيارة أولى مفتوحة تلقائياً إذا لم يكن للمريض أي زيارة سابقة.
             */
            bool hasAnyVisit = await _context.Visits
                .AnyAsync(v => v.PatientID == id.Value);

            if (!hasAnyVisit)
            {
                string appointmentTimeValue =
                    patient.AppointmentTime?.Trim().ToUpperInvariant()
                    ?? string.Empty;

                string? appointmentPeriod = null;

                if (accessAppointment != null)
                {
                    appointmentPeriod =
                        accessAppointment.TimeFrom.Hours >= 12
                            ? "PM"
                            : "AM";
                }
                else if (appointmentTimeValue.Contains("AM"))
                {
                    appointmentPeriod = "AM";
                }
                else if (appointmentTimeValue.Contains("PM"))
                {
                    appointmentPeriod = "PM";
                }
                else if (
                    TimeSpan.TryParse(
                        patient.AppointmentTime,
                        out TimeSpan parsedTime
                    )
                )
                {
                    appointmentPeriod =
                        parsedTime.Hours >= 12
                            ? "PM"
                            : "AM";
                }
                else if (
                    DateTime.TryParse(
                        patient.AppointmentTime,
                        out DateTime parsedDateTime
                    )
                )
                {
                    appointmentPeriod =
                        parsedDateTime.Hour >= 12
                            ? "PM"
                            : "AM";
                }

                /*
                 * أول زيارة تبدأ Pending دائماً، مهما كان دور
                 * المستخدم الذي فتح ملف المريض.
                 *
                 * لا يتم تحديد Case Complexity هنا؛ بل يتم تحديدها
                 * عند اعتماد الزيارة من صفحة Admin Approvals.
                 */
                var firstVisit = new Visit
                {
                    PatientID = id.Value,

                    // الزيارة ترتبط بتاريخ الموعد الصحيح.
                    VisitDate =
                        detailsAccessDate
                        ?? DateTime.Today,

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

                _context.Visits.Add(firstVisit);
                await _context.SaveChangesAsync();
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
                        ? "13:00"
                        : "09:00";
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
                        ? "13:00"
                        : "09:00";
            }

            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("PatientID,FirstName,SecondName,ThirdName,FourthName,NationalID_PassportNumber,Nationality,Gender,DateOfBirth,PhoneNumber,Address,FatherName,FatherPhone,MotherName,MotherPhone,AppointmentDate,AppointmentTime,ProfilePhotoPath")]
            Patient patient,
            IFormFile? ProfilePhotoFile,
            string? RemovePhoto)
        {
            if (id != patient.PatientID)
            {
                return NotFound();
            }

            var latestVisit =
                await _context.Visits
                    .Where(
                        v => v.PatientID == id
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

            if (
                latestVisit != null
                &&
                latestVisit.IsClosed
                &&
                !isAdmin
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

            var existingPatient =
                await _context.Patients
                    .FirstOrDefaultAsync(
                        p => p.PatientID == id
                    );

            if (existingPatient == null)
            {
                return NotFound();
            }

            string submittedTime =
                (
                    patient.AppointmentTime
                    ??
                    string.Empty
                )
                .Trim()
                .ToUpperInvariant();

            bool isEveningAppointment =
                submittedTime == "PM"
                ||
                submittedTime == "13:00"
                ||
                submittedTime == "PM|13:00";

            string normalizedAppointmentTime =
                isEveningAppointment
                    ? "13:00"
                    : "09:00";

            TimeSpan newTimeFrom =
                isEveningAppointment
                    ? new TimeSpan(13, 0, 0)
                    : new TimeSpan(9, 0, 0);

            TimeSpan newTimeTo =
                newTimeFrom.Add(
                    TimeSpan.FromHours(2)
                );

            bool appointmentDateIsValid =
                patient.AppointmentDate != default
                &&
                patient.AppointmentDate.Year >= 2000;

            if (!appointmentDateIsValid)
            {
                ModelState.AddModelError(
                    "AppointmentDate",
                    "Please select a valid appointment date."
                );
            }
            else if (
                patient.AppointmentDate.Date
                <
                DateTime.Today
            )
            {
                ModelState.AddModelError(
                    "AppointmentDate",
                    "You cannot select a past appointment date. Please choose today or a future date."
                );
            }

            /*
             * نمنع تجاوز سعة 10 مرضى في نفس الموعد.
             * لا يتم تنفيذ فحص السعة إذا كان التاريخ غير صالح
             * أو كان أقدم من تاريخ اليوم.
             *
             * نستثني المريض الحالي حتى لا يُحسب على نفسه
             * عند حفظ نفس الموعد من جديد.
             */
            if (
                appointmentDateIsValid
                &&
                patient.AppointmentDate.Date
                    >= DateTime.Today
            )
            {
                int slotCount =
                    await _context.Appointments
                        .CountAsync(
                            a =>
                                a.PatientID != id
                                &&
                                a.AppointmentStatus == "Scheduled"
                                &&
                                a.AppointmentDate.Date
                                    == patient.AppointmentDate.Date
                                &&
                                a.TimeFrom == newTimeFrom
                        );

                if (slotCount >= 10)
                {
                    ModelState.AddModelError(
                        "AppointmentTime",
                        $"The selected appointment on {patient.AppointmentDate:yyyy-MM-dd} at {normalizedAppointmentTime} is full (10/10)."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                patient.AppointmentTime =
                    normalizedAppointmentTime;

                return View(patient);
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                /*
                 * تحديث بيانات المريض يدوياً حتى لا يتم مسح
                 * StatusID أو PatientStatus أو AttendanceStatus
                 * أو أي أعمدة غير موجودة في نموذج Edit.
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
                    patient.NationalID_PassportNumber;

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
                 * تحديث نسخة الموعد الموجودة في Patient
                 * حتى تبقى الفلاتر والشاشات القديمة متوافقة.
                 */
                existingPatient.AppointmentDate =
                    patient.AppointmentDate.Date;

                existingPatient.AppointmentTime =
                    normalizedAppointmentTime;

                /*
                 * تحديث الصورة من دون فقدان الصورة القديمة
                 * إذا لم يرفع المستخدم ملفاً جديداً.
                 */
                if (RemovePhoto == "true")
                {
                    if (
                        !string.IsNullOrWhiteSpace(
                            existingPatient.ProfilePhotoPath
                        )
                    )
                    {
                        string oldFilePath =
                            Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot",
                                existingPatient
                                    .ProfilePhotoPath
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

                    existingPatient.ProfilePhotoPath =
                        null;
                }
                else if (
                    ProfilePhotoFile != null
                    &&
                    ProfilePhotoFile.Length > 0
                )
                {
                    string[] allowedTypes =
                    {
                        "image/jpeg",
                        "image/png",
                        "image/jpg"
                    };

                    if (
                        !allowedTypes.Contains(
                            ProfilePhotoFile.ContentType
                        )
                    )
                    {
                        ModelState.AddModelError(
                            "ProfilePhotoFile",
                            "Only JPG or PNG files are supported."
                        );

                        await transaction.RollbackAsync();

                        patient.ProfilePhotoPath =
                            existingPatient
                                .ProfilePhotoPath;

                        patient.AppointmentTime =
                            normalizedAppointmentTime;

                        return View(patient);
                    }

                    if (
                        ProfilePhotoFile.Length
                        >
                        2 * 1024 * 1024
                    )
                    {
                        ModelState.AddModelError(
                            "ProfilePhotoFile",
                            "Image size must be less than 2MB."
                        );

                        await transaction.RollbackAsync();

                        patient.ProfilePhotoPath =
                            existingPatient
                                .ProfilePhotoPath;

                        patient.AppointmentTime =
                            normalizedAppointmentTime;

                        return View(patient);
                    }

                    if (
                        !string.IsNullOrWhiteSpace(
                            existingPatient.ProfilePhotoPath
                        )
                    )
                    {
                        string oldFilePath =
                            Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot",
                                existingPatient
                                    .ProfilePhotoPath
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
                        $"{Guid.NewGuid()}{Path.GetExtension(ProfilePhotoFile.FileName)}";

                    string filePath =
                        Path.Combine(
                            uploadsFolder,
                            fileName
                        );

                    await using (
                        var stream =
                            new FileStream(
                                filePath,
                                FileMode.Create
                            )
                    )
                    {
                        await ProfilePhotoFile
                            .CopyToAsync(stream);
                    }

                    existingPatient.ProfilePhotoPath =
                        $"/uploads/patients/{fileName}";
                }

                /*
                 * هذا هو الإصلاح الأساسي:
                 * تحديث الموعد الحقيقي في جدول Appointments.
                 */
                var scheduledAppointment =
                    await _context.Appointments
                        .Where(
                            a =>
                                a.PatientID == id
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
                        .FirstOrDefaultAsync();

                if (scheduledAppointment == null)
                {
                    scheduledAppointment =
                        new Appointment
                        {
                            PatientID =
                                id,

                            ClinicName =
                                "General",

                            AppointmentStatus =
                                "Scheduled",

                            CreatedDate =
                                DateTime.Now
                        };

                    _context.Appointments.Add(
                        scheduledAppointment
                    );
                }

                scheduledAppointment.AppointmentDate =
                    patient.AppointmentDate.Date;

                scheduledAppointment.AppointmentDay =
                    patient.AppointmentDate
                        .DayOfWeek
                        .ToString();

                scheduledAppointment.TimeFrom =
                    newTimeFrom;

                scheduledAppointment.TimeTo =
                    newTimeTo;

                scheduledAppointment.AppointmentStatus =
                    "Scheduled";

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                string formattedTime =
                    DateTime.Today
                        .Add(newTimeFrom)
                        .ToString("hh:mm tt");

                TempData["Success"] =
                    "Patient appointment was updated successfully to "
                    +
                    patient.AppointmentDate
                        .ToString("yyyy-MM-dd")
                    +
                    " at "
                    +
                    formattedTime
                    +
                    ".";

                /*
                 * العودة إلى Home/Index حتى تظهر الرسالة
                 * والموعد المعدل في Next Appointment.
                 */
                return RedirectToAction(
                    "Index",
                    "Home"
                );
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();

                if (!PatientExists(id))
                {
                    return NotFound();
                }

                throw;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
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
            if (
                !DateTime.TryParse(
                    date,
                    out DateTime parsedDate
                )
                ||
                string.IsNullOrWhiteSpace(time)
            )
            {
                return Json(
                    new
                    {
                        available = true,
                        count = 0
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
                        count = 0,
                        message =
                            "You cannot select a past appointment date. Please choose today or a future date."
                    }
                );
            }

            string normalizedTime =
                time
                    .Trim()
                    .ToUpperInvariant();

            TimeSpan selectedTime =
                normalizedTime == "PM"
                ||
                normalizedTime == "13:00"
                ||
                normalizedTime == "PM|13:00"
                    ? new TimeSpan(13, 0, 0)
                    : new TimeSpan(9, 0, 0);

            int count =
                await _context.Appointments
                    .CountAsync(
                        appointment =>
                            appointment
                                .AppointmentStatus
                                == "Scheduled"
                            &&
                            appointment
                                .AppointmentDate
                                .Date
                                == parsedDate.Date
                            &&
                            appointment.TimeFrom
                                == selectedTime
                            &&
                            (
                                !excludePatientId.HasValue
                                ||
                                appointment.PatientID
                                    != excludePatientId.Value
                            )
                    );

            return Json(
                new
                {
                    available = count < 10,
                    count
                }
            );
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