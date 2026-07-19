namespace DentalCollegeManagementSystem_AAU.Models.StudentTransfer;

public sealed class OracleStudentDto
{
    public string Username { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string LevelDescription { get; init; } = string.Empty;

    public bool IsTransferred { get; set; }
}
