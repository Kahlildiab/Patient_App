namespace DentalCollegeManagementSystem_AAU.Models.StudentTransfer;

public sealed class TransferOperationResult
{
    public int SourceCount { get; init; }

    public int InsertedCount { get; set; }

    public int UpdatedCount { get; set; }
}
