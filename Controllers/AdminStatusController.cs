using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Filters;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [AuthFilter("Admin")]
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
                query = query.Where(p =>
                    (p.FirstName + " " + p.SecondName + " " + p.ThirdName + " " + p.FourthName)
                    .ToLower().Contains(searchName.Trim().ToLower()));

            if (!string.IsNullOrWhiteSpace(searchID))
                query = query.Where(p =>
                    p.NationalID_PassportNumber != null &&
                    p.NationalID_PassportNumber.ToLower().Contains(searchID.Trim().ToLower()));

            if (!string.IsNullOrWhiteSpace(filterStatus))
            {
                if (filterStatus == "Screening")
                    query = query.Where(p =>
                        p.PatientStatus == "Screening" || string.IsNullOrEmpty(p.PatientStatus));
                else
                    query = query.Where(p => p.PatientStatus == filterStatus);
            }

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var patients = await query
                .OrderByDescending(p => p.PatientID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPatients = await _context.Patients.CountAsync();
            ViewBag.Screening = await _context.Patients.CountAsync(p => p.PatientStatus == "Screening" || string.IsNullOrEmpty(p.PatientStatus));
            ViewBag.Allocated = await _context.Patients.CountAsync(p => p.PatientStatus == "Allocated");
            ViewBag.Discharged = await _context.Patients.CountAsync(p => p.PatientStatus == "Discharged");

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.SearchName = searchName ?? "";
            ViewBag.SearchID = searchID ?? "";
            ViewBag.FilterStatus = filterStatus ?? "";

            return View(patients);
        }

        // ═══════════════════════════════════════════════════════
        //  GET: /AdminStatus/GetStudentCount?patientId=X
        // ═══════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetStudentCount(int patientId)
        {
            var count = await _context.AllocatedStudents
                .CountAsync(a => a.PatientID == patientId);
            return Json(new { count });
        }

        // ═══════════════════════════════════════════════════════
        //  GET: /AdminStatus/HasDischargeOrder?patientId=X
        // ═══════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> HasDischargeOrder(int patientId)
        {
            bool hasOrder = false;
            // TODO: uncomment when Orders model is ready:
            // hasOrder = await _context.Orders
            //     .AnyAsync(o => o.PatientID == patientId && o.OrderType == "Discharged");
            return Json(new { hasOrder });
        }

        // ═══════════════════════════════════════════════════════
        //  POST: /AdminStatus/ChangeStatus
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
            var redirectArgs = new { searchName, searchID, filterStatus, page };

            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                TempData["Error"] = "Patient not found.";
                return RedirectToAction(nameof(AdminStatus), redirectArgs);
            }

            var allowed = new[] { "Screening", "Allocated", "Discharged" };
            if (!allowed.Contains(newStatus))
            {
                TempData["Error"] = "Invalid status value.";
                return RedirectToAction(nameof(AdminStatus), redirectArgs);
            }

            string currentStatus = string.IsNullOrEmpty(patient.PatientStatus)
                                 ? "Screening" : patient.PatientStatus;

            if (currentStatus == newStatus)
                return RedirectToAction(nameof(AdminStatus), redirectArgs);

            // ══════════════════════════════════════════════════
            //  TRANSITION RULES
            // ══════════════════════════════════════════════════

            // Rule 1: Allocated → Screening — requires 0 students
            if (currentStatus == "Allocated" && newStatus == "Screening")
            {
                int studentCount = await _context.AllocatedStudents
                    .CountAsync(a => a.PatientID == patientId);
                if (studentCount > 0)
                {
                    TempData["Error"] = $"Cannot revert to Screening. Please remove all {studentCount} assigned student(s) first.";
                    return RedirectToAction(nameof(AdminStatus), redirectArgs);
                }
            }

            // Rule 2: * → Allocated — requires at least 1 student
            if (newStatus == "Allocated" && currentStatus != "Allocated")
            {
                int studentCount = await _context.AllocatedStudents
                    .CountAsync(a => a.PatientID == patientId);
                if (studentCount == 0)
                {
                    TempData["Error"] = "At least one student must be assigned before changing status to Allocated.";
                    return RedirectToAction(nameof(AdminStatus), redirectArgs);
                }
            }

            // Rule 3: Allocated → Discharged — requires Discharge Order
            if (currentStatus == "Allocated" && newStatus == "Discharged")
            {
                // TODO: uncomment when Orders model is ready
                // bool hasOrder = await _context.Orders
                //     .AnyAsync(o => o.PatientID == patientId && o.OrderType == "Discharged");
                // if (!hasOrder)
                // {
                //     TempData["Error"] = "A Discharge Order must be created before changing status to Discharged.";
                //     return RedirectToAction(nameof(AdminStatus), redirectArgs);
                // }
            }

            //Rule 4: Discharged → Allocated — requires NO active Discharge Order
            //if (currentStatus == "Discharged" && newStatus == "Allocated")
            //{
            //TODO: uncomment when Orders model is ready
            //     bool hasOrder = await _context.Orders
            //         .AnyAsync(o => o.PatientID == patientId && o.OrderType == "Discharged");
            //    if (hasOrder)
            //    {
            //        TempData["Error"] = "Cannot re-allocate: patient still has an active Discharge Order.";
            //        return RedirectToAction(nameof(AdminStatus), redirectArgs);
            //    }
            //}

            // Rule 4: Discharged → Allocated
            if (currentStatus == "Discharged" && newStatus == "Allocated")
            {
                TempData["Error"] = "Cannot change status from Discharged to Allocated.";
                return RedirectToAction(nameof(AdminStatus), redirectArgs);
            }

            // ══════════════════════════════════════════════════
            //  APPLY CHANGE + LOG HISTORY
            // ══════════════════════════════════════════════════
            patient.PatientStatus = newStatus;

            _context.PatientStatusHistories.Add(new PatientStatusHistory
            {
                PatientID = patientId,
                OldStatus = currentStatus,
                NewStatus = newStatus,
                ChangedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = $"{patient.FirstName} {patient.FourthName} — status changed to {newStatus}.";
            return RedirectToAction(nameof(AdminStatus), redirectArgs);
        }

        // ═══════════════════════════════════════════════════════
        //  POST: /AdminStatus/BulkChangeStatus
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkChangeStatus(
            List<int> selectedPatients,
            string bulkStatus)
        {
            if (selectedPatients == null || !selectedPatients.Any())
            {
                TempData["Error"] = "No patients selected.";
                return RedirectToAction(nameof(AdminStatus));
            }

            var allowed = new[] { "Screening", "Allocated", "Discharged" };
            if (!allowed.Contains(bulkStatus))
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToAction(nameof(AdminStatus));
            }

            var patients = await _context.Patients
                .Where(p => selectedPatients.Contains(p.PatientID))
                .ToListAsync();

            foreach (var p in patients)
            {
                string oldStatus = string.IsNullOrEmpty(p.PatientStatus) ? "Screening" : p.PatientStatus;
                if (oldStatus == bulkStatus) continue;

                p.PatientStatus = bulkStatus;

                _context.PatientStatusHistories.Add(new PatientStatusHistory
                {
                    PatientID = p.PatientID,
                    OldStatus = oldStatus,
                    NewStatus = bulkStatus,
                    ChangedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"{patients.Count} patient(s) updated to {bulkStatus}.";
            return RedirectToAction(nameof(AdminStatus));
        }
    }
}