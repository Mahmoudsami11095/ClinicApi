using System;
using System.Collections.Generic;

namespace Clinic.Application.DTOs;

public class CephPointDto
{
    public string Id { get; set; } = string.Empty; // S, N, A, B, Pog, Me, Go, Gn, Or, Po
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public double X { get; set; } // Coordinate in percentage 0.0 - 100.0
    public double Y { get; set; }
    public double Confidence { get; set; } = 92.5;
    public bool IsManuallyAdjusted { get; set; }
}

public class CephMeasurementDto
{
    public string Parameter { get; set; } = string.Empty; // SNA, SNB, ANB, SN-GoGn, FMA, Wits
    public string NameAr { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = "°";
    public double NormMin { get; set; }
    public double NormMax { get; set; }
    public string Interpretation { get; set; } = string.Empty;
    public string InterpretationAr { get; set; } = string.Empty;
    public string ClinicalStatus { get; set; } = "Normal"; // Normal, Increased, Decreased
}

public class CephalometricAnalysisResultDto
{
    public string RecordId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string AnalysisMethod { get; set; } = "Steiner & Tweed Orthodontic Composite";
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    public string SkeletalClass { get; set; } = "Class I"; // Class I, Class II, Class III
    public string SkeletalClassAr { get; set; } = "صنف أول هيكلي (Class I)";
    public string GrowthPattern { get; set; } = "Normodivergent"; // Normodivergent, Hyperdivergent, Hypodivergent
    public string GrowthPatternAr { get; set; } = "نمط نمو طبيعي (Normodivergent)";
    public double OverallConfidence { get; set; } = 93.8;
    public List<CephPointDto> Landmarks { get; set; } = new();
    public List<CephMeasurementDto> Measurements { get; set; } = new();
    public string ClinicalSummary { get; set; } = string.Empty;
    public string ClinicalSummaryAr { get; set; } = string.Empty;
}

public class SaveCephalometricReportDto
{
    public string RecordId { get; set; } = string.Empty;
    public string AnalysisMethod { get; set; } = "Steiner & Tweed Orthodontic Composite";
    public string SkeletalClass { get; set; } = string.Empty;
    public string GrowthPattern { get; set; } = string.Empty;
    public List<CephPointDto> Landmarks { get; set; } = new();
    public List<CephMeasurementDto> Measurements { get; set; } = new();
    public string? DoctorNotes { get; set; }
}

public class CephalometricReportDto : SaveCephalometricReportDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
}

public class RecalculateCephRequestDto
{
    public string RecordId { get; set; } = string.Empty;
    public List<CephPointDto> Landmarks { get; set; } = new();
}

public class RickettELineDto
{
    public double UpperLipDistanceMm { get; set; }
    public double LowerLipDistanceMm { get; set; }
    public string UpperLipStatus { get; set; } = "Retracted"; // Retracted, Normal, Protruded
    public string LowerLipStatus { get; set; } = "Normal";
    public double NasolabialAngle { get; set; } = 102.0;
}

public class CephalometricVtoSimulationRequestDto
{
    public string RecordId { get; set; } = string.Empty;
    public double LeFortIMaxillaryHorizontalMm { get; set; } // + Advancement, - Impaction/Setback
    public double LeFortIMaxillaryVerticalMm { get; set; } // + Lengthening, - Intrusion
    public double BssoMandibularHorizontalMm { get; set; } // + Advancement, - Setback
    public double BssoMandibularVerticalMm { get; set; }
    public double GenioplastyHorizontalMm { get; set; } // + Chin advancement
    public double GenioplastyVerticalMm { get; set; }
    public List<CephPointDto>? BaseLandmarks { get; set; }
}

public class CephalometricVtoResultDto
{
    public string RecordId { get; set; } = string.Empty;
    public DateTime SimulatedAtUtc { get; set; } = DateTime.UtcNow;
    public double PredictedSna { get; set; }
    public double PredictedSnb { get; set; }
    public double PredictedAnb { get; set; }
    public string PredictedSkeletalClass { get; set; } = "Class I";
    public string PredictedSkeletalClassAr { get; set; } = "صنف أول هيكلي (Class I)";
    public RickettELineDto PredictedELine { get; set; } = new();
    public List<CephPointDto> DisplacedLandmarks { get; set; } = new();
    public string SurgicalRecommendationSummary { get; set; } = string.Empty;
    public string SurgicalRecommendationSummaryAr { get; set; } = string.Empty;
}

