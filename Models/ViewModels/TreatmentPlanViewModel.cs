// ViewModels/TreatmentPlanViewModel.cs
using DentalCollegeManagementSystem_AAU.Models;

public class TreatmentPlanViewModel
{
    public int PatientId { get; set; }
    public List<TreatmentProcedure> Procedures { get; set; } = new();

    // Grouped by category
    public List<TreatmentProcedure> EmergencyProcedures =>
        Procedures.Where(p => p.Category == TreatmentCategory.EmergencyTreatment).ToList();

    public List<TreatmentProcedure> StabilizationProcedures =>
        Procedures.Where(p => p.Category == TreatmentCategory.StabilizationTreatment).ToList();

    public List<TreatmentProcedure> DefinitiveProcedures =>
        Procedures.Where(p => p.Category == TreatmentCategory.DefinitiveTreatment).ToList();

    public List<TreatmentProcedure> MaintenanceProcedures =>
        Procedures.Where(p => p.Category == TreatmentCategory.MaintenanceTreatment).ToList();

    // For Add form
    public TreatmentProcedure NewProcedure { get; set; } = new();
}