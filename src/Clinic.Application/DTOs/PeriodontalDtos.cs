using System;
using System.Collections.Generic;

namespace Clinic.Application.DTOs;

public record PeriodontalSiteDto(
    string SiteLocation, // "Distobuccal", "Midbuccal", "Mesiobuccal", "Distolingual", "Midlingual", "Mesiolingual"
    int ProbingDepthMm,
    int GingivalMarginMm,
    int ClinicalAttachmentLossMm,
    bool HasBleedingOnProbing,
    bool HasSuppuration
);

public record PeriodontalToothDto(
    int ToothNumber,
    bool IsMissing,
    bool IsImplant,
    int FurcationGrade, // 0, 1, 2, 3
    int MobilityGrade,  // 0, 1, 2, 3
    List<PeriodontalSiteDto> Sites
);

public record PerioStagingAssessmentDto(
    string Stage, // "Health/Gingivitis", "Stage I", "Stage II", "Stage III", "Stage IV"
    string StageDescription,
    string Grade, // "Grade A", "Grade B", "Grade C"
    string GradeDescription,
    double BopPercentage,
    string InflammationExtent, // "Healthy", "LocalizedGingivitis", "GeneralizedGingivitis", "LocalizedPeriodontitis", "GeneralizedPeriodontitis"
    int MaxCalMm,
    int MaxPpdMm,
    int TotalSitesExamined,
    int TotalBleedingSites,
    int DeepPocketsCount,
    int FurcationTeethCount,
    int MobileTeethCount,
    string ClinicalRiskSummary,
    string ClinicalRiskSummaryAr
);

public record PeriodontalExamDto(
    string ExamId,
    string PatientId,
    int PatientAge,
    DateTime ExamDate,
    string ExaminerName,
    bool IsSmoker,
    int CigarettesPerDay,
    double? HbA1c,
    double BoneLossPercentage,
    PerioStagingAssessmentDto Assessment,
    List<PeriodontalToothDto> Teeth,
    string? VoiceNotes
);

public record SavePeriodontalExamRequest(
    int PatientAge,
    string ExaminerName,
    bool IsSmoker,
    int CigarettesPerDay,
    double? HbA1c,
    double BoneLossPercentage,
    List<PeriodontalToothDto> Teeth,
    string? VoiceNotes
);

public record CalculatePerioStagingRequest(
    int PatientAge,
    double BoneLossPercentage,
    bool IsSmoker,
    int CigarettesPerDay,
    double? HbA1c,
    List<PeriodontalToothDto> Teeth
);
