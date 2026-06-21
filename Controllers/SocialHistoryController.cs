using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    public class SocialHistoriesController : Controller
    {
        private readonly AppDbContext _context;
        public SocialHistoriesController(AppDbContext context) => _context = context;

        // POST: SocialHistories/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(SocialHistory model)
        {
            ModelState.Remove("Patient");

            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data." });

            var existing = await _context.SocialHistories
                .FirstOrDefaultAsync(s => s.PatientID == model.PatientID);

            if (existing == null)
            {
                model.CreatedDate = DateTime.Now;
                _context.SocialHistories.Add(model);
            }
            else
            {
                existing.Occupation = model.Occupation;
                existing.SmokingStatus = model.SmokingStatus;

                // Alcohol
                existing.ConsumesAlcohol = model.ConsumesAlcohol;
                existing.AlcoholType = model.ConsumesAlcohol ? model.AlcoholType : null;
                existing.AlcoholQuantity = model.ConsumesAlcohol ? model.AlcoholQuantity : null;
                existing.AlcoholFrequency = model.ConsumesAlcohol ? model.AlcoholFrequency : null;
                existing.AlcoholDuration = model.ConsumesAlcohol ? model.AlcoholDuration : null;

                // Drugs
                existing.UsesRecreationalDrugs = model.UsesRecreationalDrugs;
                existing.DrugType = model.UsesRecreationalDrugs ? model.DrugType : null;
                existing.DrugQuantity = model.UsesRecreationalDrugs ? model.DrugQuantity : null;
                existing.DrugFrequency = model.UsesRecreationalDrugs ? model.DrugFrequency : null;
                existing.DrugDuration = model.UsesRecreationalDrugs ? model.DrugDuration : null;

                existing.UpdatedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}