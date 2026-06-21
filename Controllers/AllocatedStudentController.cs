using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Filters;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [AuthFilter("Admin", "Fulltime Supervisor", "Parttime Supervisor")]

    public class AllocatedStudentController : Controller
    {
        private readonly AppDbContext _db;
        public AllocatedStudentController(AppDbContext db) => _db = db;

        // ── Index ─────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            var userIdStr = HttpContext.Session.GetString("AppUserID");
            int.TryParse(userIdStr, out int userId);

            List<Patient> patients;

            if (userRole == "Student")
            {
                // الطالب يشوف بس المرضى المسندين عليه وحالتهم Allocated
                var assignedPatientIds = await _db.AllocatedStudents
                    .Where(a => a.AppUserId == userId)
                    .Select(a => a.PatientID)
                    .ToListAsync();

                patients = await _db.Patients
                    .Include(p => p.Status)
                    .Where(p => assignedPatientIds.Contains(p.PatientID)
                             && p.PatientStatus == "Allocated")
                    .OrderBy(p => p.FirstName)
                    .ToListAsync();
            }
            else
            {
                // Admin و Supervisor يشوفون كل المرضى
                patients = await _db.Patients
                    .Include(p => p.Status)
                    .OrderBy(p => p.FirstName)
                    .Where(p => p.StatusID != 6)
                    .ToListAsync();
            }

            return View(patients);
        }

        // ── GetPatientDetailsJson ─────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetPatientDetailsJson(int patientId)
        {
            var assignedIds = await _db.AllocatedStudents
                .Where(a => a.PatientID == patientId)
                .Select(a => a.AppUserId)
                .ToListAsync();

            var assigned = await _db.AppUsers
                .Where(u => assignedIds.Contains(u.Id) && u.Status == "Active")
                .Include(u => u.UserType)
                .Select(u => new
                {
                    id = u.Id,
                    nameEn = u.NameEn,
                    userTypeEn = u.UserType != null ? u.UserType.NameEn : "—"
                })
                .ToListAsync();

            var available = await _db.AppUsers
                .Where(u => !assignedIds.Contains(u.Id) && u.Status == "Active")
                .Include(u => u.UserType)
                .Select(u => new
                {
                    id = u.Id,
                    nameEn = u.NameEn,
                    userTypeEn = u.UserType != null ? u.UserType.NameEn : "—"
                })
                .ToListAsync();

            return Json(new { assigned, available });
        }

        // ── GetStatusHistory ──────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetStatusHistory(int patientId)
        {
            var history = await _db.PatientStatusHistories
                .Where(h => h.PatientID == patientId)
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new
                {
                    oldStatus = h.OldStatus,
                    newStatus = h.NewStatus,
                    changedAt = h.ChangedAt.ToString("yyyy-MM-dd  HH:mm")
                })
                .ToListAsync();

            return Json(history);
        }

        // ── Assign ────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Assign(int patientId, int appUserId)
        {
            var exists = await _db.AllocatedStudents
                .AnyAsync(a => a.PatientID == patientId && a.AppUserId == appUserId);

            if (!exists)
            {
                _db.AllocatedStudents.Add(new AllocatedStudent
                {
                    PatientID = patientId,
                    AppUserId = appUserId,
                    AssignedDate = DateTime.Now
                });
                await _db.SaveChangesAsync();

                // ✅ أول طالب يُسند → حالة المريض تصبح Allocated
                var patient = await _db.Patients.FindAsync(patientId);
                if (patient != null && patient.PatientStatus != "Allocated")
                {
                    string oldStatus = string.IsNullOrEmpty(patient.PatientStatus)
                                     ? "Screening" : patient.PatientStatus;

                    patient.PatientStatus = "Allocated";

                    // ✅ تسجيل الـ history
                    _db.PatientStatusHistories.Add(new PatientStatusHistory
                    {
                        PatientID = patientId,
                        OldStatus = oldStatus,
                        NewStatus = "Allocated",
                        ChangedAt = DateTime.Now
                    });

                    await _db.SaveChangesAsync();
                }
            }

            return await GetPatientDetailsJson(patientId);
        }

        // ── Remove ────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Remove(int patientId, int appUserId)
        {
            var record = await _db.AllocatedStudents
                .FirstOrDefaultAsync(a => a.PatientID == patientId && a.AppUserId == appUserId);

            if (record != null)
            {
                _db.AllocatedStudents.Remove(record);
                await _db.SaveChangesAsync();

                // ✅ إذا انشال آخر طالب → ارجع لـ Screening تلقائياً
                int remaining = await _db.AllocatedStudents
                    .CountAsync(a => a.PatientID == patientId);

                if (remaining == 0)
                {
                    var patient = await _db.Patients.FindAsync(patientId);
                    if (patient != null && patient.PatientStatus == "Allocated")
                    {
                        patient.PatientStatus = "Screening";

                        // ✅ تسجيل الـ history
                        _db.PatientStatusHistories.Add(new PatientStatusHistory
                        {
                            PatientID = patientId,
                            OldStatus = "Allocated",
                            NewStatus = "Screening",
                            ChangedAt = DateTime.Now
                        });

                        await _db.SaveChangesAsync();
                    }
                }
            }

            return await GetPatientDetailsJson(patientId);
        }
    }
}