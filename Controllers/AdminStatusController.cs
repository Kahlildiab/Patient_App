using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Filters;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [AuthFilter(
        "Admin",
        "Fulltime Supervisor",
        "Parttime Supervisor"
    )]
    public class AdminStatusController : Controller
    {
        private readonly AppDbContext _context;

        public AdminStatusController(AppDbContext context)
        {
            _context = context;
        }

        // ═══════════════════════════════════════════════════════
        //  GET: /AdminStatus/AdminStatus
        // ═══════════════════════════════════════════════════════
        public async Task<IActionResult> AdminStatus(
            string? searchName,
            string? searchID,
            string? filterStatus,
            int page = 1)
        {
            int pageSize = 10;

            var query = _context.Patients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(p =>
                    (p.FirstName + " " + p.SecondName + " " +
                     p.ThirdName + " " + p.FourthName)
                    .ToLower()
                    .Contains(searchName.Trim().ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(searchID))
            {
                query = query.Where(p =>
                    p.NationalID_PassportNumber != null &&
                    p.NationalID_PassportNumber
                        .ToLower()
                        .Contains(searchID.Trim().ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(filterStatus))
            {
                if (filterStatus == "Screening")
                {
                    query = query.Where(p =>
                        p.PatientStatus == "Screening" ||
                        string.IsNullOrEmpty(p.PatientStatus));
                }
                else
                {
                    query = query.Where(p =>
                        p.PatientStatus == filterStatus);
                }
            }

            int totalItems = await query.CountAsync();

            int totalPages =
                (int)Math.Ceiling(
                    (double)totalItems / pageSize);

            if (page < 1)
            {
                page = 1;
            }

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var patients = await query
                .OrderByDescending(p => p.PatientID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPatients =
                await _context.Patients.CountAsync();

            ViewBag.Screening =
                await _context.Patients.CountAsync(p =>
                    p.PatientStatus == "Screening" ||
                    string.IsNullOrEmpty(p.PatientStatus));

            ViewBag.Allocated =
                await _context.Patients.CountAsync(p =>
                    p.PatientStatus == "Allocated");

            ViewBag.Discharged =
                await _context.Patients.CountAsync(p =>
                    p.PatientStatus == "Discharged");

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.SearchName = searchName ?? "";
            ViewBag.SearchID = searchID ?? "";
            ViewBag.FilterStatus = filterStatus ?? "";

            return View(patients);
        }

        // ═══════════════════════════════════════════════════════
        // GET: /AdminStatus/GetStudentCount?patientId=X
        // ═══════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetStudentCount(
            int patientId)
        {
            int count =
                await _context.AllocatedStudents
                    .CountAsync(a =>
                        a.PatientID == patientId);

            return Json(new { count });
        }

        // ═══════════════════════════════════════════════════════
        // GET: /AdminStatus/HasDischargeOrder?patientId=X
        // ═══════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> HasDischargeOrder(
            int patientId)
        {
            bool hasOrder = false;

            // TODO: فعّل هذا الشرط عندما يصبح Discharge Order جاهزاً.
            // hasOrder = await _context.Orders
            //     .AnyAsync(o =>
            //         o.PatientID == patientId &&
            //         o.OrderType == "Discharged");

            return Json(new { hasOrder });
        }

        // ═══════════════════════════════════════════════════════
        // POST: /AdminStatus/ChangeStatus
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(
            int patientId,
            string newStatus,
            string? searchName,
            string? searchID,
            string? filterStatus,
            int page = 1)
        {
            var redirectArgs =
                new
                {
                    searchName,
                    searchID,
                    filterStatus,
                    page
                };

            var patient =
                await _context.Patients
                    .FindAsync(patientId);

            if (patient == null)
            {
                TempData["Error"] =
                    "Patient not found.";

                return RedirectToAction(
                    nameof(AdminStatus),
                    redirectArgs);
            }

            var allowed =
                new[]
                {
                    "Screening",
                    "Allocated",
                    "Discharged"
                };

            if (!allowed.Contains(newStatus))
            {
                TempData["Error"] =
                    "Invalid status value.";

                return RedirectToAction(
                    nameof(AdminStatus),
                    redirectArgs);
            }

            string currentStatus =
                string.IsNullOrEmpty(
                    patient.PatientStatus)
                    ? "Screening"
                    : patient.PatientStatus;

            if (currentStatus == newStatus)
            {
                return RedirectToAction(
                    nameof(AdminStatus),
                    redirectArgs);
            }

            // Allocated -> Screening requires zero students.
            if (
                currentStatus == "Allocated" &&
                newStatus == "Screening")
            {
                int studentCount =
                    await _context.AllocatedStudents
                        .CountAsync(a =>
                            a.PatientID == patientId);

                if (studentCount > 0)
                {
                    TempData["Error"] =
                        $"Cannot revert to Screening. Please remove all {studentCount} assigned student(s) first.";

                    return RedirectToAction(
                        nameof(AdminStatus),
                        redirectArgs);
                }
            }

            // Any status -> Allocated requires at least one student.
            if (
                newStatus == "Allocated" &&
                currentStatus != "Allocated")
            {
                int studentCount =
                    await _context.AllocatedStudents
                        .CountAsync(a =>
                            a.PatientID == patientId);

                if (studentCount == 0)
                {
                    TempData["Error"] =
                        "At least one student must be assigned before changing status to Allocated.";

                    return RedirectToAction(
                        nameof(AdminStatus),
                        redirectArgs);
                }
            }

            // Discharged -> Allocated is not allowed.
            if (
                currentStatus == "Discharged" &&
                newStatus == "Allocated")
            {
                TempData["Error"] =
                    "Cannot change status from Discharged to Allocated.";

                return RedirectToAction(
                    nameof(AdminStatus),
                    redirectArgs);
            }

            patient.PatientStatus = newStatus;

            _context.PatientStatusHistories.Add(
                new PatientStatusHistory
                {
                    PatientID = patientId,
                    OldStatus = currentStatus,
                    NewStatus = newStatus,
                    ChangedAt = DateTime.Now
                });

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{patient.FirstName} {patient.FourthName} — status changed to {newStatus}.";

            return RedirectToAction(
                nameof(AdminStatus),
                redirectArgs);
        }

        // ═══════════════════════════════════════════════════════
        // POST: /AdminStatus/BulkChangeStatus
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkChangeStatus(
            List<int> selectedPatients,
            string bulkStatus)
        {
            if (
                selectedPatients == null ||
                !selectedPatients.Any())
            {
                TempData["Error"] =
                    "No patients selected.";

                return RedirectToAction(
                    nameof(AdminStatus));
            }

            var allowed =
                new[]
                {
                    "Screening",
                    "Allocated",
                    "Discharged"
                };

            if (!allowed.Contains(bulkStatus))
            {
                TempData["Error"] =
                    "Invalid status.";

                return RedirectToAction(
                    nameof(AdminStatus));
            }

            var patients =
                await _context.Patients
                    .Where(p =>
                        selectedPatients.Contains(
                            p.PatientID))
                    .ToListAsync();

            int changedCount = 0;

            foreach (var patient in patients)
            {
                string oldStatus =
                    string.IsNullOrEmpty(
                        patient.PatientStatus)
                        ? "Screening"
                        : patient.PatientStatus;

                if (oldStatus == bulkStatus)
                {
                    continue;
                }

                patient.PatientStatus =
                    bulkStatus;

                _context.PatientStatusHistories.Add(
                    new PatientStatusHistory
                    {
                        PatientID =
                            patient.PatientID,

                        OldStatus =
                            oldStatus,

                        NewStatus =
                            bulkStatus,

                        ChangedAt =
                            DateTime.Now
                    });

                changedCount++;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{changedCount} patient(s) updated to {bulkStatus}.";

            return RedirectToAction(
                nameof(AdminStatus));
        }
    }
}
