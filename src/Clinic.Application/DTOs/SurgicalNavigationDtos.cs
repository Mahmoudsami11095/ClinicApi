using System;
using System.Collections.Generic;

namespace Clinic.Application.DTOs;

public record SurgicalNavigationPlanDto(
    string OrderId,
    int ToothNumber,
    string ImplantBrand,
    string ImplantModel,
    double PlannedLengthMm,
    double PlannedDiameterMm,
    double PlannedEntryX,
    double PlannedEntryY,
    double PlannedEntryZ,
    double PlannedApexX,
    double PlannedApexY,
    double PlannedApexZ,
    double TargetDepthMm,
    double DistanceToNerveMm,
    double DistanceToSinusMm,
    double SafetyBufferMm,
    bool IsSafetyBufferCompliant,
    string? SafetyWarning,
    double RegistrationRmseMm,
    bool IsRegistrationValid,
    List<SurgicalDrillStepDto> DrillSequences,
    string StatusMessage,
    DateTime PlannedAtUtc
);

public record SurgicalDrillStepDto(
    int StepIndex,
    string ToolName,
    double DiameterMm,
    double LengthMm,
    double TargetDepthMm,
    int RecommendedSpeedRpm,
    int MaxTorqueNcm,
    string Irrigation,
    string Status // "Pending", "Active", "Completed"
);

public record HandpieceTrackingTelemetryDto(
    string OrderId,
    double TipPositionX,
    double TipPositionY,
    double TipPositionZ,
    double AxisVectorX,
    double AxisVectorY,
    double AxisVectorZ,
    double CurrentDepthMm,
    double AngularDeviationDeg,
    bool IsOnAxis,
    bool IsCautionAxis,
    bool IsOffAxisHazard,
    double CurrentDistanceToVitalMm,
    bool IsSafetyStopEngaged,
    int AcousticFrequencyHz,
    string AcousticAlertMode, // "Silent", "Pulse440", "Pulse880", "Continuous1500"
    string StatusMessage,
    DateTime EvaluatedAtUtc
);

public record SaveSurgicalNavigationRequest(
    int ToothNumber,
    string ImplantBrand,
    string ImplantModel,
    double PlannedLengthMm,
    double PlannedDiameterMm,
    double DistanceToNerveMm,
    double DistanceToSinusMm,
    double RegistrationRmseMm = 0.28
);

public record EvaluateDrillTelemetryRequest(
    double TipPositionX,
    double TipPositionY,
    double TipPositionZ,
    double AxisVectorX,
    double AxisVectorY,
    double AxisVectorZ,
    double DrillLengthMm = 20.0
);
