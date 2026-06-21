using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    public class MedicalHistoriesController : Controller
    {
        private readonly AppDbContext _context;
        public MedicalHistoriesController(AppDbContext context) => _context = context;

        // GET: MedicalHistories
        public async Task<IActionResult> Index()
        {
            var list = _context.MedicalHistories.Include(m => m.Patient);
            return View(await list.ToListAsync());
        }

        // GET: MedicalHistories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var model = await _context.MedicalHistories
                .Include(m => m.Patient)
                .FirstOrDefaultAsync(m => m.MedicalHistoryID == id);

            if (model == null) return NotFound();
            return View(model);
        }

        // GET: MedicalHistories/Create
        public IActionResult Create(int patientId)
        {
            return View(new MedicalHistory { PatientID = patientId, ReviewDate = DateTime.Today });
        }

        // POST: MedicalHistories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
     [Bind("PatientID,MedicalHistoryVerified,MedicalHistoryReviewedThisVisit,ChangesStatus,ReviewDate,CaseComplexity,Allergies,AllergyNotes,OngoingTreatment,RecentHospitalization,HospitalizationNotes,IsPregnant,IsBreastfeeding")]
    MedicalHistory model)
        {
            var existing = await _context.MedicalHistories
                .FirstOrDefaultAsync(m => m.PatientID == model.PatientID);

            model.LastUpdated = DateTime.Now;

            if (existing == null)
            {
                _context.MedicalHistories.Add(model);
            }
            else
            {
                existing.MedicalHistoryVerified = model.MedicalHistoryVerified;
                existing.MedicalHistoryReviewedThisVisit = model.MedicalHistoryReviewedThisVisit;
                existing.AllergyNotes = model.AllergyNotes;
                existing.ChangesStatus = model.ChangesStatus;
                existing.ReviewDate = model.ReviewDate;
                existing.CaseComplexity = model.CaseComplexity;
                existing.Allergies = model.Allergies;
                existing.OngoingTreatment = model.OngoingTreatment;
                existing.RecentHospitalization = model.RecentHospitalization;
                existing.HospitalizationNotes = model.HospitalizationNotes; // ← هذا موجود ✅
                existing.IsPregnant = model.IsPregnant;
                existing.IsBreastfeeding = model.IsBreastfeeding;
                existing.LastUpdated = model.LastUpdated;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Patients", new { id = model.PatientID });
        }

        // GET: MedicalHistories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var model = await _context.MedicalHistories.FindAsync(id);
            if (model == null) return NotFound();

            return View(model);
        }

        // POST: MedicalHistories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("MedicalHistoryID,PatientID,MedicalHistoryVerified,MedicalHistoryReviewedThisVisit,ChangesStatus,ReviewDate,CaseComplexity,Allergies,OngoingTreatment,RecentHospitalization,IsPregnant,IsBreastfeeding")]
            MedicalHistory model)
        {
            if (id != model.MedicalHistoryID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    model.LastUpdated = DateTime.Now;
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicalHistoryExists(model.MedicalHistoryID)) return NotFound();
                    else throw;
                }
                return RedirectToAction("Details", "Patients", new { id = model.PatientID });
            }
            return View(model);
        }

        // GET: MedicalHistories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var model = await _context.MedicalHistories
                .Include(m => m.Patient)
                .FirstOrDefaultAsync(m => m.MedicalHistoryID == id);

            if (model == null) return NotFound();
            return View(model);
        }

        // POST: MedicalHistories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var model = await _context.MedicalHistories.FindAsync(id);
            if (model != null) _context.MedicalHistories.Remove(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Patients", new { id = model!.PatientID });
        }

        private bool MedicalHistoryExists(int id)
            => _context.MedicalHistories.Any(e => e.MedicalHistoryID == id);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateVerification(int PatientID, int MedicalHistoryID, bool MedicalHistoryVerified)
        {
            var mh = await _context.MedicalHistories.FindAsync(MedicalHistoryID);
            if (mh != null)
            {
                mh.MedicalHistoryVerified = MedicalHistoryVerified;
                await _context.SaveChangesAsync();
            }
            return Json(new { success = true });
        }
    }

}