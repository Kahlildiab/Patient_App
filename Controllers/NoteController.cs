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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(Note note)
        {
            note.CreatedAt = DateTime.Now;
            note.CreatedBy = HttpContext.Session.GetString("UserName") ?? "Unknown";

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Patients", new { id = note.PatientId, tab = "notes" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int noteId, int patientId)
        {
            var note = await _context.Notes.FindAsync(noteId);
            if (note != null)
            {
                _context.Notes.Remove(note);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", "Patients", new { id = patientId, tab = "notes" });
        }
    }
}