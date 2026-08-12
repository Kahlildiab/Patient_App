using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [DentalCollegeManagementSystem_AAU.Filters.AuthFilter]
    public class PatientPhotosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png"
            };

        private static readonly HashSet<string> AllowedContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg", "image/jpg", "image/png"
            };

        public PatientPhotosController(
            AppDbContext context,
            IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ──────────────────────────────────────────────────────
        // POST: PatientPhotos/Upload
        // Works on Local and after Publish under /APP2
        // ──────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(
            int patientId,
            IFormFile photo,
            string? notes)
        {
            if (photo == null || photo.Length == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "No file selected."
                });
            }

            var patientExists = await _context.Patients
                .AnyAsync(p => p.PatientID == patientId);

            if (!patientExists)
            {
                return Json(new
                {
                    success = false,
                    message = "Patient not found."
                });
            }

            var extension = Path.GetExtension(photo.FileName);

            if (string.IsNullOrWhiteSpace(extension) ||
                !AllowedExtensions.Contains(extension) ||
                !AllowedContentTypes.Contains(photo.ContentType ?? string.Empty))
            {
                return Json(new
                {
                    success = false,
                    message = "Only JPG/PNG files are allowed."
                });
            }

            if (photo.Length > 10 * 1024 * 1024)
            {
                return Json(new
                {
                    success = false,
                    message = "Maximum file size is 10MB."
                });
            }

            if (string.IsNullOrWhiteSpace(_env.WebRootPath))
            {
                return Json(new
                {
                    success = false,
                    message = "WebRootPath is not configured on the server."
                });
            }

            // DB stores a path independent from the application virtual directory.
            var folderRelative = Path.Combine("uploads", "patient-photos");
            var folderAbsolute = Path.Combine(_env.WebRootPath, folderRelative);

            try
            {
                Directory.CreateDirectory(folderAbsolute);
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new
                {
                    success = false,
                    message = "The server does not have permission to write to wwwroot/uploads/patient-photos."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Could not create the upload folder: {ex.Message}"
                });
            }

            var safeExtension = extension.ToLowerInvariant();
            var fileName =
                $"{patientId}_photo_{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{safeExtension}";

            var filePath = Path.Combine(folderAbsolute, fileName);

            // This is what is stored in DB.
            var storedPhotoPath =
                $"/uploads/patient-photos/{fileName}";

            try
            {
                await using var stream = new FileStream(
                    filePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

                await photo.CopyToAsync(stream);
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new
                {
                    success = false,
                    message = "IIS does not have Write/Modify permission on wwwroot/uploads/patient-photos."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"The image could not be saved on the server: {ex.Message}"
                });
            }

            try
            {
                var patientPhoto = new PatientPhoto
                {
                    PatientID = patientId,
                    PhotoPath = storedPhotoPath,
                    FileName = Path.GetFileName(photo.FileName),
                    Notes = notes?.Trim() ?? string.Empty,
                    UploadedDate = DateTime.Now
                };

                _context.PatientPhotos.Add(patientPhoto);
                await _context.SaveChangesAsync();

                // Request.PathBase is empty locally and /APP2 when hosted under that path.
                var browserPhotoUrl =
                    $"{Request.PathBase}{storedPhotoPath}";

                return Json(new
                {
                    success = true,
                    photoId = patientPhoto.PatientPhotoID,
                    photoPath = browserPhotoUrl,
                    storedPhotoPath,
                    fileName = patientPhoto.FileName,
                    notes = patientPhoto.Notes,
                    uploadedDate = patientPhoto.UploadedDate.ToString("dd/MM/yyyy")
                });
            }
            catch (Exception ex)
            {
                // If DB save fails, remove the physical file to avoid orphan files.
                try
                {
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
                catch
                {
                    // Do not mask the original DB error.
                }

                return Json(new
                {
                    success = false,
                    message = $"The photo record could not be saved: {ex.Message}"
                });
            }
        }

        // ──────────────────────────────────────────────────────
        // POST: PatientPhotos/Delete
        // ──────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int photoId,
            int patientId)
        {
            var photo = await _context.PatientPhotos
                .FirstOrDefaultAsync(p =>
                    p.PatientPhotoID == photoId &&
                    p.PatientID == patientId);

            if (photo == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Photo not found."
                });
            }

            string? physicalPath = null;

            if (!string.IsNullOrWhiteSpace(photo.PhotoPath) &&
                !string.IsNullOrWhiteSpace(_env.WebRootPath))
            {
                var relativePath = photo.PhotoPath
                    .TrimStart('/', '\\')
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);

                physicalPath = Path.Combine(
                    _env.WebRootPath,
                    relativePath);
            }

            try
            {
                _context.PatientPhotos.Remove(photo);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(physicalPath) &&
                    System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }

                return Json(new { success = true });
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new
                {
                    success = false,
                    message = "IIS does not have permission to delete the image file."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"The photo could not be deleted: {ex.Message}"
                });
            }
        }
    }
}
