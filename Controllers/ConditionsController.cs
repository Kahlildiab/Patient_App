using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    public class ConditionsController : Controller
    {
        private readonly AppDbContext _context;
        public ConditionsController(AppDbContext context) => _context = context;

        // GET: Conditions/Create
        public IActionResult Create(int patientId)
        {
            return View(new CreateConditionsViewModel { PatientID = patientId });
        }

        // POST: Conditions/Create (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateConditionsViewModel vm)
        {
            var validItems = vm.Conditions?
                .Where(c => !string.IsNullOrWhiteSpace(c.ConditionName))
                .ToList();

            if (validItems == null || !validItems.Any())
                return Json(new { success = false, message = "Please add at least one condition." });

            var savedConditions = new List<object>();

            foreach (var item in validItems)
            {
                var condition = new Condition
                {
                    PatientID = vm.PatientID,
                    ConditionName = item.ConditionName!,
                    YearOfDiagnosis = item.YearOfDiagnosis,
                    IsCurrentlyActive = item.IsCurrentlyActive,
                    IsFlagged = item.IsFlagged,
                    Notes = item.Notes,
                    CreatedDate = DateTime.Now
                };
                _context.Add(condition);
                await _context.SaveChangesAsync();

                savedConditions.Add(new
                {
                    conditionID = condition.ConditionID,
                    conditionName = condition.ConditionName,
                    yearOfDiagnosis = condition.YearOfDiagnosis,
                    isCurrentlyActive = condition.IsCurrentlyActive,
                    isFlagged = condition.IsFlagged,
                    notes = condition.Notes
                });
            }

            return Json(new { success = true, conditions = savedConditions });
        }

        // GET: Conditions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var model = await _context.Conditions.FindAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }

        // POST: Conditions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Condition model)
        {
            if (id != model.ConditionID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Conditions.Any(e => e.ConditionID == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction("Details", "Patient", new { id = model.PatientID });
            }
            return View(model);
        }

        // GET: Conditions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var model = await _context.Conditions.FindAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }

        // POST: Conditions/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int ConditionID)
        {
            var model = await _context.Conditions.FindAsync(ConditionID);
            if (model == null) return NotFound();

            int patientID = model.PatientID;
            _context.Conditions.Remove(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Patient", new { id = patientID });
        }

        // POST: Conditions/DeleteAjax  ✅ FIXED - no extra spaces in body
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var model = await _context.Conditions.FindAsync(id);
            if (model == null)
                return Json(new { success = false, message = "Condition not found." });

            _context.Conditions.Remove(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // POST: Conditions/EditAjax
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAjax(
            int ConditionID,
            string ConditionName,
            int? YearOfDiagnosis,
            bool IsCurrentlyActive,
            string? Notes,
            bool IsFlagged)
        {
            var model = await _context.Conditions.FindAsync(ConditionID);
            if (model == null)
                return Json(new { success = false, message = "Condition not found." });

            model.ConditionName = ConditionName;
            model.YearOfDiagnosis = YearOfDiagnosis;
            model.IsCurrentlyActive = IsCurrentlyActive;
            model.IsFlagged = IsFlagged;
            model.Notes = Notes;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}