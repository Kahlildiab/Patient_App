using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Filters;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    /*
     * Student يستطيع دخول Index لمشاهدة المرضى المسندين إليه فقط.
     * أما الإسناد والحذف وجلب قائمة الطلاب فهي محمية على مستوى كل Action.
     */
    [AuthFilter(
        "Admin",
        "Fulltime Supervisor",
        "Parttime Supervisor",
        "Student"
    )]
    public class AllocatedStudentController : Controller
    {
        private readonly AppDbContext _db;

        public AllocatedStudentController(AppDbContext db)
        {
            _db = db;
        }

        // =====================================================
        // Index
        // =====================================================
        public async Task<IActionResult> Index()
        {
            string userRole =
                HttpContext.Session.GetString("UserRole")
                ?? string.Empty;

            List<Patient> patients;

            if (
                string.Equals(
                    userRole,
                    "Student",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                /*
                 * نحاول حل AppUserID حتى لو لم يكن موجوداً في Session.
                 * يتم الربط باستخدام Username أو Email.
                 */
                int appUserId =
                    await ResolveCurrentStudentAppUserIdAsync();

                if (appUserId == 0)
                {
                    TempData["Error"] =
                        "Your student account could not be linked to AppUsers.";

                    ViewBag.CaseComplexityByPatient =
                        new Dictionary<int, string>();

                    return View(
                        new List<Patient>()
                    );
                }

                var assignedPatientIds =
                    await _db.AllocatedStudents
                        .AsNoTracking()
                        .Where(
                            allocation =>
                                allocation.AppUserId == appUserId
                        )
                        .Select(
                            allocation =>
                                allocation.PatientID
                        )
                        .Distinct()
                        .ToListAsync();

                /*
                 * الطالب يشاهد فقط المرضى المسندين إليه
                 * والذين حالتهم Allocated.
                 */
                patients =
                    await _db.Patients
                        .AsNoTracking()
                        .Include(
                            patient =>
                                patient.Status
                        )
                        .Where(
                            patient =>
                                assignedPatientIds.Contains(
                                    patient.PatientID
                                )
                                &&
                                patient.PatientStatus == "Allocated"
                                &&
                                patient.StatusID != 6
                        )
                        .OrderBy(
                            patient =>
                                patient.FirstName
                        )
                        .ThenBy(
                            patient =>
                                patient.FourthName
                        )
                        .ToListAsync();
            }
            else
            {
                /*
                 * Admin وSupervisors يشاهدون جميع المرضى
                 * باستثناء المرضى المرفوضين.
                 */
                patients =
                    await _db.Patients
                        .AsNoTracking()
                        .Include(
                            patient =>
                                patient.Status
                        )
                        .Where(
                            patient =>
                                patient.StatusID != 6
                        )
                        .OrderBy(
                            patient =>
                                patient.FirstName
                        )
                        .ThenBy(
                            patient =>
                                patient.FourthName
                        )
                        .ToListAsync();
            }

            /*
             * Case Complexity محفوظة داخل جدول Visits.
             *
             * نعتمد أول قيمة غير فارغة للمريض، وهو نفس
             * المنطق المستخدم داخل صفحة Patient Details.
             */
            var patientIds =
                patients
                    .Select(
                        patient =>
                            patient.PatientID
                    )
                    .Distinct()
                    .ToList();

            var caseComplexityByPatient =
                new Dictionary<int, string>();

            if (patientIds.Any())
            {
                var complexityRows =
                    await _db.Visits
                        .AsNoTracking()
                        .Where(
                            visit =>
                                patientIds.Contains(
                                    visit.PatientID
                                )
                                &&
                                visit.CaseComplexity != null
                                &&
                                visit.CaseComplexity != ""
                        )
                        .OrderBy(
                            visit =>
                                visit.VisitDate
                        )
                        .ThenBy(
                            visit =>
                                visit.VisitID
                        )
                        .Select(
                            visit => new
                            {
                                visit.PatientID,
                                visit.CaseComplexity
                            }
                        )
                        .ToListAsync();

                caseComplexityByPatient =
                    complexityRows
                        .Where(
                            row =>
                                !string.IsNullOrWhiteSpace(
                                    row.CaseComplexity
                                )
                        )
                        .GroupBy(
                            row =>
                                row.PatientID
                        )
                        .ToDictionary(
                            group =>
                                group.Key,

                            group =>
                                group
                                    .First()
                                    .CaseComplexity!
                                    .Trim()
                        );
            }

            ViewBag.CaseComplexityByPatient =
                caseComplexityByPatient;

            return View(patients);
        }

        // =====================================================
        // Get assigned and available students
        // =====================================================
        [HttpGet]
        [AuthFilter(
            "Admin",
            "Fulltime Supervisor",
            "Parttime Supervisor"
        )]
        public async Task<IActionResult> GetPatientDetailsJson(
            int patientId
        )
        {
            bool patientExists =
                await _db.Patients
                    .AsNoTracking()
                    .AnyAsync(
                        patient =>
                            patient.PatientID == patientId
                    );

            if (!patientExists)
            {
                return NotFound(
                    new
                    {
                        success = false,
                        message = "Patient not found."
                    }
                );
            }

            /*
             * مزامنة جميع حسابات Users التي دورها Student
             * مع جدول AppUsers، ثم الحصول على AppUser IDs
             * للطلاب الفعالين.
             */
            List<int> activeStudentAppUserIds =
                await EnsureAndGetActiveStudentAppUserIdsAsync();

            /*
             * الطلاب المسندون للمريض الحالي فقط.
             */
            var assignedIds =
                await _db.AllocatedStudents
                    .AsNoTracking()
                    .Where(
                        allocation =>
                            allocation.PatientID == patientId
                    )
                    .Select(
                        allocation =>
                            allocation.AppUserId
                    )
                    .Distinct()
                    .ToListAsync();

            /*
             * جميع الطلاب الفعالين الموجودين في جدول Users
             * بعد ربطهم بسجلات AppUsers.
             */
            var allStudents =
                await _db.AppUsers
                    .AsNoTracking()
                    .Where(
                        appUser =>
                            activeStudentAppUserIds.Contains(
                                appUser.Id
                            )
                    )
                    .Select(
                        appUser => new
                        {
                            id = appUser.Id,

                            nameEn =
                                string.IsNullOrWhiteSpace(
                                    appUser.NameEn
                                )
                                    ? appUser.UserLog
                                    : appUser.NameEn,

                            userTypeEn = "Student",

                            status =
                                string.IsNullOrWhiteSpace(
                                    appUser.Status
                                )
                                    ? "Active"
                                    : appUser.Status
                        }
                    )
                    .OrderBy(
                        student =>
                            student.nameEn
                    )
                    .ToListAsync();

            /*
             * الطلاب المسندون لهذا المريض.
             */
            var assigned =
                allStudents
                    .Where(
                        student =>
                            assignedIds.Contains(
                                student.id
                            )
                    )
                    .ToList();

            /*
             * كل الطلاب غير المسندين لهذا المريض.
             *
             * الطالب المسند لمريض آخر يبقى ظاهراً هنا،
             * ويمكن إسناده لهذا المريض أيضاً.
             */
            var available =
                allStudents
                    .Where(
                        student =>
                            !assignedIds.Contains(
                                student.id
                            )
                    )
                    .ToList();

            return Json(
                new
                {
                    success = true,
                    assigned,
                    available,
                    totalStudents = allStudents.Count,
                    assignedCount = assigned.Count,
                    availableCount = available.Count
                }
            );
        }

        // =====================================================
        // Status history
        // =====================================================
        [HttpGet]
        [AuthFilter(
            "Admin",
            "Fulltime Supervisor",
            "Parttime Supervisor"
        )]
        public async Task<IActionResult> GetStatusHistory(
            int patientId
        )
        {
            var history =
                await _db.PatientStatusHistories
                    .AsNoTracking()
                    .Where(
                        historyItem =>
                            historyItem.PatientID == patientId
                    )
                    .OrderByDescending(
                        historyItem =>
                            historyItem.ChangedAt
                    )
                    .Select(
                        historyItem => new
                        {
                            oldStatus =
                                historyItem.OldStatus,

                            newStatus =
                                historyItem.NewStatus,

                            changedAt =
                                historyItem.ChangedAt
                                    .ToString(
                                        "yyyy-MM-dd  HH:mm"
                                    )
                        }
                    )
                    .ToListAsync();

            return Json(history);
        }

        // =====================================================
        // Assign student
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthFilter(
            "Admin",
            "Fulltime Supervisor",
            "Parttime Supervisor"
        )]
        public async Task<IActionResult> Assign(
            int patientId,
            int appUserId
        )
        {
            var patient =
                await _db.Patients
                    .FirstOrDefaultAsync(
                        currentPatient =>
                            currentPatient.PatientID
                                == patientId
                    );

            if (patient == null)
            {
                return NotFound(
                    new
                    {
                        success = false,
                        message = "Patient not found."
                    }
                );
            }

            /*
             * نسمح فقط بإسناد مستخدم موجود فعلياً
             * كطالب فعال في جدول Users.
             */
            List<int> activeStudentAppUserIds =
                await EnsureAndGetActiveStudentAppUserIdsAsync();

            if (
                !activeStudentAppUserIds.Contains(
                    appUserId
                )
            )
            {
                return BadRequest(
                    new
                    {
                        success = false,
                        message =
                            "The selected account is not an active student."
                    }
                );
            }

            bool allocationExists =
                await _db.AllocatedStudents
                    .AnyAsync(
                        allocation =>
                            allocation.PatientID
                                == patientId
                            &&
                            allocation.AppUserId
                                == appUserId
                    );

            if (!allocationExists)
            {
                _db.AllocatedStudents.Add(
                    new AllocatedStudent
                    {
                        PatientID = patientId,
                        AppUserId = appUserId,
                        AssignedDate = DateTime.Now
                    }
                );

                /*
                 * عند إسناد أول طالب للمريض:
                 * تصبح حالة المريض Allocated.
                 */
                if (
                    !string.Equals(
                        patient.PatientStatus,
                        "Allocated",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    string oldStatus =
                        string.IsNullOrWhiteSpace(
                            patient.PatientStatus
                        )
                            ? "Screening"
                            : patient.PatientStatus;

                    patient.PatientStatus =
                        "Allocated";

                    _db.PatientStatusHistories.Add(
                        new PatientStatusHistory
                        {
                            PatientID = patientId,
                            OldStatus = oldStatus,
                            NewStatus = "Allocated",
                            ChangedAt = DateTime.Now
                        }
                    );
                }

                await _db.SaveChangesAsync();
            }

            return await GetPatientDetailsJson(
                patientId
            );
        }

        // =====================================================
        // Remove student
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthFilter(
            "Admin",
            "Fulltime Supervisor",
            "Parttime Supervisor"
        )]
        public async Task<IActionResult> Remove(
            int patientId,
            int appUserId
        )
        {
            var allocation =
                await _db.AllocatedStudents
                    .FirstOrDefaultAsync(
                        currentAllocation =>
                            currentAllocation.PatientID
                                == patientId
                            &&
                            currentAllocation.AppUserId
                                == appUserId
                    );

            if (allocation != null)
            {
                _db.AllocatedStudents.Remove(
                    allocation
                );

                await _db.SaveChangesAsync();

                int remainingStudents =
                    await _db.AllocatedStudents
                        .CountAsync(
                            remainingAllocation =>
                                remainingAllocation.PatientID
                                    == patientId
                        );

                /*
                 * إذا أزيل آخر طالب من المريض،
                 * تعود حالته إلى Screening.
                 */
                if (remainingStudents == 0)
                {
                    var patient =
                        await _db.Patients
                            .FirstOrDefaultAsync(
                                currentPatient =>
                                    currentPatient.PatientID
                                        == patientId
                            );

                    if (
                        patient != null
                        &&
                        string.Equals(
                            patient.PatientStatus,
                            "Allocated",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        patient.PatientStatus =
                            "Screening";

                        _db.PatientStatusHistories.Add(
                            new PatientStatusHistory
                            {
                                PatientID = patientId,
                                OldStatus = "Allocated",
                                NewStatus = "Screening",
                                ChangedAt = DateTime.Now
                            }
                        );

                        await _db.SaveChangesAsync();
                    }
                }
            }

            return await GetPatientDetailsJson(
                patientId
            );
        }

        // =====================================================
        // Ensure all active system students exist in AppUsers
        // =====================================================
        private async Task<List<int>>
            EnsureAndGetActiveStudentAppUserIdsAsync()
        {
            /*
             * جلب أو إنشاء UserType باسم Student.
             */
            var studentType =
                await _db.UserTypes
                    .FirstOrDefaultAsync(
                        userType =>
                            userType.NameEn != null
                            &&
                            userType.NameEn
                                .ToLower() == "student"
                    );

            if (studentType == null)
            {
                studentType =
                    new UserType
                    {
                        NameEn = "Student",
                        NameAr = "طالب",
                        Status = "Active"
                    };

                _db.UserTypes.Add(studentType);

                await _db.SaveChangesAsync();
            }
            else if (
                !string.Equals(
                    studentType.Status,
                    "Active",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                studentType.Status = "Active";

                await _db.SaveChangesAsync();
            }

            /*
             * نعتبر الطالب فعالاً عندما:
             * UserRole = Student
             * IsActive = 1
             */
            var systemStudents =
                await _db.Users
                    .Where(
                        user =>
                            user.IsActive == 1
                            &&
                            user.UserRole != null
                            &&
                            user.UserRole
                                .ToLower() == "student"
                    )
                    .OrderBy(
                        user =>
                            user.FullName
                    )
                    .ToListAsync();

            var appUsers =
                await _db.AppUsers
                    .ToListAsync();

            bool hasChanges = false;

            foreach (var systemStudent in systemStudents)
            {
                string username =
                    NormalizeValue(
                        systemStudent.Username
                    );

                string email =
                    NormalizeValue(
                        systemStudent.Email
                    );

                /*
                 * الربط يكون بواسطة Username/UserLog،
                 * أو بواسطة Email عندما يكون موجوداً.
                 */
                var appUser =
                    appUsers.FirstOrDefault(
                        currentAppUser =>
                            (
                                !string.IsNullOrWhiteSpace(
                                    username
                                )
                                &&
                                string.Equals(
                                    NormalizeValue(
                                        currentAppUser.UserLog
                                    ),
                                    username,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            ||
                            (
                                !string.IsNullOrWhiteSpace(
                                    email
                                )
                                &&
                                string.Equals(
                                    NormalizeValue(
                                        currentAppUser.Email
                                    ),
                                    email,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                    );

                if (appUser == null)
                {
                    appUser =
                        new AppUser
                        {
                            UserLog =
                                systemStudent.Username
                                ?? string.Empty,

                            Email =
                                systemStudent.Email
                                ?? string.Empty,

                            NameEn =
                                !string.IsNullOrWhiteSpace(
                                    systemStudent.FullName
                                )
                                    ? systemStudent.FullName
                                    : systemStudent.Username,

                            NameAr =
                                !string.IsNullOrWhiteSpace(
                                    systemStudent.FullName
                                )
                                    ? systemStudent.FullName
                                    : systemStudent.Username,

                            Mobile =
                                systemStudent.PhoneNumber
                                ?? string.Empty,

                            UserTypeId =
                                studentType.Id,

                            Notes =
                                string.Empty,

                            Status =
                                "Active"
                        };

                    _db.AppUsers.Add(
                        appUser
                    );

                    appUsers.Add(
                        appUser
                    );

                    hasChanges = true;
                }
                else
                {
                    bool appUserChanged = false;

                    if (
                        appUser.UserTypeId
                            != studentType.Id
                    )
                    {
                        appUser.UserTypeId =
                            studentType.Id;

                        appUserChanged = true;
                    }

                    if (
                        !string.Equals(
                            appUser.Status,
                            "Active",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        appUser.Status = "Active";
                        appUserChanged = true;
                    }

                    if (
                        string.IsNullOrWhiteSpace(
                            appUser.UserLog
                        )
                    )
                    {
                        appUser.UserLog =
                            systemStudent.Username
                            ?? string.Empty;

                        appUserChanged = true;
                    }

                    if (
                        string.IsNullOrWhiteSpace(
                            appUser.Email
                        )
                        &&
                        !string.IsNullOrWhiteSpace(
                            systemStudent.Email
                        )
                    )
                    {
                        appUser.Email =
                            systemStudent.Email;

                        appUserChanged = true;
                    }

                    if (
                        string.IsNullOrWhiteSpace(
                            appUser.NameEn
                        )
                    )
                    {
                        appUser.NameEn =
                            !string.IsNullOrWhiteSpace(
                                systemStudent.FullName
                            )
                                ? systemStudent.FullName
                                : systemStudent.Username;

                        appUserChanged = true;
                    }

                    if (
                        string.IsNullOrWhiteSpace(
                            appUser.NameAr
                        )
                    )
                    {
                        appUser.NameAr =
                            !string.IsNullOrWhiteSpace(
                                systemStudent.FullName
                            )
                                ? systemStudent.FullName
                                : systemStudent.Username;

                        appUserChanged = true;
                    }

                    if (
                        string.IsNullOrWhiteSpace(
                            appUser.Mobile
                        )
                        &&
                        !string.IsNullOrWhiteSpace(
                            systemStudent.PhoneNumber
                        )
                    )
                    {
                        appUser.Mobile =
                            systemStudent.PhoneNumber;

                        appUserChanged = true;
                    }

                    if (appUserChanged)
                    {
                        hasChanges = true;
                    }
                }
            }

            if (hasChanges)
            {
                await _db.SaveChangesAsync();
            }

            /*
             * بعد الحفظ تكون IDs للسجلات الجديدة قد تولدت.
             */
            var resultIds =
                new List<int>();

            foreach (var systemStudent in systemStudents)
            {
                string username =
                    NormalizeValue(
                        systemStudent.Username
                    );

                string email =
                    NormalizeValue(
                        systemStudent.Email
                    );

                var matchingAppUser =
                    appUsers.FirstOrDefault(
                        currentAppUser =>
                            (
                                !string.IsNullOrWhiteSpace(
                                    username
                                )
                                &&
                                string.Equals(
                                    NormalizeValue(
                                        currentAppUser.UserLog
                                    ),
                                    username,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            ||
                            (
                                !string.IsNullOrWhiteSpace(
                                    email
                                )
                                &&
                                string.Equals(
                                    NormalizeValue(
                                        currentAppUser.Email
                                    ),
                                    email,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                    );

                if (
                    matchingAppUser != null
                    &&
                    matchingAppUser.Id > 0
                )
                {
                    resultIds.Add(
                        matchingAppUser.Id
                    );
                }
            }

            return resultIds
                .Distinct()
                .ToList();
        }

        // =====================================================
        // Resolve current student's AppUserID
        // =====================================================
        private async Task<int>
            ResolveCurrentStudentAppUserIdAsync()
        {
            string appUserIdString =
                HttpContext.Session.GetString(
                    "AppUserID"
                )
                ?? string.Empty;

            if (
                int.TryParse(
                    appUserIdString,
                    out int existingAppUserId
                )
                &&
                existingAppUserId > 0
            )
            {
                return existingAppUserId;
            }

            List<int> activeStudentIds =
                await EnsureAndGetActiveStudentAppUserIdsAsync();

            string username =
                NormalizeValue(
                    HttpContext.Session.GetString(
                        "Username"
                    )
                );

            string email =
                NormalizeValue(
                    HttpContext.Session.GetString(
                        "UserEmail"
                    )
                );

            var appUser =
                await _db.AppUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        currentAppUser =>
                            activeStudentIds.Contains(
                                currentAppUser.Id
                            )
                            &&
                            (
                                (
                                    !string.IsNullOrWhiteSpace(
                                        username
                                    )
                                    &&
                                    currentAppUser.UserLog
                                        .ToLower() == username
                                )
                                ||
                                (
                                    !string.IsNullOrWhiteSpace(
                                        email
                                    )
                                    &&
                                    currentAppUser.Email
                                        .ToLower() == email
                                )
                            )
                    );

            if (appUser == null)
            {
                return 0;
            }

            HttpContext.Session.SetString(
                "AppUserID",
                appUser.Id.ToString()
            );

            return appUser.Id;
        }

        // =====================================================
        // Normalize comparison values
        // =====================================================
        private static string NormalizeValue(
            string? value
        )
        {
            return string.IsNullOrWhiteSpace(
                value
            )
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }
    }
}
