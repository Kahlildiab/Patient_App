using DentalCollegeManagementSystem_AAU.Models.StudentTransfer;
using DentalCollegeManagementSystem_AAU.Options;
using DentalCollegeManagementSystem_AAU.Services.StudentTransfer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DentalCollegeManagementSystem_AAU.Controllers;

/*
    إذا كان مشروعك يستخدم AuthFilter خاص بدلاً من Authorize،
    استبدل [Authorize] بالفلتر الموجود عندك.
*/
[Authorize]
public sealed class StudentTransferController : Controller
{
    private readonly IStudentTransferService _studentTransferService;
    private readonly StudentTransferOptions _options;
    private readonly ILogger<StudentTransferController> _logger;

    public StudentTransferController(
        IStudentTransferService studentTransferService,
        IOptions<StudentTransferOptions> options,
        ILogger<StudentTransferController> logger)
    {
        _studentTransferService =
            studentTransferService;

        _options =
            options.Value;

        _logger =
            logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int page = 1,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            StudentTransferPageViewModel model =
                await _studentTransferService.GetPageAsync(
                    search,
                    page,
                    pageSize ??
                    _options.DefaultPageSize,
                    cancellationToken);

            return View(model);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unable to load Oracle students.");

            ViewBag.ErrorMessage =
                "تعذر قراءة بيانات الطلاب. تأكد من اتصال Oracle واسم عمود رقم الطالب داخل appsettings.json.";

            return View(
                new StudentTransferPageViewModel
                {
                    Search =
                        search?.Trim() ??
                        string.Empty,

                    Page =
                        Math.Max(page, 1),

                    PageSize =
                        pageSize ??
                        _options.DefaultPageSize,

                    TotalPages = 1
                });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transfer(
        string? search,
        int page = 1,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            TransferOperationResult result =
                await _studentTransferService.TransferAsync(
                    cancellationToken);

            if (result.SourceCount == 0)
            {
                TempData["MessageType"] =
                    "warning";

                TempData["Message"] =
                    "لم يتم العثور على طلاب مطابقين في Oracle.";
            }
            else
            {
                TempData["MessageType"] =
                    "success";

                TempData["Message"] =
                    $"تمت العملية بنجاح. الطلاب الجدد: {result.InsertedCount}، الطلاب الذين تم تحديث بياناتهم: {result.UpdatedCount}.";
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unable to transfer Oracle students.");

            TempData["MessageType"] =
                "error";

            TempData["Message"] =
                "حدث خطأ أثناء نقل الطلاب. راجع سجل الأخطاء وتأكد من أسماء أعمدة View Doctor واتصالات قواعد البيانات.";
        }

        return RedirectToAction(
            nameof(Index),
            new
            {
                search,
                page,
                pageSize
            });
    }
}
