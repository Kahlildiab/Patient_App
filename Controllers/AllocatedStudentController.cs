using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Filters;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    /*
     * Index أصبح صفحة إدارية لعرض الطلاب ومعلومات الإسناد.
     * بقية Actions بقيت كما هي للحفاظ على منطق الإسناد والتاريخ الحالي.
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
        // Index - Students table
        // =====================================================
        [AuthFilter(
            "Admin",
            "Fulltime Supervisor"
        )]
        public async Task<IActionResult> Index(
            string? searchName,
            string? studentNo,
            string? email,
            string? allocationStatus,
            int page = 1
        )
        {
            const int pageSize = 10;

            // Keep system Student users synchronized with AppUsers.
            List<int> activeStudentAppUserIds =
                await EnsureAndGetActiveStudentAppUserIdsAsync();

            var query =
                _db.AppUsers
                    .AsNoTracking()
                    .Where(
                        appUser =>
                            activeStudentAppUserIds.Contains(appUser.Id)
                    );

            string normalizedName = NormalizeValue(searchName);
            string normalizedStudentNo = NormalizeValue(studentNo);
            string normalizedEmail = NormalizeValue(email);

            if (!string.IsNullOrWhiteSpace(normalizedName))
            {
                query = query.Where(
                    appUser =>
                        (
                            appUser.NameEn != null
                            && appUser.NameEn.ToLower().Contains(normalizedName)
                        )
                        ||
                        (
                            appUser.NameAr != null
                            && appUser.NameAr.ToLower().Contains(normalizedName)
                        )
                );
            }

            if (!string.IsNullOrWhiteSpace(normalizedStudentNo))
            {
                query = query.Where(
                    appUser =>
                        appUser.UserLog != null
                        && appUser.UserLog
                            .ToLower()
                            .Contains(normalizedStudentNo)
                );
            }

            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                query = query.Where(
                    appUser =>
                        appUser.Email != null
                        && appUser.Email
                            .ToLower()
                            .Contains(normalizedEmail)
                );
            }

            if (
                string.Equals(
                    allocationStatus,
                    "Assigned",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                query = query.Where(
                    appUser =>
                        _db.AllocatedStudents.Any(
                            allocation =>
                                allocation.AppUserId == appUser.Id
                                && allocation.IsActive
                        )
                );
            }
            else if (
                string.Equals(
                    allocationStatus,
                    "Unassigned",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                query = query.Where(
                    appUser =>
                        !_db.AllocatedStudents.Any(
                            allocation =>
                                allocation.AppUserId == appUser.Id
                                && allocation.IsActive
                        )
                );
            }

            int totalItems = await query.CountAsync();

            int totalPages =
                (int)Math.Ceiling(
                    (double)totalItems / pageSize
                );

            if (page < 1)
            {
                page = 1;
            }

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var students =
                await query
                    .OrderBy(
                        appUser =>
                            appUser.NameEn
                    )
                    .ThenBy(
                        appUser =>
                            appUser.UserLog
                    )
                    .Skip(
                        (page - 1) * pageSize
                    )
                    .Take(pageSize)
                    .Select(
                        appUser =>
                            new AllocatedStudentRowViewModel
                            {
                                AppUserId = appUser.Id,

                                StudentNumber =
                                    appUser.UserLog ?? string.Empty,

                                FullName =
                                    appUser.NameEn == null
                                    || appUser.NameEn == ""
                                        ? appUser.UserLog ?? string.Empty
                                        : appUser.NameEn,

                                Email =
                                    appUser.Email ?? string.Empty,

                                PhoneNumber =
                                    appUser.Mobile ?? string.Empty,

                                AccountStatus =
                                    appUser.Status == null
                                    || appUser.Status == ""
                                        ? "Active"
                                        : appUser.Status
                            }
                    )
                    .ToListAsync();

            var pageStudentIds =
                students
                    .Select(student => student.AppUserId)
                    .ToList();

            var patientCounts =
                await _db.AllocatedStudents
                    .AsNoTracking()
                    .Where(
                        allocation =>
                            pageStudentIds.Contains(allocation.AppUserId)
                            && allocation.IsActive
                    )
                    .GroupBy(
                        allocation =>
                            allocation.AppUserId
                    )
                    .Select(
                        group => new
                        {
                            AppUserId = group.Key,
                            Count = group
                                .Select(allocation => allocation.PatientID)
                                .Distinct()
                                .Count()
                        }
                    )
                    .ToDictionaryAsync(
                        item => item.AppUserId,
                        item => item.Count
                    );

            foreach (var student in students)
            {
                student.AssignedPatientsCount =
                    patientCounts.TryGetValue(
                        student.AppUserId,
                        out int count
                    )
                        ? count
                        : 0;
            }

            int totalActiveStudents = activeStudentAppUserIds.Count;

            int totalAssignedStudents =
                await _db.AllocatedStudents
                    .AsNoTracking()
                    .Where(
                        allocation =>
                            activeStudentAppUserIds.Contains(allocation.AppUserId)
                            && allocation.IsActive
                    )
                    .Select(
                        allocation =>
                            allocation.AppUserId
                    )
                    .Distinct()
                    .CountAsync();

            var model =
                new AllocatedStudentIndexViewModel
                {
                    Students = students,
                    SearchName = searchName?.Trim() ?? string.Empty,
                    StudentNo = studentNo?.Trim() ?? string.Empty,
                    Email = email?.Trim() ?? string.Empty,
                    AllocationStatus = allocationStatus?.Trim() ?? string.Empty,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalItems = totalItems,
                    PageSize = pageSize,
                    TotalActiveStudents = totalActiveStudents,
                    TotalAssignedStudents = totalAssignedStudents,
                    TotalUnassignedStudents =
                        Math.Max(
                            0,
                            totalActiveStudents - totalAssignedStudents
                        )
                };

            return View(model);
        }

        // =====================================================
        // Get active patients assigned to one student - Admin only
        // =====================================================
        [HttpGet]
        [AuthFilter(
            "Admin",
            "Fulltime Supervisor"
        )]
        public async Task<IActionResult> GetStudentPatients(
            int appUserId
        )
        {
            List<int> activeStudentAppUserIds =
                await EnsureAndGetActiveStudentAppUserIdsAsync();

            if (!activeStudentAppUserIds.Contains(appUserId))
            {
                return NotFound(
                    new
                    {
                        success = false,
                        message = "Student not found."
                    }
                );
            }

            var student =
                await _db.AppUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        appUser => appUser.Id == appUserId
                    );

            if (student == null)
            {
                return NotFound(
                    new
                    {
                        success = false,
                        message = "Student not found."
                    }
                );
            }

            var patientRows =
                await (
                    from allocation in _db.AllocatedStudents.AsNoTracking()
                    join patient in _db.Patients.AsNoTracking()
                        on allocation.PatientID equals patient.PatientID
                    where
                        allocation.AppUserId == appUserId
                        && allocation.IsActive
                    orderby allocation.AssignedDate descending
                    select new
                    {
                        patientId = patient.PatientID,
                        name =
                            ((patient.FirstName ?? "") + " " +
                             (patient.SecondName ?? "") + " " +
                             (patient.ThirdName ?? "") + " " +
                             (patient.FourthName ?? "")).Trim(),
                        nationalId =
                            patient.NationalID_PassportNumber ?? "",
                        gender = patient.Gender ?? "",
                        status =
                            string.IsNullOrWhiteSpace(patient.PatientStatus)
                                ? "Screening"
                                : patient.PatientStatus,
                        assignedDate = allocation.AssignedDate
                    }
                )
                .ToListAsync();

            var patients =
                patientRows
                    .GroupBy(patient => patient.patientId)
                    .Select(group => group.First())
                    .Select(
                        patient => new
                        {
                            patient.patientId,
                            patient.name,
                            patient.nationalId,
                            patient.gender,
                            patient.status,
                            assignedDate =
                                patient.assignedDate.ToString("dd/MM/yyyy HH:mm")
                        }
                    )
                    .ToList();

            return Json(
                new
                {
                    success = true,
                    studentName =
                        string.IsNullOrWhiteSpace(student.NameEn)
                            ? student.UserLog
                            : student.NameEn,
                    count = patients.Count,
                    patients
                }
            );
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
                            && allocation.IsActive
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
        // Status and student-allocation history
        // =====================================================
        [HttpGet]
        [AuthFilter(
            "Admin",
            "Fulltime Supervisor",
            "Parttime Supervisor",
            "Student"
        )]
        public async Task<IActionResult> GetStatusHistory(
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

            var statusHistory =
                await _db.PatientStatusHistories
                    .AsNoTracking()
                    .Where(
                        historyItem =>
                            historyItem.PatientID == patientId
                    )
                    .OrderBy(
                        historyItem =>
                            historyItem.ChangedAt
                    )
                    .Select(
                        historyItem => new
                        {
                            historyItem.OldStatus,
                            historyItem.NewStatus,
                            historyItem.ChangedAt
                        }
                    )
                    .ToListAsync();

            var allocationHistory =
                await (
                    from allocation in
                        _db.AllocatedStudents.AsNoTracking()

                    join appUser in
                        _db.AppUsers.AsNoTracking()
                        on allocation.AppUserId equals appUser.Id

                    where allocation.PatientID == patientId

                    orderby allocation.AssignedDate

                    select new
                    {
                        allocation.AssignedDate,
                        allocation.RemovedDate,
                        allocation.IsActive,
                        StudentName =
                            string.IsNullOrWhiteSpace(appUser.NameEn)
                                ? appUser.UserLog
                                : appUser.NameEn
                    }
                )
                .ToListAsync();

            var result = new List<object>();

            for (int index = 0; index < statusHistory.Count; index++)
            {
                var current = statusHistory[index];

                DateTime intervalStart =
                    index == 0
                        ? DateTime.MinValue
                        : statusHistory[index - 1].ChangedAt;

                DateTime intervalEnd = current.ChangedAt;

                string lastAssignedStudent = "—";
                string newAssignedStudent = "—";

                if (
                    string.Equals(
                        current.NewStatus,
                        "Allocated",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    var newlyAssignedNames =
                        allocationHistory
                            .Where(
                                allocation =>
                                    allocation.AssignedDate > intervalStart
                                    &&
                                    allocation.AssignedDate
                                        <= intervalEnd.AddMinutes(5)
                            )
                            .Select(
                                allocation =>
                                    allocation.StudentName
                            )
                            .Where(
                                name =>
                                    !string.IsNullOrWhiteSpace(name)
                            )
                            .Distinct()
                            .ToList();

                    if (!newlyAssignedNames.Any())
                    {
                        newlyAssignedNames =
                            allocationHistory
                                .Where(
                                    allocation =>
                                        allocation.AssignedDate
                                            <= intervalEnd.AddMinutes(5)
                                        &&
                                        (
                                            allocation.IsActive
                                            ||
                                            !allocation.RemovedDate.HasValue
                                            ||
                                            allocation.RemovedDate.Value
                                                > intervalEnd
                                        )
                                )
                                .OrderByDescending(
                                    allocation =>
                                        allocation.AssignedDate
                                )
                                .Select(
                                    allocation =>
                                        allocation.StudentName
                                )
                                .Where(
                                    name =>
                                        !string.IsNullOrWhiteSpace(name)
                                )
                                .Distinct()
                                .ToList();
                    }

                    if (newlyAssignedNames.Any())
                    {
                        newAssignedStudent =
                            string.Join(", ", newlyAssignedNames);
                    }
                }

                if (
                    string.Equals(
                        current.NewStatus,
                        "Screening",
                        StringComparison.OrdinalIgnoreCase
                    )
                    &&
                    string.Equals(
                        current.OldStatus,
                        "Allocated",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    var removedNames =
                        allocationHistory
                            .Where(
                                allocation =>
                                    allocation.RemovedDate.HasValue
                                    &&
                                    allocation.RemovedDate.Value
                                        > intervalStart
                                    &&
                                    allocation.RemovedDate.Value
                                        <= intervalEnd.AddMinutes(5)
                            )
                            .OrderByDescending(
                                allocation =>
                                    allocation.RemovedDate
                            )
                            .Select(
                                allocation =>
                                    allocation.StudentName
                            )
                            .Where(
                                name =>
                                    !string.IsNullOrWhiteSpace(name)
                            )
                            .Distinct()
                            .ToList();

                    if (!removedNames.Any())
                    {
                        removedNames =
                            allocationHistory
                                .Where(
                                    allocation =>
                                        allocation.RemovedDate.HasValue
                                        &&
                                        allocation.RemovedDate.Value
                                            <= intervalEnd.AddMinutes(5)
                                )
                                .OrderByDescending(
                                    allocation =>
                                        allocation.RemovedDate
                                )
                                .Take(1)
                                .Select(
                                    allocation =>
                                        allocation.StudentName
                                )
                                .Where(
                                    name =>
                                        !string.IsNullOrWhiteSpace(name)
                                )
                                .ToList();
                    }

                    if (removedNames.Any())
                    {
                        lastAssignedStudent =
                            string.Join(", ", removedNames);
                    }
                }

                result.Add(
                    new
                    {
                        oldStatus = current.OldStatus,
                        newStatus = current.NewStatus,
                        lastAssignedStudent,
                        newAssignedStudent,
                        changedAt =
                            current.ChangedAt.ToString(
                                "yyyy-MM-dd HH:mm"
                            )
                    }
                );
            }

            var currentlyAssignedStudents =
                allocationHistory
                    .Where(
                        allocation =>
                            allocation.IsActive
                    )
                    .Select(
                        allocation =>
                            allocation.StudentName
                    )
                    .Where(
                        name =>
                            !string.IsNullOrWhiteSpace(name)
                    )
                    .Distinct()
                    .ToList();

            return Json(
                new
                {
                    success = true,
                    history = result
                        .AsEnumerable()
                        .Reverse()
                        .ToList(),
                    currentlyAssignedStudents
                }
            );
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
                            &&
                            allocation.IsActive
                    );

            if (!allocationExists)
            {
                _db.AllocatedStudents.Add(
                    new AllocatedStudent
                    {
                        PatientID = patientId,
                        AppUserId = appUserId,
                        AssignedDate = DateTime.Now,
                        IsActive = true,
                        RemovedDate = null
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
        // Remove student (soft delete)
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
                            currentAllocation.PatientID == patientId
                            &&
                            currentAllocation.AppUserId == appUserId
                            &&
                            currentAllocation.IsActive
                    );

            if (allocation != null)
            {
                allocation.IsActive = false;
                allocation.RemovedDate = DateTime.Now;

                await _db.SaveChangesAsync();
            }

            /*
             * لا نغيّر حالة المريض تلقائياً هنا.
             * عند الضغط على Screening يجب إزالة جميع الطلاب،
             * ثم الضغط على Confirm Screening من نافذة الحالة.
             */
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