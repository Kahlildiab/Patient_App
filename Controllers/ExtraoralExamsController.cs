using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    public class ExtraoralExamsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        // ─── الأقسام المسموح بها — مطابقة لـ View ───────────────
        private static readonly HashSet<string> _allowedSections = new(StringComparer.OrdinalIgnoreCase)
        {
            "right", "frontal", "left",
            "right_smile", "frontal_smile", "left_smile",
            "other"
        };

        public ExtraoralExamsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ✅ تحويل section key إلى label مقروء
        private static string GetSectionLabel(string key) => key switch
        {
            "right" => "Right Side",
            "frontal" => "Frontal Side",
            "left" => "Left Side",
            "right_smile" => "Right Side Smile",
            "frontal_smile" => "Frontal Side Smile",
            "left_smile" => "Left Side Smile",
            _ => "Other"
        };

        // ═══════════════════════════════════════════════════════
        //  GET — عرض صفحة الفحص الخارجي لمريض معين
        // ═══════════════════════════════════════════════════════
        public async Task<IActionResult> Index(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            var exam = await _context.ExtraoralExams
                .FirstOrDefaultAsync(e => e.PatientID == patientId);

            var photos = exam != null
                ? await _context.ExtraoralExamPhotos
                    .Where(p => p.ExtraoralExamID == exam.ExtraoralExamID)
                    .OrderBy(p => p.UploadedDate)
                    .ToListAsync()
                : new List<ExtraoralExamPhoto>();

            ViewBag.ExtraoralExam = exam;
            ViewBag.ExtraoralPhotos = photos;

            return View(patient);
        }

        // ═══════════════════════════════════════════════════════
        //  POST — حفظ / تحديث بيانات الفحص (AJAX)
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAjax(ExtraoralExam model)
        {
            ModelState.Remove("Patient");
            ModelState.Remove("Photos");

            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data." });

            var existing = await _context.ExtraoralExams
                .FirstOrDefaultAsync(e => e.PatientID == model.PatientID);

            if (existing == null)
            {
                model.CreatedDate = DateTime.Now;
                _context.ExtraoralExams.Add(model);
                await _context.SaveChangesAsync();
                return Json(new { success = true, examId = model.ExtraoralExamID });
            }
            else
            {
                existing.FacialSymmetry = model.FacialSymmetry;
                existing.SkinColor = model.SkinColor;
                existing.FacialProfile = model.FacialProfile;
                existing.TMJExamination = model.TMJExamination;
                existing.LymphNodesPalpation = model.LymphNodesPalpation;
                existing.OtherFindings = model.OtherFindings;
                existing.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true, examId = existing.ExtraoralExamID });
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
            var exam = await _context.ExtraoralExams
                .FirstOrDefaultAsync(e => e.PatientID == patientId);

            if (exam == null)
            {
                exam = new ExtraoralExam
                {
                    PatientID = patientId,
                    CreatedDate = DateTime.Now
                };
                _context.ExtraoralExams.Add(exam);
                await _context.SaveChangesAsync();
            }

            // ── بناء اسم الملف ────────────────────────────────
            var existingCount = await _context.ExtraoralExamPhotos
                .CountAsync(p => p.ExtraoralExamID == exam.ExtraoralExamID
                              && p.PhotoSection == photoSection);

            var counter = existingCount + 1;
            var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];
            var fileName = $"{patientId}_extraoral_{photoSection}_{counter}_{uniqueSuffix}{ext}";

            // ── مسار الحفظ ────────────────────────────────────
            var folderRelative = Path.Combine("uploads", "extraoral");
            var folderAbsolute = Path.Combine(_env.WebRootPath, folderRelative);
            Directory.CreateDirectory(folderAbsolute);

            var fullPath = Path.Combine(folderAbsolute, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
                await photo.CopyToAsync(stream);

            var photoPath = $"/{folderRelative.Replace("\\", "/")}/{fileName}";

            // ── حفظ السجل في قاعدة البيانات ──────────────────
            var examPhoto = new ExtraoralExamPhoto
            {
                ExtraoralExamID = exam.ExtraoralExamID,
                PhotoPath = photoPath,
                PhotoSection = photoSection,
                UploadedDate = DateTime.Now
            };

            _context.ExtraoralExamPhotos.Add(examPhoto);
            await _context.SaveChangesAsync();

            // ✅ نرجع sectionLabel و uploadedDate عشان Photos tab يتحدث بدون ريفريش
            return Json(new
            {
                success = true,
                photoPath = photoPath,
                photoId = examPhoto.ExtraoralExamPhotoID,
                examId = exam.ExtraoralExamID,
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
            var photo = await _context.ExtraoralExamPhotos.FindAsync(photoId);

            if (photo == null)
                return Json(new { success = false, message = "Photo not found." });

            // حذف الملف من الـ wwwroot
            if (!string.IsNullOrEmpty(photo.PhotoPath))
            {
                var physicalPath = Path.Combine(_env.WebRootPath, photo.PhotoPath.TrimStart('/'));
                if (System.IO.File.Exists(physicalPath))
                    System.IO.File.Delete(physicalPath);
            }

            // حذف السجل من قاعدة البيانات
            _context.ExtraoralExamPhotos.Remove(photo);
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

            var photos = await _context.ExtraoralExamPhotos
                .Where(p => p.ExtraoralExamID == examId && p.PhotoSection == section)
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

            _context.ExtraoralExamPhotos.RemoveRange(photos);
            await _context.SaveChangesAsync();

            return Json(new { success = true, deleted = photos.Count });
        }
    }
}