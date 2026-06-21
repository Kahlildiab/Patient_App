using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [DentalCollegeManagementSystem_AAU.Filters.AuthFilter]
    public class PatientPhotosController : Controller
    {
        private readonly AppDbContext _context;

        public PatientPhotosController(AppDbContext context) => _context = context;

        // ──────────────────────────────────────────────────────
        // POST: /PatientPhotos/Upload
        // ──────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int patientId, IFormFile photo, string? notes)
        {
            if (photo == null || photo.Length == 0)
                return Json(new { success = false, message = "No file selected." });

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
            if (!allowedTypes.Contains(photo.ContentType))
                return Json(new { success = false, message = "Only JPG/PNG files are allowed." });

            if (photo.Length > 10 * 1024 * 1024)
                return Json(new { success = false, message = "Maximum file size is 10MB." });

            // ── Save file ──────────────────────────────────────
            var folder = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot", "uploads", "patient-photos");
            Directory.CreateDirectory(folder);

            var fileName = $"{patientId}_photo_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(photo.FileName)}";
            var filePath = Path.Combine(folder, fileName);
            var photoPath = $"/uploads/patient-photos/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
                await photo.CopyToAsync(stream);

            // ── Save to DB ─────────────────────────────────────
            var patientPhoto = new PatientPhoto
            {
                PatientID = patientId,
                PhotoPath = photoPath,
                FileName = photo.FileName,
                Notes = notes ?? string.Empty,
                UploadedDate = DateTime.Now
            };

            _context.PatientPhotos.Add(patientPhoto);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                photoId = patientPhoto.PatientPhotoID,
                photoPath,
                fileName = photo.FileName,
                uploadedDate = patientPhoto.UploadedDate.ToString("dd/MM/yyyy")
            });
        }

        // ──────────────────────────────────────────────────────
        // POST: /PatientPhotos/Delete
        // ──────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int photoId, int patientId)
        {
            var photo = await _context.PatientPhotos.FindAsync(photoId);

            if (photo == null || photo.PatientID != patientId)
                return Json(new { success = false, message = "Photo not found." });

            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot",
                photo.PhotoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            _context.PatientPhotos.Remove(photo);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}