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
            string? visitDate,
            int page = 1)
        {
            int pageSize = 10;

            var query = _context.Patients.AsQueryable();

            DateTime? parsedVisitDate = null;

            if (!string.IsNullOrWhiteSpace(visitDate) &&
                DateTime.TryParseExact(
                    visitDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime selectedVisitDate))
            {
                parsedVisitDate = selectedVisitDate.Date;
            }

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

            if (parsedVisitDate.HasValue)
            {
                DateTime fromDate = parsedVisitDate.Value.Date;
                DateTime toDate = fromDate.AddDays(1);

                query = query.Where(p =>
                    _context.Appointments.Any(a =>
                        a.PatientID == p.PatientID &&
                        a.AppointmentDate >= fromDate &&
                        a.AppointmentDate < toDate));
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

            var patientIds = patients
                .Select(p => p.PatientID)
                .ToList();

            var appointmentRows = await _context.Appointments
                .Where(a => patientIds.Contains(a.PatientID))
                .Select(a => new
                {
                    a.PatientID,
                    a.AppointmentDate
                })
                .ToListAsync();

            var displayedVisitDates =
                new Dictionary<int, DateTime?>();

            foreach (int patientId in patientIds)
            {
                var patientDates = appointmentRows
                    .Where(a => a.PatientID == patientId)
                    .Select(a => (DateTime?)a.AppointmentDate)
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value)
                    .OrderBy(d => d)
                    .ToList();

                DateTime? displayedDate = null;

                if (parsedVisitDate.HasValue)
                {
                    displayedDate = patientDates
                        .FirstOrDefault(d =>
                            d.Date == parsedVisitDate.Value.Date);

                    if (displayedDate == DateTime.MinValue)
                    {
                        displayedDate = null;
                    }
                }
                else
                {
                    displayedDate = patientDates
                        .FirstOrDefault(d => d.Date >= DateTime.Today);

                    if (displayedDate == DateTime.MinValue)
                    {
                        displayedDate = patientDates
                            .OrderByDescending(d => d)
                            .FirstOrDefault();

                        if (displayedDate == DateTime.MinValue)
                        {
                            displayedDate = null;
                        }
                    }
                }

                displayedVisitDates[patientId] = displayedDate;
            }

            ViewBag.VisitDates = displayedVisitDates;

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
            ViewBag.VisitDate = parsedVisitDate?.ToString("yyyy-MM-dd") ?? "";

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
                        a.PatientID == patientId &&
                        a.IsActive);

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
        // POST: /AdminStatus/AssignStudent
        // Adds the student allocation only.
        // Patient status changes only after Confirm Allocation.
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignStudent(
            int patientId,
            int appUserId)
        {
            bool patientExists =
                await _context.Patients
                    .AnyAsync(p => p.PatientID == patientId);

            if (!patientExists)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Patient not found."
                });
            }

            var student =
                await _context.AppUsers
                    .FirstOrDefaultAsync(u =>
                        u.Id == appUserId &&
                        (u.Status == null || u.Status == "Active"));

            if (student == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "The selected student account is not active or does not exist."
                });
            }

            bool alreadyAssigned =
                await _context.AllocatedStudents
                    .AnyAsync(a =>
                        a.PatientID == patientId &&
                        a.AppUserId == appUserId &&
                        a.IsActive);

            if (!alreadyAssigned)
            {
                _context.AllocatedStudents.Add(
                    new AllocatedStudent
                    {
                        PatientID = patientId,
                        AppUserId = appUserId,
                        AssignedDate = DateTime.Now,
                        IsActive = true,
                        RemovedDate = null
                    });

                await _context.SaveChangesAsync();
            }

            return await BuildAllocationJson(patientId);
        }

        // ═══════════════════════════════════════════════════════
        // POST: /AdminStatus/RemoveStudent
        // Removes the allocation only.
        // It does not change PatientStatus automatically.
        // Screening is confirmed separately by ChangeStatus.
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudent(
            int patientId,
            int appUserId)
        {
            var allocation =
                await _context.AllocatedStudents
                    .FirstOrDefaultAsync(a =>
                        a.PatientID == patientId &&
                        a.AppUserId == appUserId &&
                        a.IsActive);

            if (allocation != null)
            {
                allocation.IsActive = false;
                allocation.RemovedDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return await BuildAllocationJson(patientId);
        }

        // ═══════════════════════════════════════════════════════
        // Allocation JSON used by the Admin Status modal.
        // ═══════════════════════════════════════════════════════
        private async Task<IActionResult> BuildAllocationJson(
            int patientId)
        {
            var assignedIds =
                await _context.AllocatedStudents
                    .AsNoTracking()
                    .Where(a => a.PatientID == patientId)
                    .Select(a => a.AppUserId)
                    .Distinct()
                    .ToListAsync();

            var studentTypeIds =
                await _context.UserTypes
                    .AsNoTracking()
                    .Where(t =>
                        t.NameEn != null &&
                        t.NameEn.ToLower() == "student")
                    .Select(t => t.Id)
                    .ToListAsync();

            var allStudents =
                await _context.AppUsers
                    .AsNoTracking()
                    .Where(u =>
                        studentTypeIds.Contains(u.UserTypeId) &&
                        (u.Status == null || u.Status == "Active"))
                    .Select(u => new
                    {
                        id = u.Id,
                        nameEn = string.IsNullOrWhiteSpace(u.NameEn)
                            ? u.UserLog
                            : u.NameEn,
                        userTypeEn = "Student",
                        status = string.IsNullOrWhiteSpace(u.Status)
                            ? "Active"
                            : u.Status
                    })
                    .OrderBy(u => u.nameEn)
                    .ToListAsync();

            var assigned =
                allStudents
                    .Where(u => assignedIds.Contains(u.id))
                    .ToList();

            var available =
                allStudents
                    .Where(u => !assignedIds.Contains(u.id))
                    .ToList();

            return Json(new
            {
                success = true,
                assigned,
                available,
                assignedCount = assigned.Count,
                availableCount = available.Count
            });
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
            string? visitDate,
            int page = 1)
        {
            var redirectArgs =
                new
                {
                    searchName,
                    searchID,
                    filterStatus,
                    visitDate,
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
                            a.PatientID == patientId &&
                            a.IsActive);

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
                            a.PatientID == patientId &&
                            a.IsActive);

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