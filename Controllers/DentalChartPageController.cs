using Microsoft.AspNetCore.Mvc;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    /// <summary>
    /// Controller يخدّم صفحة الـ Dental Chart (View)
    /// Route: /DentalChartPage/Index?patientId=5
    /// </summary>
    public class DentalChartPageController : Controller
    {
        // GET /DentalChartPage/Index?patientId=5
        public IActionResult Index(int patientId)
        {
            if (patientId <= 0)
                return BadRequest("patientId مطلوب وأكبر من صفر.");

            ViewBag.PatientId = patientId;
            return View("~/Views/DentalCharting/Index.cshtml");
        }
    }
}