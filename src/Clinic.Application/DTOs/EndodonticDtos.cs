using System;
using System.Collections.Generic;

namespace Clinic.Application.DTOs;

public class EndoPoint2DDto
{
    public double X { get; set; }
    public double Y { get; set; }

    public EndoPoint2DDto() { }

    public EndoPoint2DDto(double x, double y)
    {
        X = x;
        Y = y;
    }
}

public class EndoCanalCurvatureDto
{
    public string CanalName { get; set; } = "Mesio-Buccal (MB1)";
    public int ToothNumber { get; set; } = 36;
    public EndoPoint2DDto PointA { get; set; } = new(120, 50); // Coronal Orifice Reference
    public EndoPoint2DDto PointB { get; set; } = new(135, 175); // Curvature Inception
    public EndoPoint2DDto PointC { get; set; } = new(178, 285); // Apical Terminus
    public double SchneiderAngleDegrees { get; set; } = 28.4;
    public double RadiusOfCurvatureMm { get; set; } = 5.8;
    public string CurvatureClassification { get; set; } = "Moderate"; // Mild (<20°), Moderate (20°-35°), Severe (>35°)
    public string CurvatureClassificationAr { get; set; } = "انحناء متوسط";
    public string CyclicFatigueRiskLevel { get; set; } = "Medium"; // Low, Medium, HighRiskOfSeparation
    public string Recommendation { get; set; } = "Perform coronal pre-flaring before rotary progression. Maintain file lubrication.";
    public string RecommendationAr { get; set; } = "يُنصح بالتوسيع التاجي الأولي قبل إدخال المبارد الدوارة مع الترطيب المستمر.";
}

public class EndoWorkingLengthDto
{
    public string CoronalReferenceLandmark { get; set; } = "Mesio-Buccal Cusp Tip";
    public double RadiographicRootLengthMm { get; set; } = 21.5;
    public string DiagnosisCondition { get; set; } = "NecroticWithApicalLesion"; // VitalPulp, NecroticWithApicalLesion
    public double ApicalSafetyOffsetMm { get; set; } = 1.0; // 0.5 mm for vital, 1.0 mm for necrotic
    public double CalculatedWorkingLengthMm { get; set; } = 20.5;
    public bool ConfirmedByApexLocator { get; set; } = true;
    public double ApexLocatorReadingMm { get; set; } = 0.0; // 0.0 = APEX
    public string SafetyStatus { get; set; } = "OptimalApicalConstriction";
    public string SafetyStatusAr { get; set; } = "التضيق القمي الأمثل";
}

public class EndoRotaryFileStepDto
{
    public int StepOrder { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int IsoTipSize { get; set; }
    public double Taper { get; set; }
    public int SpeedRpm { get; set; } = 350;
    public double TorqueNcm { get; set; } = 2.5;
    public string KinematicMotion { get; set; } = "Continuous Rotation (360°)";
    public string ClinicalInstructions { get; set; } = string.Empty;
}

public class EndoRotaryRecipeDto
{
    public string SystemName { get; set; } = "ProTaper Ultimate";
    public string SystemManufacturer { get; set; } = "Dentsply Sirona";
    public string RecommendedForClassification { get; set; } = "Moderate";
    public string RecommendedSpeedRangeRpm { get; set; } = "300 - 400 RPM";
    public string RecommendedTorqueRangeNcm { get; set; } = "2.0 - 4.0 N·cm";
    public string? CyclicFatigueWarning { get; set; }
    public List<EndoRotaryFileStepDto> FileSequence { get; set; } = new();
}

public class EndodonticAnalysisResultDto
{
    public string RecordId { get; set; } = string.Empty;
    public int ToothNumber { get; set; } = 36;
    public DateTime AnalysisTimestampUtc { get; set; } = DateTime.UtcNow;
    public EndoCanalCurvatureDto Curvature { get; set; } = new();
    public EndoWorkingLengthDto WorkingLength { get; set; } = new();
    public EndoRotaryRecipeDto RecommendedRecipe { get; set; } = new();
    public List<EndoRotaryRecipeDto> AlternativeRecipes { get; set; } = new();
    public string ClinicalNotes { get; set; } = string.Empty;
    public bool IsVerifiedByDoctor { get; set; }
    public string? VerifiedByDoctorName { get; set; }
}

public class RecalculateEndodonticCanalRequestDto
{
    public int ToothNumber { get; set; } = 36;
    public string CanalName { get; set; } = "Mesio-Buccal (MB1)";
    public EndoPoint2DDto? PointA { get; set; }
    public EndoPoint2DDto? PointB { get; set; }
    public EndoPoint2DDto? PointC { get; set; }
    public string? CoronalReferenceLandmark { get; set; }
    public string? DiagnosisCondition { get; set; }
    public double? CustomApicalOffsetMm { get; set; }
    public double? RadiographicRootLengthMm { get; set; }
}

public class SaveEndodonticAnalysisRequestDto
{
    public int ToothNumber { get; set; } = 36;
    public string CanalName { get; set; } = "Mesio-Buccal (MB1)";
    public double SchneiderAngleDegrees { get; set; }
    public double WorkingLengthMm { get; set; }
    public string SelectedFileSystem { get; set; } = "ProTaper Ultimate";
    public string? DoctorNotes { get; set; }
    public bool IsVerified { get; set; } = true;
}
