using System.Collections.Generic;

namespace Clinic.Application.DTOs;

public class CdsEvaluationRequestDto
{
    public string? PatientId { get; set; }
    public List<string> PrescribedMolecules { get; set; } = new();
    public List<string>? PatientChronicMedications { get; set; }
    public List<string>? PatientChronicConditions { get; set; }
    public double? PatientWeightKg { get; set; }
    public int? PatientAgeYears { get; set; }
}

public class CdsAlertDto
{
    public string Severity { get; set; } = "Critical"; // "Critical", "Moderate", "Minor"
    public string AlertType { get; set; } = "DrugDrugInteraction"; // "DrugDrugInteraction", "DiseaseContraindication", "AllergyConflict"
    public string DrugA { get; set; } = string.Empty;
    public string DrugB { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ClinicalEffect { get; set; } = string.Empty;
    public string SuggestedAlternative { get; set; } = string.Empty;
}

public class PediatricDosingSuggestionDto
{
    public string DrugName { get; set; } = string.Empty;
    public double RecommendedMgPerKg { get; set; }
    public double CalculatedDoseMg { get; set; }
    public double MaxAdultDoseMg { get; set; }
    public double FinalCappedDoseMg { get; set; }
    public string DosingInterval { get; set; } = "Every 8 hours (TID)";
}

public class CdsEvaluationResponseDto
{
    public bool IsSafe { get; set; } = true;
    public bool HasCriticalAlerts { get; set; } = false;
    public int TotalAlertsCount { get; set; }
    public List<CdsAlertDto> Alerts { get; set; } = new();
    public List<PediatricDosingSuggestionDto> PediatricSuggestions { get; set; } = new();
}
