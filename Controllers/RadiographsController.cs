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
        private readonly IWebHostEnvironment _env;

        private const long MaxPhotoSize = 10 * 1024 * 1024; // 10 MB

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png"
            };

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
            "Cephalometric",
            "CBCT"
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

        public RadiographsController(
            AppDbContext context,
            IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // يعمل Local وبعد Publish حتى لو التطبيق داخل /APP2 أو Virtual Directory.
        private string GetWebRootPath()
        {
            return !string.IsNullOrWhiteSpace(_env.WebRootPath)
                ? _env.WebRootPath
                : Path.Combine(_env.ContentRootPath, "wwwroot");
        }

        // يبني URL صحيح للصورة مع PathBase الخاص بالتطبيق.
        private string BuildPhotoUrl(string relativePath)
        {
            string cleanPath = (relativePath ?? string.Empty)
                .Replace("\\", "/")
                .TrimStart('/');

            string pathBase =
                Request.PathBase.Value?.TrimEnd('/')
                ?? string.Empty;

            return $"{pathBase}/{cleanPath}";
        }

        // يدعم المسارات القديمة المخزنة مثل /uploads/... والجديدة uploads/...
        private string GetPhysicalPath(string relativePath)
        {
            string cleanPath = (relativePath ?? string.Empty)
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            return Path.Combine(
                GetWebRootPath(),
                cleanPath
            );
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
        [RequestSizeLimit(MaxPhotoSize)]
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

            if (photo.Length > MaxPhotoSize)
            {
                return Json(new
                {
                    success = false,
                    message = "Maximum file size is 10MB."
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

            string extension =
                Path.GetExtension(photo.FileName)
                    .ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(extension)
                || !AllowedExtensions.Contains(extension))
            {
                return Json(new
                {
                    success = false,
                    message = "Only JPG or PNG files are supported."
                });
            }

            if (!string.IsNullOrWhiteSpace(photo.ContentType)
                && !AllowedContentTypes.Contains(
                    photo.ContentType,
                    StringComparer.OrdinalIgnoreCase))
            {
                return Json(new
                {
                    success = false,
                    message = "Only JPG or PNG files are supported."
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
                || normalizedType == "Bitewing";

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
                    GetWebRootPath(),
                    "uploads",
                    "radiographs"
                );

            string savedFileName =
                $"{patientId}_radiograph_{Guid.NewGuid():N}{extension}";

            string filePath =
                Path.Combine(
                    uploadsFolder,
                    savedFileName
                );

            // نخزن في DB مسار نسبي حتى يعمل Local وPublish.
            string photoPath =
                $"uploads/radiographs/{savedFileName}";

            try
            {
                Directory.CreateDirectory(uploadsFolder);

                await using var stream =
                    new FileStream(
                        filePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None
                    );

                await photo.CopyToAsync(stream);
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new
                {
                    success = false,
                    message = "Server cannot write to wwwroot/uploads/radiographs. Give the IIS Application Pool Modify permission on this folder."
                });
            }
            catch (IOException ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Could not save the radiograph on the server: " + ex.Message
                });
            }

            int? replacedPhotoId = null;
            string? oldPhysicalPath = null;

            /*
             * مهم:
             * لا نستخدم BeginTransactionAsync هنا لأن المشروع يستخدم
             * EnableRetryOnFailure / SqlServerRetryingExecutionStrategy.
             *
             * SaveChangesAsync ينفذ حذف الصورة القديمة من قاعدة البيانات
             * وإضافة الصورة الجديدة داخل Transaction تلقائية واحدة من EF Core.
             * لذلك لا نحتاج Transaction يدوية، وبهذا نتجنب الخطأ:
             * "SqlServerRetryingExecutionStrategy does not support user-initiated transactions".
             */
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

                /*
                 * EF Core creates the required database transaction automatically.
                 * This operation is compatible with SqlServerRetryingExecutionStrategy.
                 */
                await _context.SaveChangesAsync();

                // حذف الصورة القديمة من القرص فقط بعد نجاح الحفظ في قاعدة البيانات.
                if (!string.IsNullOrWhiteSpace(oldPhysicalPath)
                    && System.IO.File.Exists(oldPhysicalPath))
                {
                    try
                    {
                        System.IO.File.Delete(oldPhysicalPath);
                    }
                    catch
                    {
                        // لا نفشل الحفظ الجديد إذا تعذر حذف الملف القديم فقط.
                    }
                }

                string displayType =
                    usesTemplate
                        ? SlotDisplayNames.GetValueOrDefault(
                            normalizedSlot,
                            normalizedType)
                        : normalizedType;

                string photoUrl =
                    BuildPhotoUrl(photoPath);

                return Json(new
                {
                    success = true,
                    photoId = radiograph.RadiographID,
                    replacedPhotoId,
                    photoPath,
                    photoUrl,
                    fileName = photo.FileName,
                    radiographType = normalizedType,
                    templateSlot = normalizedSlot,
                    displayType,
                    uploadedDate =
                        radiograph.UploadedDate
                            .ToString("dd/MM/yyyy HH:mm")
                });
            }
            catch (Exception ex)
            {
                /*
                 * إذا فشل الحفظ في قاعدة البيانات نحذف الملف الجديد
                 * الذي تم رفعه حتى لا يبقى ملف بدون سجل في DB.
                 */
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch
                    {
                    }
                }

                return Json(new
                {
                    success = false,
                    message = "The radiograph could not be saved: " + ex.Message
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

            try
            {
                _context.Radiographs.Remove(radiograph);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Could not delete the radiograph record: " + ex.Message
                });
            }

            if (System.IO.File.Exists(physicalPath))
            {
                try
                {
                    System.IO.File.Delete(physicalPath);
                }
                catch (UnauthorizedAccessException)
                {
                    // السجل انحذف من DB، لذلك نرجع نجاح مع تحذير فقط.
                    return Json(new
                    {
                        success = true,
                        templateSlot,
                        warning = "Radiograph record was deleted, but IIS could not delete the physical file. Check Modify permission on wwwroot/uploads/radiographs."
                    });
                }
                catch (IOException)
                {
                    // نفس المنطق: لا نرجع السجل للـ DB بسبب ملف فقط.
                }
            }

            return Json(new
            {
                success = true,
                templateSlot
            });
        }
    }
}
