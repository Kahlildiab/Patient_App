namespace DentalCollegeManagementSystem_AAU.Controllers
{
    internal class DentalChartHistoryDto
    {
        public int Id { get; set; }
        public string SessionNote { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public string? ReportJson { get; internal set; }
        public string? BpeJson { get; internal set; }
    }
}