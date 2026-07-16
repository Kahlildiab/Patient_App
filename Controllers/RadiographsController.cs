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

        private static readonly string[] AllowedContentTypes =
        {
            "image/jpeg",
            "image/png",
            "image/jpg"
        };

        private static readonly string[] AllowedRadiographTypes =
        {
            "Periapical",
            "Bitewing",
            "Panoramic",
            "Occlusal",
            "Cephalometric"
        };

        private static readonly HashSet<string> PeriapicalSlots =
            new(
                new[]
                {
                    "UR_MOLAR_PA",
                    "UR_PREMOLAR_PA",
                    "UR_ANTERIOR_PA",
                    "UPPER_CENTRAL_PA",
                    "UL_ANTERIOR_PA",
                    "UL_PREMOLAR_PA",
                    "UL_MOLAR_PA",
                    "LR_MOLAR_PA",
                    "LR_PREMOLAR_PA",
                    "LR_ANTERIOR_PA",
                    "LOWER_CENTRAL_PA",
                    "LL_ANTERIOR_PA",
                    "LL_PREMOLAR_PA",
                    "LL_MOLAR_PA"
                },
                StringComparer.OrdinalIgnoreCase
            );

        private static readonly HashSet<string> BitewingSlots =
            new(
                new[]
                {
                    "RIGHT_MOLAR_BW",
                    "RIGHT_PREMOLAR_BW",
                    "LEFT_PREMOLAR_BW",
                    "LEFT_MOLAR_BW"
                },
                StringComparer.OrdinalIgnoreCase
            );

        private static readonly Dictionary<string, string> SlotDisplayNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["UR_MOLAR_PA"] = "UR Molar Periapical",
                ["UR_PREMOLAR_PA"] = "UR Premolar Periapical",
                ["UR_ANTERIOR_PA"] = "UR Anterior Periapical",
                ["UPPER_CENTRAL_PA"] = "Upper Central Periapical",
                ["UL_ANTERIOR_PA"] = "UL Anterior Periapical",
                ["UL_PREMOLAR_PA"] = "UL Premolar Periapical",
                ["UL_MOLAR_PA"] = "UL Molar Periapical",

                ["LR_MOLAR_PA"] = "LR Molar Periapical",
                ["LR_PREMOLAR_PA"] = "LR Premolar Periapical",
                ["LR_ANTERIOR_PA"] = "LR Anterior Periapical",
                ["LOWER_CENTRAL_PA"] = "Lower Central Periapical",
                ["LL_ANTERIOR_PA"] = "LL Anterior Periapical",
                ["LL_PREMOLAR_PA"] = "LL Premolar Periapical",
                ["LL_MOLAR_PA"] = "LL Molar Periapical",

                ["RIGHT_MOLAR_BW"] = "Right Molar Bitewing",
                ["RIGHT_PREMOLAR_BW"] = "Right Premolar Bitewing",
                ["LEFT_PREMOLAR_BW"] = "Left Premolar Bitewing",
                ["LEFT_MOLAR_BW"] = "Left Molar Bitewing"
            };

        public RadiographsController(AppDbContext context)
        {
            _context = context;
        }

        /*
         * Periapical and Bitewing use template slots.
         * No database migration is required.
         *
         * The stored value is:
         * Periapical|UR_MOLAR_PA
         * Bitewing|RIGHT_MOLAR_BW
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(
            int patientId,
            IFormFile photo,
            string radiographType,
            string? templateSlot)
        {
            if (photo == null || photo.Length == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "No file selected."
                });
            }

            bool patientExists =
                await _context.Patients.AnyAsync(
                    p => p.PatientID == patientId
                );

            if (!patientExists)
            {
                return Json(new
                {
                    success = false,
                    message = "Patient was not found."
                });
            }

            if (!AllowedContentTypes.Contains(
                    photo.ContentType,
                    StringComparer.OrdinalIgnoreCase))
            {
                return Json(new
                {
                    success = false,
                    message = "Only JPG or PNG files are supported."
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

            string? normalizedType =
                AllowedRadiographTypes.FirstOrDefault(
                    t => string.Equals(
                        t,
                        radiographType?.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    )
                );

            if (string.IsNullOrWhiteSpace(normalizedType))
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid radiograph type."
                });
            }

            bool usesTemplate =
                normalizedType == "Periapical"
                ||
                normalizedType == "Bitewing";

            string normalizedSlot =
                templateSlot?.Trim()
                ?? string.Empty;

            if (usesTemplate)
            {
                bool validSlot =
                    normalizedType == "Periapical"
                        ? PeriapicalSlots.Contains(normalizedSlot)
                        : BitewingSlots.Contains(normalizedSlot);

                if (!validSlot)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid radiograph template position."
                    });
                }
            }
            else
            {
                normalizedSlot = string.Empty;
            }

            string storedType =
                usesTemplate
                    ? $"{normalizedType}|{normalizedSlot}"
                    : normalizedType;

            string uploadsFolder =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "radiographs"
                );

            Directory.CreateDirectory(uploadsFolder);

            string extension =
                Path.GetExtension(photo.FileName);

            string savedFileName =
                $"{patientId}_radiograph_{Guid.NewGuid():N}{extension}";

            string filePath =
                Path.Combine(uploadsFolder, savedFileName);

            string photoPath =
                $"/uploads/radiographs/{savedFileName}";

            await using (var stream =
                new FileStream(filePath, FileMode.CreateNew))
            {
                await photo.CopyToAsync(stream);
            }

            int? replacedPhotoId = null;
            string? oldPhysicalPath = null;

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                /*
                 * One image per Periapical/Bitewing template slot.
                 * Uploading again replaces the previous image.
                 */
                if (usesTemplate)
                {
                    var existing =
                        await _context.Radiographs
                            .Where(r =>
                                r.PatientID == patientId
                                &&
                                r.RadiographType == storedType)
                            .OrderByDescending(r => r.UploadedDate)
                            .FirstOrDefaultAsync();

                    if (existing != null)
                    {
                        replacedPhotoId =
                            existing.RadiographID;

                        oldPhysicalPath =
                            GetPhysicalPath(existing.PhotoPath);

                        _context.Radiographs.Remove(existing);
                    }
                }

                var radiograph =
                    new Radiograph
                    {
                        PatientID = patientId,
                        PhotoPath = photoPath,
                        FileName = photo.FileName,
                        RadiographType = storedType,
                        UploadedDate = DateTime.Now
                    };

                _context.Radiographs.Add(radiograph);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (!string.IsNullOrWhiteSpace(oldPhysicalPath)
                    && System.IO.File.Exists(oldPhysicalPath))
                {
                    System.IO.File.Delete(oldPhysicalPath);
                }

                string displayType =
                    usesTemplate
                        ? SlotDisplayNames.GetValueOrDefault(
                            normalizedSlot,
                            normalizedType)
                        : normalizedType;

                return Json(new
                {
                    success = true,
                    photoId = radiograph.RadiographID,
                    replacedPhotoId,
                    photoPath,
                    fileName = photo.FileName,
                    radiographType = normalizedType,
                    templateSlot = normalizedSlot,
                    displayType,
                    uploadedDate =
                        radiograph.UploadedDate
                            .ToString("dd/MM/yyyy HH:mm")
                });
            }
            catch
            {
                await transaction.RollbackAsync();

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                return Json(new
                {
                    success = false,
                    message = "The radiograph could not be saved."
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int photoId,
            int patientId)
        {
            var radiograph =
                await _context.Radiographs
                    .FirstOrDefaultAsync(r =>
                        r.RadiographID == photoId
                        &&
                        r.PatientID == patientId);

            if (radiograph == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Radiograph was not found."
                });
            }

            string? templateSlot = null;

            string[] parts =
                (radiograph.RadiographType ?? "")
                    .Split('|');

            if (parts.Length == 2)
            {
                templateSlot = parts[1];
            }

            string physicalPath =
                GetPhysicalPath(radiograph.PhotoPath);

            _context.Radiographs.Remove(radiograph);
            await _context.SaveChangesAsync();

            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            return Json(new
            {
                success = true,
                templateSlot
            });
        }

        private string GetPhysicalPath(string relativePath)
        {
            return Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                relativePath
                    .TrimStart('/')
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar
                    )
            );
        }
    }
}
