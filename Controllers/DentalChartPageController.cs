using Microsoft.AspNetCore.Mvc;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    /// <summary>
    /// يفتح صفحة Dental Chart فقط.
    /// مثال:
    /// /DentalChartPage/Index?patientId=5
    /// </summary>
    public class DentalChartPageController : Controller
    {
        [HttpGet]
        public IActionResult Index(int patientId)
        {
            if (patientId <= 0)
                return BadRequest("patientId مطلوب وأكبر من صفر.");

            ViewBag.PatientId = patientId;
            return View("~/Views/DentalCharting/Index.cshtml");
        }
    }
}
