namespace DentalCollegeManagementSystem_AAU.Models
{
    public class CreateConditionsViewModel
    {
        public int PatientID { get; set; }
        public List<ConditionItemViewModel> Conditions { get; set; } = new() { new ConditionItemViewModel() };
    }

    public class ConditionItemViewModel
    {
        public string? ConditionName { get; set; }
        public int? YearOfDiagnosis { get; set; }
        public bool IsCurrentlyActive { get; set; } = true;
        public bool IsFlagged { get; set; } = false;  

        public string? Notes { get; set; }
    }
}