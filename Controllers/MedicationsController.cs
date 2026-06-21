using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    public class MedicationsController : Controller
    {
        private readonly AppDbContext _context;
        public MedicationsController(AppDbContext context) => _context = context;

        // GET: Medications
        public async Task<IActionResult> Index()
        {
            var list = _context.Medications.Include(m => m.Patient);
            return View(await list.ToListAsync());
        }

        // GET: Medications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var model = await _context.Medications
                .Include(m => m.Patient)
                .FirstOrDefaultAsync(m => m.MedicationID == id);
            if (model == null) return NotFound();
            return View(model);
        }

        // GET: Medications/Create
        public IActionResult Create(int patientId)
        {
            ViewData["PatientID"] = new SelectList(_context.Patients, "PatientID", "FirstName");
            return View(new Medication { PatientID = patientId });
        }

        // POST: Medications/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("MedicationID,PatientID,DrugName,Dose,Frequency,Duration,FlagType,CreatedDate")]
            Medication model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedDate = DateTime.Now;
                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Patient", new { id = model.PatientID });
            }
            ViewData["PatientID"] = new SelectList(_context.Patients, "PatientID", "FirstName", model.PatientID);
            return View(model);
        }

        // POST: Medications/CreateAjax
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax(int PatientID, List<Medication> Medications)
        {
            var validItems = Medications?
                .Where(m => !string.IsNullOrWhiteSpace(m.DrugName))
                .ToList();

            if (validItems == null || !validItems.Any())
                return Json(new { success = false, message = "Please add at least one medication." });

            var saved = new List<object>();

            foreach (var item in validItems)
            {
                item.PatientID = PatientID;
                item.CreatedDate = DateTime.Now;
                _context.Add(item);
                await _context.SaveChangesAsync();

                saved.Add(new
                {
                    medicationID = item.MedicationID,
                    drugName = item.DrugName,
                    dose = item.Dose,
                    frequency = item.Frequency,
                    duration = item.Duration,
                    flagType = item.FlagType
                });
            }

            return Json(new { success = true, medications = saved });
        }

        // GET: Medications/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var model = await _context.Medications.FindAsync(id);
            if (model == null) return NotFound();
            ViewData["PatientID"] = new SelectList(_context.Patients, "PatientID", "FirstName", model.PatientID);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
    int id,
    [Bind("MedicationID,PatientID,DrugName,Dose,Frequency,Duration,FlagType")]
    Medication model)
        {
            if (id != model.MedicationID) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    // ✅ احتفظ بالـ CreatedDate الأصلي من DB
                    var original = await _context.Medications
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.MedicationID == id);

                    model.CreatedDate = original?.CreatedDate ?? DateTime.Now;

                    _context.Update(model);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicationExists(model.MedicationID)) return NotFound();
                    else throw;
                }
                return RedirectToAction("Details", "Patient", new { id = model.PatientID });
            }
            ViewData["PatientID"] = new SelectList(_context.Patients, "PatientID", "FirstName", model.PatientID);
            return View(model);
        }

        // GET: Medications/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var model = await _context.Medications
                .Include(m => m.Patient)
                .FirstOrDefaultAsync(m => m.MedicationID == id);
            if (model == null) return NotFound();
            return View(model);
        }

        // POST: Medications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var model = await _context.Medications.FindAsync(id);
            if (model != null) _context.Medications.Remove(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Patient", new { id = model!.PatientID });
        }

        private bool MedicationExists(int id)
            => _context.Medications.Any(e => e.MedicationID == id);

        // POST: Medications/EditAjax
        // POST: Medications/EditAjax
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAjax(int MedicationID, string DrugName, string Dose, string Frequency, string Duration, string FlagType)
        {
            var medication = await _context.Medications.FindAsync(MedicationID);
            if (medication == null)
                return Json(new { success = false, message = "Medication not found." });

            // تحديث الحقول مع الاحتفاظ بـ CreatedDate
            medication.DrugName = DrugName;
            medication.Dose = Dose;
            medication.Frequency = Frequency;
            medication.Duration = Duration;
            medication.FlagType = FlagType;

            try
            {
                _context.Update(medication);
                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    MedicationID = medication.MedicationID,
                    DrugName = medication.DrugName,
                    Dose = medication.Dose,
                    Frequency = medication.Frequency,
                    Duration = medication.Duration,
                    FlagType = medication.FlagType
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Medications.Any(m => m.MedicationID == MedicationID))
                    return Json(new { success = false, message = "Medication no longer exists." });
                throw;
            }
        }

        // POST: Medications/DeleteAjax
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var model = await _context.Medications.FindAsync(id);
            if (model == null)
                return Json(new { success = false, message = "Medication not found." });

            _context.Medications.Remove(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}