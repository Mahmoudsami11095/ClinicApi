using System.Collections.Generic;

namespace Clinic.Application.DTOs;

public class ClinicalFindingDto
{
    public string ToothNumber { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Severity { get; set; } = "Moderate";
}

public class DraftPrescriptionDto
{
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
}

public class ClinicalSoapNoteDto
{
    public string ChiefComplaint { get; set; } = string.Empty;
    public string HistoryOfPresentIllness { get; set; } = string.Empty;
    public string Assessment { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    
    public List<ClinicalFindingDto> OdontogramFindings { get; set; } = new();
    public List<DraftPrescriptionDto> DraftPrescriptions { get; set; } = new();

    public string RawTranscript { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
}
