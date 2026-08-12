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

        // الأقسام المسموح بها — مطابقة للـ View
        private static readonly HashSet<string> _allowedSections = new(StringComparer.OrdinalIgnoreCase)
        {
            "right_lateral", "anterior", "left_lateral",
            "upper_arch", "lower_arch", "other"
        };

        private static readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
        };

        private const long MaxPhotoSize = 15 * 1024 * 1024; // 15 MB

        public IntraoralExamsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // تحويل section key إلى label مقروء
        private static string GetSectionLabel(string key) => key switch
        {
            "right_lateral" => "Right Side",
            "anterior" => "Anterior",
            "left_lateral" => "Left Side",
            "upper_arch" => "Upper Jaw",
            "lower_arch" => "Lower Jaw",
            _ => "Other"
        };

        // يعمل Local وبعد Publish حتى لو التطبيق داخل /APP2 أو Virtual Directory
        private string GetWebRootPath()
        {
            return !string.IsNullOrWhiteSpace(_env.WebRootPath)
                ? _env.WebRootPath
                : Path.Combine(_env.ContentRootPath, "wwwroot");
        }

        // يبني URL صحيح للصورة مع PathBase الخاص بالتطبيق
        private string BuildPhotoUrl(string relativePath)
        {
            var cleanPath = (relativePath ?? string.Empty)
                .Replace("\\", "/")
                .TrimStart('/');

            var pathBase = Request.PathBase.Value?.TrimEnd('/') ?? string.Empty;
            return $"{pathBase}/{cleanPath}";
        }

        // تحويل المسار المخزن في DB إلى مسار فعلي داخل wwwroot
        private string GetPhysicalPhotoPath(string photoPath)
        {
            var cleanPath = (photoPath ?? string.Empty)
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            return Path.Combine(GetWebRootPath(), cleanPath);
        }

        // GET: /IntraoralExams/Index?patientId=1
        public async Task<IActionResult> Index(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
                return NotFound();

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

        // POST: Save/update exam by AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAjax(IntraoralExam model)
        {
            ModelState.Remove("Patient");
            ModelState.Remove("Photos");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                return Json(new
                {
                    success = false,
                    message = errors.Count > 0
                        ? string.Join(" | ", errors)
                        : "Invalid data."
                });
            }

            var patientExists = await _context.Patients
                .AnyAsync(p => p.PatientID == model.PatientID);

            if (!patientExists)
                return Json(new { success = false, message = "Patient not found." });

            var existing = await _context.IntraoralExams
                .FirstOrDefaultAsync(e => e.PatientID == model.PatientID);

            if (existing == null)
            {
                model.CreatedDate = DateTime.Now;
                _context.IntraoralExams.Add(model);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    examId = model.IntraoralExamID
                });
            }

            existing.OralHygieneStatus = model.OralHygieneStatus;
            existing.OralMucosa = model.OralMucosa;
            existing.TongueExamination = model.TongueExamination;
            existing.Palate = model.Palate;
            existing.FloorOfMouth = model.FloorOfMouth;
            existing.OcclusionClassification = model.OcclusionClassification;
            existing.OtherFindings = model.OtherFindings;
            existing.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                examId = existing.IntraoralExamID
            });
        }

        // POST: Upload one photo by AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxPhotoSize)]
        public async Task<IActionResult> UploadPhotoAjax(
            int patientId,
            IFormFile photo,
            string photoSection = "other")
        {
            if (photo == null || photo.Length == 0)
                return Json(new { success = false, message = "No photo provided." });

            if (photo.Length > MaxPhotoSize)
                return Json(new { success = false, message = "Photo is too large. Maximum size is 15 MB." });

            if (!_allowedSections.Contains(photoSection))
                photoSection = "other";

            var ext = Path.GetExtension(photo.FileName);
            if (string.IsNullOrWhiteSpace(ext) || !_allowedExtensions.Contains(ext))
                return Json(new { success = false, message = "Invalid file type. Only images are allowed." });

            var patientExists = await _context.Patients
                .AnyAsync(p => p.PatientID == patientId);

            if (!patientExists)
                return Json(new { success = false, message = "Patient not found." });

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

            var existingCount = await _context.IntraoralExamPhotos
                .CountAsync(p => p.IntraoralExamID == exam.IntraoralExamID
                              && p.PhotoSection == photoSection);

            var counter = existingCount + 1;
            var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
            var safeExt = ext.ToLowerInvariant();
            var fileName = $"{patientId}_intraoral_{photoSection}_{counter}_{uniqueSuffix}{safeExt}";

            // الملف يحفظ فعليًا داخل wwwroot/uploads/intraoral
            // والـ DB يخزن مسار نسبي حتى يعمل Local وPublish.
            var folderRelative = Path.Combine("uploads", "intraoral");
            var folderAbsolute = Path.Combine(GetWebRootPath(), folderRelative);
            var fullPath = Path.Combine(folderAbsolute, fileName);

            try
            {
                Directory.CreateDirectory(folderAbsolute);

                await using (var stream = new FileStream(
                    fullPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await photo.CopyToAsync(stream);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new
                {
                    success = false,
                    message = "Server cannot write to wwwroot/uploads/intraoral. Give the IIS Application Pool Modify permission on this folder."
                });
            }
            catch (IOException ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Could not save the photo on the server: " + ex.Message
                });
            }

            var relativePath = $"uploads/intraoral/{fileName}";

            var examPhoto = new IntraoralExamPhoto
            {
                IntraoralExamID = exam.IntraoralExamID,
                PhotoPath = relativePath,
                PhotoSection = photoSection,
                UploadedDate = DateTime.Now
            };

            try
            {
                _context.IntraoralExamPhotos.Add(examPhoto);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // إذا فشل الحفظ في DB نحذف الملف حتى لا يبقى ملف بدون سجل.
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);

                throw;
            }

            var photoUrl = BuildPhotoUrl(relativePath);

            return Json(new
            {
                success = true,
                photoPath = relativePath,
                photoUrl,
                photoId = examPhoto.IntraoralExamPhotoID,
                examId = exam.IntraoralExamID,
                section = photoSection,
                sectionLabel = GetSectionLabel(photoSection),
                uploadedDate = examPhoto.UploadedDate.ToString("dd/MM/yyyy")
            });
        }

        // POST: Delete one photo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhoto(int photoId)
        {
            var photo = await _context.IntraoralExamPhotos.FindAsync(photoId);

            if (photo == null)
                return Json(new { success = false, message = "Photo not found." });

            try
            {
                if (!string.IsNullOrWhiteSpace(photo.PhotoPath))
                {
                    var physicalPath = GetPhysicalPhotoPath(photo.PhotoPath);

                    if (System.IO.File.Exists(physicalPath))
                        System.IO.File.Delete(physicalPath);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new
                {
                    success = false,
                    message = "Server cannot delete this file. Check IIS folder permissions."
                });
            }

            _context.IntraoralExamPhotos.Remove(photo);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: Delete all photos in one section
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSectionPhotos(int examId, string section)
        {
            if (!_allowedSections.Contains(section))
                return Json(new { success = false, message = "Invalid section." });

            var photos = await _context.IntraoralExamPhotos
                .Where(p => p.IntraoralExamID == examId && p.PhotoSection == section)
                .ToListAsync();

            try
            {
                foreach (var photo in photos)
                {
                    if (string.IsNullOrWhiteSpace(photo.PhotoPath))
                        continue;

                    var physicalPath = GetPhysicalPhotoPath(photo.PhotoPath);

                    if (System.IO.File.Exists(physicalPath))
                        System.IO.File.Delete(physicalPath);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new
                {
                    success = false,
                    message = "Server cannot delete files. Check IIS folder permissions."
                });
            }

            _context.IntraoralExamPhotos.RemoveRange(photos);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                deleted = photos.Count
            });
        }
    }
}
