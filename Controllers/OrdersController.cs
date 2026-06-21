using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [DentalCollegeManagementSystem_AAU.Filters.AuthFilter]
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            var orders = await _context.Orders
                .Where(o => o.PatientID == patientId)
                .Include(o => o.CreatedByUser)
                .Include(o => o.ConsentDetail)
                .Include(o => o.ReferralDetail)
                .Include(o => o.MedicationDetail)
                .Include(o => o.XRayDetail)
                .Include(o => o.DischargeDetail)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            ViewBag.Patient = patient;
            ViewBag.Pending = orders.Count(o => o.AdminApprovalStatus == "Pending" || o.AdminApprovalStatus == null);
            ViewBag.Completed = orders.Count(o => o.Status == "Completed");

            ViewBag.XRays = orders.Count(o => o.OrderType == "XRay");
            ViewBag.Referrals = orders.Count(o => o.OrderType == "Referral");

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int patientId, string orderType, IFormCollection form)
        {
            var userIdStr = HttpContext.Session.GetString("UserID");
            int userId = int.TryParse(userIdStr, out int parsedId) ? parsedId : 0;
            var userRole = HttpContext.Session.GetString("UserRole");

            int count = await _context.Orders.CountAsync() + 1;
            string orderNumber = $"ORD-{count:D4}";

            var order = new Order
            {
                PatientID = patientId,
                OrderType = orderType,
                OrderNumber = orderNumber,
                CreatedByUserID = userId,
                CreatedDate = DateTime.Now,
                Notes = form["Notes"],
                // ✅ لو Student — تضاف كـ Pending للـ Admin
                AdminApprovalStatus = userRole == "Student" ? "Pending" : "Approved"
            };

            switch (orderType)
            {
                case "Consent":
                    order.Status = "Signed";
                    order.ConsentDetail = new ConsentOrder
                    {
                        ConsentType = form["ConsentType"]!,
                        Description = form["Description"],
                        IsSigned = true,
                        SignedDate = DateTime.Now
                    };
                    break;

                case "Referral":
                    order.Status = "Pending";
                    order.ReferralDetail = new ReferralOrder
                    {
                        ReferredTo = form["ReferredTo"]!,
                        Specialty = form["Specialty"]!,
                        Reason = form["Reason"],
                        ReferralDate = DateTime.Now
                    };
                    break;

                case "Medication":
                    order.Status = "Active";
                    int.TryParse(form["DurationDays"], out int duration);
                    order.MedicationDetail = new MedicationOrder
                    {
                        MedicationName = form["MedicationName"]!,
                        Dosage = form["Dosage"]!,
                        Frequency = form["Frequency"]!,
                        DurationDays = duration > 0 ? duration : 1,
                        Instructions = form["Instructions"]
                    };
                    break;

                case "XRay":
                    order.Status = "Pending";
                    order.XRayDetail = new XRayOrder
                    {
                        XRayType = form["XRayType"]!,
                        ToothNumber = form["ToothNumber"],
                        Region = form["Region"],
                        ClinicalIndication = form["ClinicalIndication"]
                    };
                    break;

                case "Discharge":
                    order.Status = "Completed";
                    order.DischargeDetail = new DischargeOrder
                    {
                        DischargeReason = form["DischargeReason"]!,
                        AfterCareInstructions = form["AfterCareInstructions"],
                        FollowUpPlan = form["FollowUpPlan"],
                        DischargeDate = DateTime.Now
                    };

                    if (userRole != "Student")
                    {
                        var patientToDischarge = await _context.Patients.FindAsync(patientId);
                        if (patientToDischarge != null)
                            patientToDischarge.PatientStatus = "Discharged";
                    }
                    break;

                default:
                    TempData["Error"] = "Invalid order type.";
                    return RedirectToAction("Details", "Patients", new { id = patientId });
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = userRole == "Student"
                ? "⏳ Order submitted and awaiting Admin approval."
                : $"{orderType} order created successfully!";

            return RedirectToAction("Details", "Patients", new { id = patientId, activeTab = "orders" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int patientId)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Order deleted successfully.";
            }
            return RedirectToAction("Details", "Patients", new { id = patientId, activeTab = "orders" });
        }

        // ✅ POST - Admin Approve Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminApprove(int id, int patientId)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.AdminApprovalStatus = "Approved";
                order.AdminApprovedBy = HttpContext.Session.GetString("FullName");
                order.AdminApprovedDate = DateTime.Now;

                // لو Discharge — نحدث المريض بعد الموافقة
                if (order.OrderType == "Discharge")
                {
                    var patient = await _context.Patients.FindAsync(order.PatientID);
                    if (patient != null)
                        patient.PatientStatus = "Discharged";
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ Order approved by Admin!";
            }

            return RedirectToAction("Index", "AdminApprovals");
        }

        // ✅ POST - Admin Reject Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminReject(int id, int patientId)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                TempData["Success"] = "🗑️ Order rejected and removed.";
            }

            return RedirectToAction("Index", "AdminApprovals");
        }
    }
}