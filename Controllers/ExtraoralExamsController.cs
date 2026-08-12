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

        private static readonly HashSet<string> _allowedSections = new(StringComparer.OrdinalIgnoreCase)
        {
            "right", "frontal", "left",
            "right_smile", "frontal_smile", "left_smile",
            "other"
        };

        private static readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
        };

        private const long MaxPhotoSize = 15 * 1024 * 1024; // 15 MB

        public ExtraoralExamsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

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

        private string GetWebRootPath()
        {
            return !string.IsNullOrWhiteSpace(_env.WebRootPath)
                ? _env.WebRootPath
                : Path.Combine(_env.ContentRootPath, "wwwroot");
        }

        private string BuildPhotoUrl(string relativePath)
        {
            var cleanPath = (relativePath ?? string.Empty)
                .Replace("\\", "/")
                .TrimStart('/');

            var pathBase = Request.PathBase.Value?.TrimEnd('/') ?? string.Empty;
            return $"{pathBase}/{cleanPath}";
        }

        private string GetPhysicalPhotoPath(string photoPath)
        {
            var cleanPath = (photoPath ?? string.Empty)
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            return Path.Combine(GetWebRootPath(), cleanPath);
        }

        // GET: /ExtraoralExams/Index?patientId=1
        public async Task<IActionResult> Index(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
                return NotFound();

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

        // POST: Save/update exam by AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAjax(ExtraoralExam model)
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

            var existing = await _context.ExtraoralExams
                .FirstOrDefaultAsync(e => e.PatientID == model.PatientID);

            if (existing == null)
            {
                model.CreatedDate = DateTime.Now;
                _context.ExtraoralExams.Add(model);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    examId = model.ExtraoralExamID
                });
            }

            existing.FacialSymmetry = model.FacialSymmetry;
            existing.SkinColor = model.SkinColor;
            existing.FacialProfile = model.FacialProfile;
            existing.TMJExamination = model.TMJExamination;
            existing.LymphNodesPalpation = model.LymphNodesPalpation;
            existing.OtherFindings = model.OtherFindings;
            existing.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                examId = existing.ExtraoralExamID
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

            var existingCount = await _context.ExtraoralExamPhotos
                .CountAsync(p => p.ExtraoralExamID == exam.ExtraoralExamID
                              && p.PhotoSection == photoSection);

            var counter = existingCount + 1;
            var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
            var safeExt = ext.ToLowerInvariant();
            var fileName = $"{patientId}_extraoral_{photoSection}_{counter}_{uniqueSuffix}{safeExt}";

            // IMPORTANT:
            // File is saved physically under wwwroot/uploads/extraoral
            // DB stores a relative path so it works locally and after Publish.
            var folderRelative = Path.Combine("uploads", "extraoral");
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
                    message = "Server cannot write to wwwroot/uploads/extraoral. Give the IIS Application Pool Modify permission on this folder."
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

            var relativePath = $"uploads/extraoral/{fileName}";

            var examPhoto = new ExtraoralExamPhoto
            {
                ExtraoralExamID = exam.ExtraoralExamID,
                PhotoPath = relativePath,
                PhotoSection = photoSection,
                UploadedDate = DateTime.Now
            };

            try
            {
                _context.ExtraoralExamPhotos.Add(examPhoto);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // If DB save fails, remove the physical file so we do not leave orphan files.
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
                photoId = examPhoto.ExtraoralExamPhotoID,
                examId = exam.ExtraoralExamID,
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
            var photo = await _context.ExtraoralExamPhotos.FindAsync(photoId);

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

            _context.ExtraoralExamPhotos.Remove(photo);
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

            var photos = await _context.ExtraoralExamPhotos
                .Where(p => p.ExtraoralExamID == examId && p.PhotoSection == section)
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

            _context.ExtraoralExamPhotos.RemoveRange(photos);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                deleted = photos.Count
            });
        }
    }
}
