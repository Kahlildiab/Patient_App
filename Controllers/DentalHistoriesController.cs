using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    public class DentalHistoriesController : Controller
    {
        private readonly AppDbContext _context;

        public DentalHistoriesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(DentalHistory model)
        {
            var existing = await _context.DentalHistories
                .FirstOrDefaultAsync(d => d.PatientID == model.PatientID);

            if (existing != null)
            {
                existing.FirstAppointmentDate = model.FirstAppointmentDate;
                existing.AttendedAppointment = model.AttendedAppointment;
                existing.FrequencyOfDentalCheckup = model.FrequencyOfDentalCheckup;
                existing.BrushingFrequency = model.BrushingFrequency;
                existing.BrushingTechnique = model.BrushingTechnique;
                existing.TypeOfToothbrush = model.TypeOfToothbrush;
                existing.TypeOfToothpaste = model.TypeOfToothpaste;
                existing.DentalFloss = model.DentalFloss;
                existing.InterdentalBrushes = model.InterdentalBrushes;
                existing.Mouthwash = model.Mouthwash;
                existing.TongueCleaner = model.TongueCleaner;
                existing.WaterFlosser = model.WaterFlosser;

                _context.Update(existing);
            }
            else
            {
                _context.Add(model);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "true";
            return RedirectToAction("Details", "Patients", new { id = model.PatientID, tab = "dental-history" });
        }
    }
}