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

        private string GetCurrentUserName()
        {
            return
                HttpContext.Session.GetString("FullName")
                ?? HttpContext.Session.GetString("UserName")
                ?? HttpContext.Session.GetString("Username")
                ?? "Unknown";
        }

        private string GetCurrentUserRole()
        {
            return
                HttpContext.Session.GetString("UserRole")
                ?? "Unknown";
        }

        private static bool IsStudentRole(string? role)
        {
            return string.Equals(
                role,
                "Student",
                StringComparison.OrdinalIgnoreCase
            );
        }

        private IActionResult RedirectToNotes(
            int patientId)
        {
            return RedirectToAction(
                "Details",
                "Patients",
                new
                {
                    id = patientId,
                    tab = "notes"
                }
            );
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
                TempData["NoteError"] =
                    "Patient was not found.";

                return RedirectToNotes(patientId);
            }

            if (await IsVisitClosedAsync(patientId))
            {
                TempData["NoteError"] =
                    "This visit is closed. Notes cannot be added.";

                return RedirectToNotes(patientId);
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["NoteError"] =
                    "Please enter the note content.";

                return RedirectToNotes(patientId);
            }

            bool patientExists =
                await _context.Patients.AnyAsync(
                    patient =>
                        patient.PatientID == patientId
                );

            if (!patientExists)
            {
                TempData["NoteError"] =
                    "Patient was not found.";

                return RedirectToNotes(patientId);
            }

            /*
             * Every new note must belong to the currently open visit.
             * This allows End Visit to check only the notes belonging
             * to that exact visit.
             */
            var currentVisit =
                await _context.Visits
                    .Where(
                        visit =>
                            visit.PatientID == patientId
                            &&
                            !visit.IsClosed
                    )
                    .OrderByDescending(
                        visit => visit.VisitDate
                    )
                    .ThenByDescending(
                        visit => visit.VisitID
                    )
                    .FirstOrDefaultAsync();

            if (currentVisit == null)
            {
                TempData["NoteError"] =
                    "There is no active visit. "
                    + "Create or open a visit before adding a note.";

                return RedirectToNotes(patientId);
            }

            string currentRole =
                GetCurrentUserRole();

            string currentUserName =
                GetCurrentUserName();

            bool isStudent =
                IsStudentRole(currentRole);

            var note = new Note
            {
                PatientId = patientId,
                VisitId = currentVisit.VisitID,
                Content = content.Trim(),
                NoteType = "General",
                CreatedAt = DateTime.Now,
                CreatedBy = currentUserName,
                CreatedByRole = currentRole,

                /*
                 * Student notes require approval.
                 * Notes added by Admin/Supervisors are approved directly.
                 */
                ApprovalStatus =
                    isStudent
                        ? "Pending"
                        : "Approved",

                ApprovedBy =
                    isStudent
                        ? null
                        : currentUserName,

                ApprovedDate =
                    isStudent
                        ? null
                        : DateTime.Now,

                RejectionReason = null
            };

            try
            {
                _context.Notes.Add(note);

                await _context.SaveChangesAsync();

                TempData["NoteSuccess"] =
                    isStudent
                        ? "Note submitted successfully and sent for approval."
                        : "Note added successfully.";
            }
            catch (DbUpdateException ex)
            {
                TempData["NoteError"] =
                    "An error occurred while saving the note.";

                Console.WriteLine(
                    ex.InnerException?.Message
                    ?? ex.Message
                );
            }

            return RedirectToNotes(patientId);
        }

        // POST: Note/EditNote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNote(
            int noteId,
            int patientId,
            string content)
        {
            if (patientId <= 0 || noteId <= 0)
            {
                TempData["NoteError"] =
                    "Note was not found.";

                return RedirectToNotes(patientId);
            }

            if (await IsVisitClosedAsync(patientId))
            {
                TempData["NoteError"] =
                    "This visit is closed. Notes cannot be edited.";

                return RedirectToNotes(patientId);
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["NoteError"] =
                    "Please enter the note content.";

                return RedirectToNotes(patientId);
            }

            var note =
                await _context.Notes
                    .FirstOrDefaultAsync(
                        currentNote =>
                            currentNote.Id == noteId
                            &&
                            currentNote.PatientId == patientId
                    );

            if (note == null)
            {
                TempData["NoteError"] =
                    "Note was not found.";

                return RedirectToNotes(patientId);
            }

            string currentRole =
                GetCurrentUserRole();

            string currentUserName =
                GetCurrentUserName();

            bool isStudent =
                IsStudentRole(currentRole);

            if (isStudent)
            {
                bool ownsNote =
                    string.Equals(
                        note.CreatedByRole,
                        "Student",
                        StringComparison.OrdinalIgnoreCase
                    )
                    &&
                    string.Equals(
                        note.CreatedBy,
                        currentUserName,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (!ownsNote)
                {
                    TempData["NoteError"] =
                        "You can only edit your own note.";

                    return RedirectToNotes(patientId);
                }

                /*
                 * The student edits only after rejection.
                 * Pending notes are waiting for review and approved
                 * notes are locked.
                 */
                if (
                    !string.Equals(
                        note.ApprovalStatus,
                        "Rejected",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    TempData["NoteError"] =
                        "Only a rejected note can be edited by the student.";

                    return RedirectToNotes(patientId);
                }
            }

            note.Content =
                content.Trim();

            if (string.IsNullOrWhiteSpace(note.NoteType))
            {
                note.NoteType = "General";
            }

            if (isStudent)
            {
                /*
                 * A rejected note returns to Pending after correction.
                 */
                note.ApprovalStatus = "Pending";
                note.ApprovedBy = null;
                note.ApprovedDate = null;
                note.RejectionReason = null;
            }
            else
            {
                /*
                 * An edit made by an approval role is accepted directly.
                 */
                note.ApprovalStatus = "Approved";
                note.ApprovedBy = currentUserName;
                note.ApprovedDate = DateTime.Now;
                note.RejectionReason = null;
            }

            try
            {
                await _context.SaveChangesAsync();

                TempData["NoteSuccess"] =
                    isStudent
                        ? "Note updated and sent for approval again."
                        : "Note updated successfully.";
            }
            catch (DbUpdateException ex)
            {
                TempData["NoteError"] =
                    "An error occurred while updating the note.";

                Console.WriteLine(
                    ex.InnerException?.Message
                    ?? ex.Message
                );
            }

            return RedirectToNotes(patientId);
        }

        // POST: Note/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int noteId,
            int patientId)
        {
            if (patientId <= 0 || noteId <= 0)
            {
                TempData["NoteError"] =
                    "Note was not found.";

                return RedirectToNotes(patientId);
            }

            if (await IsVisitClosedAsync(patientId))
            {
                TempData["NoteError"] =
                    "This visit is closed. Notes cannot be deleted.";

                return RedirectToNotes(patientId);
            }

            var note =
                await _context.Notes
                    .FirstOrDefaultAsync(
                        currentNote =>
                            currentNote.Id == noteId
                            &&
                            currentNote.PatientId == patientId
                    );

            if (note == null)
            {
                TempData["NoteError"] =
                    "Note was not found.";

                return RedirectToNotes(patientId);
            }

            string currentRole =
                GetCurrentUserRole();

            string currentUserName =
                GetCurrentUserName();

            bool isStudent =
                IsStudentRole(currentRole);

            if (isStudent)
            {
                bool ownsNote =
                    string.Equals(
                        note.CreatedByRole,
                        "Student",
                        StringComparison.OrdinalIgnoreCase
                    )
                    &&
                    string.Equals(
                        note.CreatedBy,
                        currentUserName,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (!ownsNote)
                {
                    TempData["NoteError"] =
                        "You can only delete your own note.";

                    return RedirectToNotes(patientId);
                }

                if (
                    string.Equals(
                        note.ApprovalStatus,
                        "Approved",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    TempData["NoteError"] =
                        "An approved note cannot be deleted by the student.";

                    return RedirectToNotes(patientId);
                }
            }

            try
            {
                _context.Notes.Remove(note);

                await _context.SaveChangesAsync();

                TempData["NoteSuccess"] =
                    "Note deleted successfully.";
            }
            catch (DbUpdateException ex)
            {
                TempData["NoteError"] =
                    "An error occurred while deleting the note.";

                Console.WriteLine(
                    ex.InnerException?.Message
                    ?? ex.Message
                );
            }

            return RedirectToNotes(patientId);
        }

        private async Task<bool> IsVisitClosedAsync(
            int patientId)
        {
            string userRole =
                GetCurrentUserRole();

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

            var latestVisit =
                await _context.Visits
                    .Where(
                        visit =>
                            visit.PatientID == patientId
                    )
                    .OrderByDescending(
                        visit => visit.VisitDate
                    )
                    .ThenByDescending(
                        visit => visit.VisitID
                    )
                    .FirstOrDefaultAsync();

            return
                latestVisit != null
                &&
                latestVisit.IsClosed;
        }
    }
}
