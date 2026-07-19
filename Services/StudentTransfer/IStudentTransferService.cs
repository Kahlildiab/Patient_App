using DentalCollegeManagementSystem_AAU.Models.StudentTransfer;

namespace DentalCollegeManagementSystem_AAU.Services.StudentTransfer;

public interface IStudentTransferService
{
    Task<StudentTransferPageViewModel> GetPageAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TransferOperationResult> TransferAsync(
        CancellationToken cancellationToken = default);
}
