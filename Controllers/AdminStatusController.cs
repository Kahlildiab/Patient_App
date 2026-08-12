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
            string? caseComplexity,
            int page = 1)
        {
            const int pageSize = 10;

            var query =
                _context.Patients
                    .AsNoTracking()
                    .AsQueryable();

            DateTime? parsedVisitDate = null;

            if (
                !string.IsNullOrWhiteSpace(visitDate)
                &&
                DateTime.TryParseExact(
                    visitDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime selectedVisitDate
                )
            )
            {
                parsedVisitDate = selectedVisitDate.Date;
            }

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                string normalizedSearchName =
                    searchName.Trim().ToLower();

                query = query.Where(
                    patient =>
                        (
                            patient.FirstName + " "
                            + patient.SecondName + " "
                            + patient.ThirdName + " "
                            + patient.FourthName
                        )
                        .ToLower()
                        .Contains(normalizedSearchName)
                );
            }

            if (!string.IsNullOrWhiteSpace(searchID))
            {
                string normalizedSearchId =
                    searchID.Trim().ToLower();

                query = query.Where(
                    patient =>
                        patient.NationalID_PassportNumber != null
                        && patient.NationalID_PassportNumber
                            .ToLower()
                            .Contains(normalizedSearchId)
                );
            }

            if (!string.IsNullOrWhiteSpace(filterStatus))
            {
                if (
                    string.Equals(
                        filterStatus,
                        "Screening",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    query = query.Where(
                        patient =>
                            patient.PatientStatus == "Screening"
                            || string.IsNullOrEmpty(patient.PatientStatus)
                    );
                }
                else
                {
                    query = query.Where(
                        patient =>
                            patient.PatientStatus == filterStatus
                    );
                }
            }

            if (parsedVisitDate.HasValue)
            {
                DateTime fromDate = parsedVisitDate.Value.Date;
                DateTime toDate = fromDate.AddDays(1);

                query = query.Where(
                    patient =>
                        _context.Appointments.Any(
                            appointment =>
                                appointment.PatientID == patient.PatientID
                                && appointment.AppointmentDate >= fromDate
                                && appointment.AppointmentDate < toDate
                        )
                );
            }

            // Case Complexity is stored in Visits.
            // The first non-empty CaseComplexity for each patient is used,
            // matching the logic that was previously in AllocatedStudent.
            if (!string.IsNullOrWhiteSpace(caseComplexity))
            {
                var candidatePatientIds =
                    await query
                        .Select(patient => patient.PatientID)
                        .ToListAsync();

                var candidateComplexities =
                    await GetCaseComplexityByPatientAsync(
                        candidatePatientIds
                    );

                List<int> matchingPatientIds;

                if (
                    string.Equals(
                        caseComplexity,
                        "Not Set",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    matchingPatientIds =
                        candidatePatientIds
                            .Where(
                                patientId =>
                                    !candidateComplexities.ContainsKey(patientId)
                            )
                            .ToList();
                }
                else
                {
                    matchingPatientIds =
                        candidateComplexities
                            .Where(
                                item =>
                                    string.Equals(
                                        item.Value,
                                        caseComplexity.Trim(),
                                        StringComparison.OrdinalIgnoreCase
                                    )
                            )
                            .Select(item => item.Key)
                            .ToList();
                }

                query = query.Where(
                    patient =>
                        matchingPatientIds.Contains(patient.PatientID)
                );
            }

            int totalItems = await query.CountAsync();

            int totalPages =
                (int)Math.Ceiling(
                    (double)totalItems / pageSize
                );

            if (page < 1)
            {
                page = 1;
            }

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var patients =
                await query
                    .OrderByDescending(
                        patient => patient.PatientID
                    )
                    .Skip(
                        (page - 1) * pageSize
                    )
                    .Take(pageSize)
                    .ToListAsync();

            var patientIds =
                patients
                    .Select(patient => patient.PatientID)
                    .ToList();

            var appointmentRows =
                await _context.Appointments
                    .AsNoTracking()
                    .Where(
                        appointment =>
                            patientIds.Contains(appointment.PatientID)
                    )
                    .Select(
                        appointment => new
                        {
                            appointment.PatientID,
                            appointment.AppointmentDate
                        }
                    )
                    .ToListAsync();

            var displayedVisitDates =
                new Dictionary<int, DateTime?>();

            foreach (int patientId in patientIds)
            {
                var patientDates =
                    appointmentRows
                        .Where(
                            appointment =>
                                appointment.PatientID == patientId
                        )
                        .Select(
                            appointment =>
                                (DateTime?)appointment.AppointmentDate
                        )
                        .Where(date => date.HasValue)
                        .Select(date => date!.Value)
                        .OrderBy(date => date)
                        .ToList();

                DateTime? displayedDate = null;

                if (parsedVisitDate.HasValue)
                {
                    displayedDate =
                        patientDates
                            .FirstOrDefault(
                                date =>
                                    date.Date == parsedVisitDate.Value.Date
                            );

                    if (displayedDate == DateTime.MinValue)
                    {
                        displayedDate = null;
                    }
                }
                else
                {
                    displayedDate =
                        patientDates
                            .FirstOrDefault(
                                date =>
                                    date.Date >= DateTime.Today
                            );

                    if (displayedDate == DateTime.MinValue)
                    {
                        displayedDate =
                            patientDates
                                .OrderByDescending(date => date)
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

            ViewBag.CaseComplexityByPatient =
                await GetCaseComplexityByPatientAsync(patientIds);

            ViewBag.TotalPatients =
                await _context.Patients.CountAsync();

            ViewBag.Screening =
                await _context.Patients.CountAsync(
                    patient =>
                        patient.PatientStatus == "Screening"
                        || string.IsNullOrEmpty(patient.PatientStatus)
                );

            ViewBag.Allocated =
                await _context.Patients.CountAsync(
                    patient =>
                        patient.PatientStatus == "Allocated"
                );

            ViewBag.Discharged =
                await _context.Patients.CountAsync(
                    patient =>
                        patient.PatientStatus == "Discharged"
                );

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.SearchName = searchName ?? string.Empty;
            ViewBag.SearchID = searchID ?? string.Empty;
            ViewBag.FilterStatus = filterStatus ?? string.Empty;
            ViewBag.VisitDate =
                parsedVisitDate?.ToString("yyyy-MM-dd")
                ?? string.Empty;
            ViewBag.CaseComplexity = caseComplexity ?? string.Empty;

            return View(patients);
        }

        private async Task<Dictionary<int, string>>
            GetCaseComplexityByPatientAsync(
                IEnumerable<int> patientIds
            )
        {
            var ids =
                patientIds
                    .Distinct()
                    .ToList();

            if (!ids.Any())
            {
                return new Dictionary<int, string>();
            }

            var rows =
                await _context.Visits
                    .AsNoTracking()
                    .Where(
                        visit =>
                            ids.Contains(visit.PatientID)
                            && visit.CaseComplexity != null
                            && visit.CaseComplexity != ""
                    )
                    .OrderBy(
                        visit => visit.VisitDate
                    )
                    .ThenBy(
                        visit => visit.VisitID
                    )
                    .Select(
                        visit => new
                        {
                            visit.PatientID,
                            visit.CaseComplexity
                        }
                    )
                    .ToListAsync();

            return rows
                .Where(
                    row =>
                        !string.IsNullOrWhiteSpace(row.CaseComplexity)
                )
                .GroupBy(
                    row => row.PatientID
                )
                .ToDictionary(
                    group => group.Key,
                    group => group.First().CaseComplexity!.Trim()
                );
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
                    .Where(a =>
                        a.PatientID == patientId
                        && a.IsActive)
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
            string? caseComplexity,
            int page = 1)
        {
            var redirectArgs =
                new
                {
                    searchName,
                    searchID,
                    filterStatus,
                    visitDate,
                    caseComplexity,
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