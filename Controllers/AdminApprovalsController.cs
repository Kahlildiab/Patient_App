using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Filters;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [AuthFilter("Admin", "Fulltime Supervisor", "Parttime Supervisor")]
    public class AdminApprovalsController : Controller
    {
        private readonly AppDbContext _context;

        public AdminApprovalsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var pendingVisits = await _context.Visits
                .Include(v => v.Patient)
                .Where(v => v.AdminApprovalStatus == "Pending")
                .OrderByDescending(v => v.CreatedDate)
                .ToListAsync();

            var pendingProcedures = await _context.TreatmentProcedures
                .Include(p => p.Patient)
                .Where(p => p.AdminApprovalStatus == "Pending")
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            var pendingOrders = await _context.Orders
                .Include(o => o.Patient)
                .Include(o => o.CreatedByUser)
                .Include(o => o.ConsentDetail)
                .Include(o => o.ReferralDetail)
                .Include(o => o.MedicationDetail)
                .Include(o => o.XRayDetail)
                .Include(o => o.DischargeDetail)
                .Where(o => o.AdminApprovalStatus == "Pending")
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            ViewBag.PendingVisits = pendingVisits;
            ViewBag.PendingProcedures = pendingProcedures;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.TotalPending = pendingVisits.Count
                                      + pendingProcedures.Count
                                      + pendingOrders.Count;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAllProcedures()
        {
            var adminName = HttpContext.Session.GetString("FullName") ?? "Admin";

            var pending = await _context.TreatmentProcedures
                .Where(p => p.AdminApprovalStatus == "Pending")
                .ToListAsync();

            foreach (var p in pending)
            {
                p.AdminApprovalStatus = "Approved";
                p.AdminApprovedBy = adminName;
                p.AdminApprovedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"✅ {pending.Count} procedure(s) approved successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAllVisits()
        {
            var adminName = HttpContext.Session.GetString("FullName") ?? "Admin";

            var pending = await _context.Visits
                .Where(v => v.AdminApprovalStatus == "Pending")
                .ToListAsync();

            foreach (var v in pending)
            {
                v.AdminApprovalStatus = "Approved";
                v.AdminApprovedBy = adminName;
                v.AdminApprovedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"✅ {pending.Count} visit(s) approved successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAllOrders()
        {
            var adminName = HttpContext.Session.GetString("FullName") ?? "Admin";

            var pending = await _context.Orders
                .Where(o => o.AdminApprovalStatus == "Pending")
                .ToListAsync();

            foreach (var o in pending)
            {
                o.AdminApprovalStatus = "Approved";
                o.AdminApprovedBy = adminName;
                o.AdminApprovedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"✅ {pending.Count} order(s) approved successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}