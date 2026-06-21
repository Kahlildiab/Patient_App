using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [DentalCollegeManagementSystem_AAU.Filters.AuthFilter]
    public class RadiographsController : Controller
    {
        private readonly AppDbContext _context;
        public RadiographsController(AppDbContext context) => _context = context;

        // ═══════════════════════════════════════════════════════
        //  POST — رفع راديوغراف (AJAX)
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int patientId, IFormFile photo, string radiographType)
        {
            if (photo == null || photo.Length == 0)
                return Json(new { success = false, message = "No file selected." });

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
            if (!allowedTypes.Contains(photo.ContentType))
                return Json(new { success = false, message = "Only JPG/PNG allowed." });

            if (photo.Length > 10 * 1024 * 1024)
                return Json(new { success = false, message = "Max size is 10MB." });

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "radiographs");
            Directory.CreateDirectory(folder);

            var fileName = $"{patientId}_radiograph_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(photo.FileName)}";
            var filePath = Path.Combine(folder, fileName);
            var photoPath = $"/uploads/radiographs/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
                await photo.CopyToAsync(stream);

            var radiograph = new Radiograph
            {
                PatientID = patientId,
                PhotoPath = photoPath,
                FileName = photo.FileName,
                RadiographType = radiographType ?? "General",
                UploadedDate = DateTime.Now
            };

            _context.Radiographs.Add(radiograph);
            await _context.SaveChangesAsync();

            // ✅ نرجع uploadedDate عشان Photos tab يتحدث بدون ريفريش
            return Json(new
            {
                success = true,
                photoId = radiograph.RadiographID,
                photoPath,
                fileName = photo.FileName,
                uploadedDate = radiograph.UploadedDate.ToString("dd/MM/yyyy")
            });
        }

        // ═══════════════════════════════════════════════════════
        //  POST — حذف راديوغراف (AJAX)
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int photoId, int patientId)
        {
            var radiograph = await _context.Radiographs.FindAsync(photoId);
            if (radiograph != null)
            {
                var filePath = Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot",
                    radiograph.PhotoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                _context.Radiographs.Remove(radiograph);
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }
    }
}