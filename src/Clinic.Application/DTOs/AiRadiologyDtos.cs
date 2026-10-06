namespace Clinic.Application.DTOs;

public class BoundingBoxDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public class AiRadiologyFindingDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Caries, PeriapicalRadiolucency, BoneLoss, ThirdMolarImpaction
    public string TypeAr { get; set; } = string.Empty;
    public int ToothFdi { get; set; }
    public int ToothUniversal { get; set; }
    public string Severity { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Location { get; set; } = string.Empty;
    public BoundingBoxDto Box { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
    public string RecommendationAr { get; set; } = string.Empty;
    public bool IsAcceptedByDoctor { get; set; } = true;
}

public class AiRadiologyAnalysisResultDto
{
    public string RecordId { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
    public string ModelEngine { get; set; } = "DentalVision-YOLOv11-Ensemble";
    public double OverallConfidence { get; set; }
    public List<AiRadiologyFindingDto> Findings { get; set; } = new();
    public string SummaryReport { get; set; } = string.Empty;
    public string SummaryReportAr { get; set; } = string.Empty;
    public bool IsVerifiedByDoctor { get; set; }
}

public class SyncAiFindingsRequestDto
{
    public List<string> AcceptedFindingIds { get; set; } = new();
    public string? DoctorNotes { get; set; }
}
