namespace DentalCollegeManagementSystem_AAU.Models.StudentTransfer;

public sealed class StudentTransferPageViewModel
{
    public IReadOnlyList<OracleStudentDto> Students { get; init; } =
        Array.Empty<OracleStudentDto>();

    public string Search { get; init; } = string.Empty;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public int TotalItems { get; init; }

    public int TotalPages { get; init; }

    public int OracleTotalCount { get; init; }

    public int ExistingCount { get; init; }

    public int NewCount { get; init; }

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    public int FirstItemNumber =>
        TotalItems == 0 ? 0 : ((Page - 1) * PageSize) + 1;

    public int LastItemNumber =>
        Math.Min(Page * PageSize, TotalItems);
}
