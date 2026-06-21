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

                if (slotCount >= 20)
                {
                    ModelState.AddModelError(string.Empty,
                        $"لا تتوفر أماكن في هذه الفترة ({patient.AppointmentTime}) بتاريخ {patient.AppointmentDate:yyyy-MM-dd}. الطاقة ممتلئة (20/20).");
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

                if (slotCount >= 20)
                {
                    ModelState.AddModelError(string.Empty,
                        $"لا تتوفر أماكن في هذه الفترة ({patient.AppointmentTime}) بتاريخ {patient.AppointmentDate:yyyy-MM-dd}. الطاقة ممتلئة (20/20).");
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

                    if (slotCount >= 20)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"لا تتوفر أماكن في هذه الفترة ({patient.AppointmentTime}) بتاريخ {patient.AppointmentDate:yyyy-MM-dd}. الطاقة ممتلئة (20/20).");
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

            var timeFrom = patient.AppointmentTime == "PM"
                ? new TimeSpan(13, 0, 0)
                : new TimeSpan(8, 0, 0);

            var timeTo = patient.AppointmentTime == "PM"
                ? new TimeSpan(17, 0, 0)
                : new TimeSpan(12, 0, 0);

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
        public async Task<IActionResult> Details(int? id, string activeTab = "overview")
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.Status)
                .FirstOrDefaultAsync(m => m.PatientID == id);

            if (patient == null) return NotFound();

            var templates = await _context.Competency
                .Where(c => c.PatientID == null)
                .OrderBy(c => c.CategoryOrder)
                .ThenBy(c => c.ItemOrder)
                .ToListAsync();

            var patientCompetencies = await _context.Competency
                .Where(c => c.PatientID == id)
                .ToListAsync();

            var medicalHistory = await _context.MedicalHistories.FirstOrDefaultAsync(m => m.PatientID == id);
            var conditions = await _context.Conditions.Where(c => c.PatientID == id).OrderByDescending(c => c.CreatedDate).ToListAsync();
            var medications = await _context.Medications.Where(m => m.PatientID == id).ToListAsync();
            var visits = await _context.Visits.Where(v => v.PatientID == id).OrderByDescending(v => v.VisitDate).ToListAsync();
            var dentalHistory = await _context.DentalHistories.FirstOrDefaultAsync(d => d.PatientID == id);
            var socialHistory = await _context.SocialHistories.FirstOrDefaultAsync(s => s.PatientID == id);
            var extraoralexam = await _context.ExtraoralExams.FirstOrDefaultAsync(s => s.PatientID == id);
            var intraoralexam = await _context.IntraoralExams.FirstOrDefaultAsync(s => s.PatientID == id);
            var notes = await _context.Notes.Where(n => n.PatientId == id).OrderByDescending(n => n.CreatedAt).ToListAsync();
            var treatmentProcedures = await _context.TreatmentProcedures.Where(p => p.PatientId == id).ToListAsync();

            ViewBag.AllCompetencies = templates;
            ViewBag.PatientCompetencies = patientCompetencies;
            ViewBag.MedicalHistory = medicalHistory;
            ViewBag.Conditions = conditions;
            ViewBag.Medications = medications;
            ViewBag.SocialHistory = socialHistory;
            ViewBag.DentalHistory = dentalHistory;
            ViewBag.ExtraoralExam = extraoralexam;
            ViewBag.IntraoralExam = intraoralexam;
            ViewBag.TreatmentProcedures = treatmentProcedures;
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
            ViewBag.ActiveTab = activeTab;

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

            // ✅ يستثني Cancelled
            var nextAppointment = await _context.Appointments
                .Where(a => a.PatientID == id
                         && a.AppointmentDate.Date >= DateTime.Today
                         && a.AppointmentStatus != "Cancelled")
                .OrderBy(a => a.AppointmentDate)
                .FirstOrDefaultAsync();
            ViewBag.NextAppointment = nextAppointment;

            var allAppointments = await _context.Appointments
                .Where(a => a.PatientID == id)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();
            ViewBag.AllAppointments = allAppointments;

            return View(patient);
        }

        // ─── Edit ─────────────────────────────────────────────────────────────────
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();
            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("PatientID,FirstName,SecondName,ThirdName,FourthName,NationalID_PassportNumber,Nationality,Gender,DateOfBirth,PhoneNumber,Address,FatherName,FatherPhone,MotherName,MotherPhone,AppointmentDate,AppointmentTime,ProfilePhotoPath")]
            Patient patient,
            IFormFile? ProfilePhotoFile,
            string? RemovePhoto)
        {
            if (id != patient.PatientID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (RemovePhoto == "true")
                    {
                        if (!string.IsNullOrEmpty(patient.ProfilePhotoPath))
                        {
                            var oldFile = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                                patient.ProfilePhotoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                            if (System.IO.File.Exists(oldFile)) System.IO.File.Delete(oldFile);
                        }
                        patient.ProfilePhotoPath = null;
                    }
                    else if (ProfilePhotoFile != null && ProfilePhotoFile.Length > 0)
                    {
                        var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
                        if (!allowedTypes.Contains(ProfilePhotoFile.ContentType))
                        {
                            ModelState.AddModelError("ProfilePhotoFile", "Only JPG or PNG files are supported.");
                            return View(patient);
                        }
                        if (ProfilePhotoFile.Length > 2 * 1024 * 1024)
                        {
                            ModelState.AddModelError("ProfilePhotoFile", "Image size must be less than 2MB.");
                            return View(patient);
                        }

                        if (!string.IsNullOrEmpty(patient.ProfilePhotoPath))
                        {
                            var oldFile = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                                patient.ProfilePhotoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                            if (System.IO.File.Exists(oldFile)) System.IO.File.Delete(oldFile);
                        }

                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "patients");
                        Directory.CreateDirectory(uploadsFolder);
                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ProfilePhotoFile.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await ProfilePhotoFile.CopyToAsync(stream);
                        patient.ProfilePhotoPath = $"/uploads/patients/{fileName}";
                    }

                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.PatientID)) return NotFound();
                    else throw;
                }

                return RedirectToAction("Details", "Patients", new { id = patient.PatientID });
            }
            return View(patient);
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
        public async Task<IActionResult> CheckSlot(string date, string time)
        {
            if (!DateTime.TryParse(date, out DateTime parsedDate) || string.IsNullOrEmpty(time))
                return Json(new { available = true, count = 0 });

            int count = await _context.Patients.CountAsync(p =>
                p.StatusID == 2 &&
                p.AppointmentDate.Date == parsedDate.Date &&
                p.AppointmentTime == time);

            return Json(new { available = count < 10, count = count });
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