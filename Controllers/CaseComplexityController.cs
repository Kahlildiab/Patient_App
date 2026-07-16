using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [AuthFilter(
        "Admin",
        "Fulltime Supervisor",
        "Parttime Supervisor"
    )]
    public class CaseComplexityController : Controller
    {
        private readonly AppDbContext _context;

        public CaseComplexityController(
            AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(
            int patientId,
            string? caseComplexity,
            string? returnTab)
        {
            string? normalizedComplexity =
                NormalizeCaseComplexity(
                    caseComplexity
                );

            string safeReturnTab =
                NormalizeReturnTab(
                    returnTab
                );

            if (
                string.IsNullOrWhiteSpace(
                    normalizedComplexity
                )
            )
            {
                TempData["CaseComplexityError"] =
                    "Please select a valid Case Complexity.";

                return RedirectToPatient(
                    patientId,
                    safeReturnTab
                );
            }

            bool patientExists =
                await _context.Patients
                    .AsNoTracking()
                    .AnyAsync(
                        patient =>
                            patient.PatientID
                                == patientId
                    );

            if (!patientExists)
            {
                return NotFound();
            }

            var visits =
                await _context.Visits
                    .Where(
                        visit =>
                            visit.PatientID
                                == patientId
                    )
                    .OrderBy(
                        visit =>
                            visit.VisitDate
                    )
                    .ThenBy(
                        visit =>
                            visit.VisitID
                    )
                    .ToListAsync();

            if (!visits.Any())
            {
                TempData["CaseComplexityError"] =
                    "The patient does not have a visit yet.";

                return RedirectToPatient(
                    patientId,
                    safeReturnTab
                );
            }

            /*
             * Case Complexity هي قيمة واحدة للمريض
             * ويتم تخزينها في أول زيارة.
             */
            var firstVisit =
                visits.First();

            firstVisit.CaseComplexity =
                normalizedComplexity;

            /*
             * تنظيف أي قيم مكررة في زيارات أخرى
             * حتى يبقى المصدر الوحيد هو أول زيارة.
             */
            foreach (
                var visit in
                visits.Skip(1)
            )
            {
                if (
                    !string.IsNullOrWhiteSpace(
                        visit.CaseComplexity
                    )
                )
                {
                    visit.CaseComplexity =
                        null;
                }
            }

            await _context.SaveChangesAsync();

            TempData["CaseComplexitySuccess"] =
                "Case Complexity was updated successfully to "
                + normalizedComplexity
                + ".";

            return RedirectToPatient(
                patientId,
                safeReturnTab
            );
        }

        private IActionResult RedirectToPatient(
            int patientId,
            string returnTab)
        {
            return RedirectToAction(
                "Details",
                "Patients",
                new
                {
                    id = patientId,
                    tab = returnTab
                }
            );
        }

        private static string?
            NormalizeCaseComplexity(
                string? value)
        {
            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                return null;
            }

            return value
                .Trim()
                .ToLowerInvariant()
                switch
            {
                "mild" =>
                    "Mild",

                "moderate" =>
                    "Moderate",

                "advance" =>
                    "Advance",

                "advanced" =>
                    "Advance",

                _ =>
                    null
            };
        }

        private static string NormalizeReturnTab(
            string? value)
        {
            string requestedTab =
                string.IsNullOrWhiteSpace(
                    value
                )
                    ? "medical-history"
                    : value.Trim();

            var allowedTabs =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                )
                {
                    "medical-history",
                    "dental-history",
                    "social-history",
                    "extraoral-exam",
                    "intraoral-exam",
                    "treatment-plan",
                    "todays-visit",
                    "radiographs",
                    "photos",
                    "competencies",
                    "orders",
                    "notes"
                };

            return allowedTabs.Contains(
                requestedTab
            )
                ? requestedTab
                : "medical-history";
        }
    }
}
