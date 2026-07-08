using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Filters;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [AuthFilter(
        "Admin",
        "Fulltime Supervisor",
        "Parttime Supervisor",
        "Student"
    )]
    public class CompetenciesController : Controller
    {
        private readonly AppDbContext _context;

        public CompetenciesController(
            AppDbContext context)
        {
            _context = context;
        }

        private static bool CanEditCompetencies(
            string? role)
        {
            return
                string.Equals(
                    role,
                    "Admin",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    role,
                    "Fulltime Supervisor",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    role,
                    "Parttime Supervisor",
                    StringComparison.OrdinalIgnoreCase
                );
        }

        private static bool IsStudent(
            string? role)
        {
            return string.Equals(
                role,
                "Student",
                StringComparison.OrdinalIgnoreCase
            );
        }

        private int GetCurrentUserId()
        {
            string userIdText =
                HttpContext.Session.GetString("UserID")
                ?? string.Empty;

            return int.TryParse(
                userIdText,
                out int userId
            )
                ? userId
                : 0;
        }

        private string GetCurrentUserName()
        {
            return
                HttpContext.Session.GetString("FullName")
                ??
                HttpContext.Session.GetString("Username")
                ??
                "Responsible Authority";
        }

        // =====================================================
        // Competencies main page
        //
        // Staff:
        // - sees all students
        // - can search and filter
        // - can open and update any student
        //
        // Student:
        // - sees only own competencies
        // - view only
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            string? status,
            int? studentId)
        {
            string userRole =
                HttpContext.Session.GetString("UserRole")
                ?? string.Empty;

            bool isStudent =
                IsStudent(userRole);

            bool canEdit =
                CanEditCompetencies(userRole);

            if (
                !isStudent
                &&
                !canEdit
            )
            {
                return RedirectToAction(
                    "AccessDenied",
                    "Account"
                );
            }

            int currentUserId =
                GetCurrentUserId();

            if (isStudent)
            {
                if (currentUserId <= 0)
                {
                    return RedirectToAction(
                        "Login",
                        "Account"
                    );
                }

                /*
                 * الطالب لا يستطيع فتح سجل طالب آخر،
                 * حتى لو غيّر studentId في الرابط.
                 */
                studentId =
                    currentUserId;
            }

            search =
                (search ?? string.Empty).Trim();

            status =
                string.IsNullOrWhiteSpace(status)
                    ? "all"
                    : status.Trim().ToLowerInvariant();

            /*
             * القوالب الأصلية فقط.
             * سجلات المرضى القديمة وسجلات الطلاب لا تدخل هنا.
             */
            List<Competency> templates =
                await _context.Competency
                    .AsNoTracking()
                    .Where(
                        competency =>
                            competency.PatientID == null
                            &&
                            competency.StudentUserID == null
                            &&
                            competency.TemplateCompetencyID == null
                    )
                    .OrderBy(
                        competency =>
                            competency.CategoryOrder
                    )
                    .ThenBy(
                        competency =>
                            competency.ItemOrder
                    )
                    .ThenBy(
                        competency =>
                            competency.CompetencyID
                    )
                    .ToListAsync();

            int totalTemplates =
                templates.Count;

            List<User> students =
                new();

            Dictionary<int, int>
                completedCountByStudent =
                    new();

            if (!isStudent)
            {
                IQueryable<User> studentsQuery =
                    _context.Users
                        .AsNoTracking()
                        .Where(
                            user =>
                                user.IsActive == 1
                                &&
                                user.UserRole != null
                                &&
                                user.UserRole.ToLower()
                                    == "student"
                        );

                if (
                    !string.IsNullOrWhiteSpace(
                        search
                    )
                )
                {
                    studentsQuery =
                        studentsQuery.Where(
                            user =>
                                (
                                    user.FullName != null
                                    &&
                                    user.FullName.Contains(
                                        search
                                    )
                                )
                                ||
                                (
                                    user.Username != null
                                    &&
                                    user.Username.Contains(
                                        search
                                    )
                                )
                                ||
                                (
                                    user.Email != null
                                    &&
                                    user.Email.Contains(
                                        search
                                    )
                                )
                        );
                }

                students =
                    await studentsQuery
                        .OrderBy(
                            user =>
                                user.FullName
                        )
                        .ThenBy(
                            user =>
                                user.Username
                        )
                        .ToListAsync();

                List<int> studentIds =
                    students
                        .Select(
                            student =>
                                student.UserID
                        )
                        .ToList();

                completedCountByStudent =
                    await _context.Competency
                        .AsNoTracking()
                        .Where(
                            competency =>
                                competency.StudentUserID
                                    .HasValue
                                &&
                                studentIds.Contains(
                                    competency.StudentUserID
                                        .Value
                                )
                                &&
                                competency.TemplateCompetencyID
                                    .HasValue
                                &&
                                competency.IsCompleted
                        )
                        .GroupBy(
                            competency =>
                                competency.StudentUserID
                                    .Value
                        )
                        .Select(
                            group =>
                                new
                                {
                                    StudentUserID =
                                        group.Key,

                                    CompletedCount =
                                        group.Count()
                                }
                        )
                        .ToDictionaryAsync(
                            item =>
                                item.StudentUserID,

                            item =>
                                item.CompletedCount
                        );

                /*
                 * فلترة حالة الإنجاز.
                 */
                if (status != "all")
                {
                    students =
                        students
                            .Where(
                                student =>
                                {
                                    int completedCount =
                                        completedCountByStudent
                                            .TryGetValue(
                                                student.UserID,
                                                out int savedCount
                                            )
                                                ? Math.Min(
                                                    savedCount,
                                                    totalTemplates
                                                )
                                                : 0;

                                    bool completed =
                                        totalTemplates > 0
                                        &&
                                        completedCount
                                            >= totalTemplates;

                                    bool inProgress =
                                        completedCount > 0
                                        &&
                                        !completed;

                                    bool notStarted =
                                        completedCount == 0;

                                    return status switch
                                    {
                                        "completed" =>
                                            completed,

                                        "inprogress" =>
                                            inProgress,

                                        "notstarted" =>
                                            notStarted,

                                        _ => true
                                    };
                                }
                            )
                            .ToList();
                }
            }

            User? selectedStudent =
                null;

            List<Competency> studentRecords =
                new();

            if (
                studentId.HasValue
                &&
                studentId.Value > 0
            )
            {
                selectedStudent =
                    await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            user =>
                                user.UserID
                                    == studentId.Value
                                &&
                                user.IsActive == 1
                                &&
                                user.UserRole != null
                                &&
                                user.UserRole.ToLower()
                                    == "student"
                        );

                if (selectedStudent == null)
                {
                    TempData["CompetencyError"] =
                        "Student was not found.";

                    if (isStudent)
                    {
                        return RedirectToAction(
                            "Login",
                            "Account"
                        );
                    }

                    studentId =
                        null;
                }
                else
                {
                    studentRecords =
                        await _context.Competency
                            .AsNoTracking()
                            .Where(
                                competency =>
                                    competency.StudentUserID
                                        == selectedStudent.UserID
                                    &&
                                    competency.TemplateCompetencyID
                                        .HasValue
                            )
                            .OrderBy(
                                competency =>
                                    competency.CategoryOrder
                            )
                            .ThenBy(
                                competency =>
                                    competency.ItemOrder
                            )
                            .ToListAsync();
                }
            }

            ViewBag.UserRole =
                userRole;

            ViewBag.IsStudent =
                isStudent;

            ViewBag.CanEdit =
                canEdit;

            ViewBag.Search =
                search;

            ViewBag.Status =
                status;

            ViewBag.Students =
                students;

            ViewBag.CompletedCountByStudent =
                completedCountByStudent;

            ViewBag.TotalTemplates =
                totalTemplates;

            ViewBag.SelectedStudent =
                selectedStudent;

            ViewBag.StudentRecords =
                studentRecords;

            return View(templates);
        }

        // =====================================================
        // Toggle competency for one student
        //
        // Only:
        // Admin
        // Fulltime Supervisor
        // Parttime Supervisor
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthFilter(
            "Admin",
            "Fulltime Supervisor",
            "Parttime Supervisor"
        )]
        public async Task<IActionResult> Toggle(
            int studentUserId,
            int competencyId)
        {
            string userRole =
                HttpContext.Session.GetString("UserRole")
                ?? string.Empty;

            if (!CanEditCompetencies(userRole))
            {
                return Json(
                    new
                    {
                        success = false,
                        message =
                            "You are not allowed to modify competencies."
                    }
                );
            }

            bool studentExists =
                await _context.Users
                    .AsNoTracking()
                    .AnyAsync(
                        user =>
                            user.UserID
                                == studentUserId
                            &&
                            user.IsActive == 1
                            &&
                            user.UserRole != null
                            &&
                            user.UserRole.ToLower()
                                == "student"
                    );

            if (!studentExists)
            {
                return Json(
                    new
                    {
                        success = false,
                        message =
                            "Student was not found."
                    }
                );
            }

            Competency? template =
                await _context.Competency
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        competency =>
                            competency.CompetencyID
                                == competencyId
                            &&
                            competency.PatientID
                                == null
                            &&
                            competency.StudentUserID
                                == null
                            &&
                            competency.TemplateCompetencyID
                                == null
                    );

            if (template == null)
            {
                return Json(
                    new
                    {
                        success = false,
                        message =
                            "Competency template was not found."
                    }
                );
            }

            Competency? record =
                await _context.Competency
                    .FirstOrDefaultAsync(
                        competency =>
                            competency.StudentUserID
                                == studentUserId
                            &&
                            competency.TemplateCompetencyID
                                == competencyId
                    );

            DateTime now =
                DateTime.Now;

            string updatedBy =
                GetCurrentUserName();

            if (record == null)
            {
                record =
                    new Competency
                    {
                        CategoryName =
                            template.CategoryName,

                        CategoryOrder =
                            template.CategoryOrder,

                        ItemName =
                            template.ItemName,

                        ItemOrder =
                            template.ItemOrder,

                        PatientID =
                            null,

                        StudentUserID =
                            studentUserId,

                        TemplateCompetencyID =
                            template.CompetencyID,

                        IsCompleted =
                            true,

                        CompletedDate =
                            now,

                        UpdatedBy =
                            updatedBy,

                        UpdatedDate =
                            now
                    };

                _context.Competency.Add(record);
            }
            else
            {
                record.IsCompleted =
                    !record.IsCompleted;

                record.CompletedDate =
                    record.IsCompleted
                        ? now
                        : null;

                record.UpdatedBy =
                    updatedBy;

                record.UpdatedDate =
                    now;
            }

            await _context.SaveChangesAsync();

            int totalItemsCount =
                await _context.Competency
                    .AsNoTracking()
                    .CountAsync(
                        competency =>
                            competency.PatientID == null
                            &&
                            competency.StudentUserID == null
                            &&
                            competency.TemplateCompetencyID == null
                    );

            int completedCount =
                await _context.Competency
                    .AsNoTracking()
                    .CountAsync(
                        competency =>
                            competency.StudentUserID
                                == studentUserId
                            &&
                            competency.TemplateCompetencyID
                                .HasValue
                            &&
                            competency.IsCompleted
                    );

            completedCount =
                totalItemsCount > 0
                    ? Math.Min(
                        completedCount,
                        totalItemsCount
                    )
                    : 0;

            int percentage =
                totalItemsCount > 0
                    ? (int)Math.Round(
                        (double)completedCount
                        / totalItemsCount
                        * 100
                    )
                    : 0;

            return Json(
                new
                {
                    success = true,

                    isCompleted =
                        record.IsCompleted,

                    completedDate =
                        record.CompletedDate?
                            .ToString(
                                "yyyy-MM-dd"
                            ),

                    updatedBy =
                        record.UpdatedBy,

                    totalCompleted =
                        completedCount,

                    totalItems =
                        totalItemsCount,

                    percentage
                }
            );
        }
    }
}
