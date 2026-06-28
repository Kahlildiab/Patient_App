using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [DentalCollegeManagementSystem_AAU.Filters.AuthFilter]
    public class NoteController : Controller
    {
        private readonly AppDbContext _context;

        public NoteController(AppDbContext context)
        {
            _context = context;
        }

        // POST: Note/AddNote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(
            int patientId,
            string content)
        {
            if (patientId <= 0)
            {
                TempData["NoteError"] = "Patient was not found.";

                return RedirectToAction(
                    "Details",
                    "Patients",
                    new { id = patientId, tab = "notes" });
            }

            if (await IsVisitClosedAsync(patientId))
            {
                TempData["NoteError"] =
                    "This visit is closed. Notes cannot be added.";

                return RedirectToAction(
                    "Details",
                    "Patients",
                    new { id = patientId, tab = "notes" });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["NoteError"] =
                    "Please enter the note content.";

                return RedirectToAction(
                    "Details",
                    "Patients",
                    new { id = patientId, tab = "notes" });
            }

            bool patientExists = await _context.Patients
                .AnyAsync(p => p.PatientID == patientId);

            if (!patientExists)
            {
                TempData["NoteError"] = "Patient was not found.";

                return RedirectToAction(
                    "Details",
                    "Patients",
                    new { id = patientId, tab = "notes" });
            }

            var note = new Note
            {
                PatientId = patientId,
                Content = content.Trim(),
                CreatedAt = DateTime.Now,
                CreatedBy =
                    HttpContext.Session.GetString("UserName")
                    ?? HttpContext.Session.GetString("FullName")
                    ?? "Unknown"
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            TempData["NoteSuccess"] =
                "Note added successfully.";

            return RedirectToAction(
                "Details",
                "Patients",
                new { id = patientId, tab = "notes" });
        }

        // POST: Note/EditNote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNote(
            int noteId,
            int patientId,
            string content)
        {
            if (await IsVisitClosedAsync(patientId))
            {
                TempData["NoteError"] =
                    "This visit is closed. Notes cannot be edited.";

                return RedirectToAction(
                    "Details",
                    "Patients",
                    new { id = patientId, tab = "notes" });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["NoteError"] =
                    "Please enter the note content.";

                return RedirectToAction(
                    "Details",
                    "Patients",
                    new { id = patientId, tab = "notes" });
            }

            var note = await _context.Notes
                .FirstOrDefaultAsync(n =>
                    n.Id == noteId &&
                    n.PatientId == patientId);

            if (note == null)
            {
                TempData["NoteError"] = "Note was not found.";

                return RedirectToAction(
                    "Details",
                    "Patients",
                    new { id = patientId, tab = "notes" });
            }

            note.Content = content.Trim();

            await _context.SaveChangesAsync();

            TempData["NoteSuccess"] =
                "Note updated successfully.";

            return RedirectToAction(
                "Details",
                "Patients",
                new { id = patientId, tab = "notes" });
        }

        // POST: Note/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int noteId,
            int patientId)
        {
            if (await IsVisitClosedAsync(patientId))
            {
                TempData["NoteError"] =
                    "This visit is closed. Notes cannot be deleted.";

                return RedirectToAction(
                    "Details",
                    "Patients",
                    new { id = patientId, tab = "notes" });
            }

            var note = await _context.Notes
                .FirstOrDefaultAsync(n =>
                    n.Id == noteId &&
                    n.PatientId == patientId);

            if (note == null)
            {
                TempData["NoteError"] = "Note was not found.";

                return RedirectToAction(
                    "Details",
                    "Patients",
                    new { id = patientId, tab = "notes" });
            }

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();

            TempData["NoteSuccess"] =
                "Note deleted successfully.";

            return RedirectToAction(
                "Details",
                "Patients",
                new { id = patientId, tab = "notes" });
        }

        private async Task<bool> IsVisitClosedAsync(int patientId)
        {
            string userRole =
                HttpContext.Session.GetString("UserRole")
                ?? string.Empty;

            bool isAdmin =
                string.Equals(
                    userRole,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase
                );

            // Admin can modify a closed visit.
            if (isAdmin)
            {
                return false;
            }

            var latestVisit = await _context.Visits
                .Where(v => v.PatientID == patientId)
                .OrderByDescending(v => v.VisitDate)
                .ThenByDescending(v => v.VisitID)
                .FirstOrDefaultAsync();

            return latestVisit != null &&
                   latestVisit.IsClosed;
        }
    }
}
