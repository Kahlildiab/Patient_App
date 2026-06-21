namespace DentalCollegeManagementSystem_AAU.Controllers
{
    internal class DentalChartHistoryDto
    {
        public int Id { get; set; }
        public string SessionNote { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? ReportJson { get; internal set; }
    }
}