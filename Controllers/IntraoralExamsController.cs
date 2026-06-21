using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    public class IntraoralExamsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        // ─── الأقسام المسموح بها — مطابقة لـ View ───────────────
        private static readonly HashSet<string> _allowedSections = new(StringComparer.OrdinalIgnoreCase)
        {
            "right_lateral", "anterior", "left_lateral",
            "upper_arch", "lower_arch", "other"
        };

        public IntraoralExamsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ✅ تحويل section key إلى label مقروء
        private static string GetSectionLabel(string key) => key switch
        {
            "right_lateral" => "Right Side",
            "anterior" => "Anterior",
            "left_lateral" => "Left Side",
            "upper_arch" => "Upper Jaw",
            "lower_arch" => "Lower Jaw",
            _ => "Other"
        };

        // ═══════════════════════════════════════════════════════
        //  GET — عرض صفحة الفحص الداخلي لمريض معين
        // ═══════════════════════════════════════════════════════
        public async Task<IActionResult> Index(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            var exam = await _context.IntraoralExams
                .FirstOrDefaultAsync(e => e.PatientID == patientId);

            var photos = exam != null
                ? await _context.IntraoralExamPhotos
                    .Where(p => p.IntraoralExamID == exam.IntraoralExamID)
                    .OrderBy(p => p.UploadedDate)
                    .ToListAsync()
                : new List<IntraoralExamPhoto>();

            ViewBag.IntraoralExam = exam;
            ViewBag.IntraoralPhotos = photos;

            return View(patient);
        }

        // ═══════════════════════════════════════════════════════
        //  POST — حفظ / تحديث بيانات الفحص (AJAX)
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAjax(IntraoralExam model)
        {
            ModelState.Remove("Patient");
            ModelState.Remove("Photos");

            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data." });

            var existing = await _context.IntraoralExams
                .FirstOrDefaultAsync(e => e.PatientID == model.PatientID);

            if (existing == null)
            {
                model.CreatedDate = DateTime.Now;
                _context.IntraoralExams.Add(model);
                await _context.SaveChangesAsync();
                return Json(new { success = true, examId = model.IntraoralExamID });
            }
            else
            {
                existing.OralHygieneStatus = model.OralHygieneStatus;
                existing.OralMucosa = model.OralMucosa;
                existing.TongueExamination = model.TongueExamination;
                existing.Palate = model.Palate;
                existing.FloorOfMouth = model.FloorOfMouth;
                existing.OcclusionClassification = model.OcclusionClassification;
                existing.OtherFindings = model.OtherFindings;
                existing.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true, examId = existing.IntraoralExamID });
            }
        }

        // ═══════════════════════════════════════════════════════
        //  POST — رفع صورة واحدة لقسم معين (AJAX)
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhotoAjax(
            int patientId,
            IFormFile photo,
            string photoSection = "other")
        {
            // ── Validation ──────────────────────────────────────
            if (photo == null || photo.Length == 0)
                return Json(new { success = false, message = "No photo provided." });

            if (!_allowedSections.Contains(photoSection))
                photoSection = "other";

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return Json(new { success = false, message = "Invalid file type. Only images are allowed." });

            // ── جلب أو إنشاء الفحص ────────────────────────────
            var exam = await _context.IntraoralExams
                .FirstOrDefaultAsync(e => e.PatientID == patientId);

            if (exam == null)
            {
                exam = new IntraoralExam
                {
                    PatientID = patientId,
                    CreatedDate = DateTime.Now
                };
                _context.IntraoralExams.Add(exam);
                await _context.SaveChangesAsync();
            }

            // ── بناء اسم الملف ────────────────────────────────
            var existingCount = await _context.IntraoralExamPhotos
                .CountAsync(p => p.IntraoralExamID == exam.IntraoralExamID
                              && p.PhotoSection == photoSection);

            var counter = existingCount + 1;
            var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];
            var fileName = $"{patientId}_intraoral_{photoSection}_{counter}_{uniqueSuffix}{ext}";

            // ── مسار الحفظ ────────────────────────────────────
            var folderRelative = Path.Combine("uploads", "intraoral");
            var folderAbsolute = Path.Combine(_env.WebRootPath, folderRelative);
            Directory.CreateDirectory(folderAbsolute);

            var fullPath = Path.Combine(folderAbsolute, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
                await photo.CopyToAsync(stream);

            var photoPath = $"/{folderRelative.Replace("\\", "/")}/{fileName}";

            // ── حفظ السجل في قاعدة البيانات ──────────────────
            var examPhoto = new IntraoralExamPhoto
            {
                IntraoralExamID = exam.IntraoralExamID,
                PhotoPath = photoPath,
                PhotoSection = photoSection,
                UploadedDate = DateTime.Now
            };

            _context.IntraoralExamPhotos.Add(examPhoto);
            await _context.SaveChangesAsync();

            // ✅ نرجع sectionLabel و uploadedDate عشان Photos tab يتحدث بدون ريفريش
            return Json(new
            {
                success = true,
                photoPath = photoPath,
                photoId = examPhoto.IntraoralExamPhotoID,
                examId = exam.IntraoralExamID,
                section = photoSection,
                sectionLabel = GetSectionLabel(photoSection),
                uploadedDate = examPhoto.UploadedDate.ToString("dd/MM/yyyy")
            });
        }

        // ═══════════════════════════════════════════════════════
        //  POST — حذف صورة بالـ photoId (AJAX)
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhoto(int photoId)
        {
            var photo = await _context.IntraoralExamPhotos.FindAsync(photoId);

            if (photo == null)
                return Json(new { success = false, message = "Photo not found." });

            if (!string.IsNullOrEmpty(photo.PhotoPath))
            {
                var physicalPath = Path.Combine(_env.WebRootPath, photo.PhotoPath.TrimStart('/'));
                if (System.IO.File.Exists(physicalPath))
                    System.IO.File.Delete(physicalPath);
            }

            _context.IntraoralExamPhotos.Remove(photo);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ═══════════════════════════════════════════════════════
        //  POST — حذف كل صور قسم معين (AJAX)
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSectionPhotos(int examId, string section)
        {
            if (!_allowedSections.Contains(section))
                return Json(new { success = false, message = "Invalid section." });

            var photos = await _context.IntraoralExamPhotos
                .Where(p => p.IntraoralExamID == examId && p.PhotoSection == section)
                .ToListAsync();

            foreach (var photo in photos)
            {
                if (!string.IsNullOrEmpty(photo.PhotoPath))
                {
                    var physicalPath = Path.Combine(_env.WebRootPath, photo.PhotoPath.TrimStart('/'));
                    if (System.IO.File.Exists(physicalPath))
                        System.IO.File.Delete(physicalPath);
                }
            }

            _context.IntraoralExamPhotos.RemoveRange(photos);
            await _context.SaveChangesAsync();

            return Json(new { success = true, deleted = photos.Count });
        }
    }
}