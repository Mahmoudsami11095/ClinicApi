using System;
using System.Collections.Generic;

namespace Clinic.Application.DTOs;

public record CreateTelehealthSessionRequest(
    string? ClinicId,
    string? AppointmentId,
    string? PatientId,
    string? PatientName,
    string? InvitedSpecialistId = null,
    string? InvitedSpecialistName = null,
    string? InvitedSpecialty = null
);

public record EndTelehealthSessionRequest(
    string? ClinicalSummary,
    int? DurationSeconds
);

public record ViewportSyncDto(
    string RoomToken,
    string ViewType, // "Mesh3D", "Radiograph2D", "DICOM"
    double CameraX,
    double CameraY,
    double CameraZ,
    double RotationPitch,
    double RotationYaw,
    double ZoomFactor,
    string? ActiveAssetId = null,
    DateTime? TimestampUtc = null
);

public record SharedAnnotationDto(
    string RoomToken,
    string AnnotationId,
    string SenderRole, // "Doctor", "Patient", "Specialist", "LabTech"
    string ToolType,   // "caliper", "arrow", "freehand", "probe"
    double[]? Coordinates = null,
    string? TextLabel = null,
    double? MeasurementValueMm = null,
    string? ColorHex = null,
    DateTime? TimestampUtc = null
);

public record TelehealthMedicationItemDto(
    string DrugName,
    string Dosage,
    string Frequency,
    string Duration,
    string Instructions
);

public record IssueTelePrescriptionRequest(
    string PatientId,
    string PatientName,
    List<TelehealthMedicationItemDto> Medications,
    string? Notes = null,
    string? DispatchChannel = "WhatsApp" // "WhatsApp", "SMS", "Print"
);

public record TelehealthAuditDossierDto(
    string RoomToken,
    string DoctorName,
    string PatientName,
    string? SpecialistName,
    string Status,
    DateTime? StartedAtUtc,
    DateTime? EndedAtUtc,
    int DurationSeconds,
    string? Sha256AuditSeal,
    bool IsSha256Verified,
    int TotalEventsLogged,
    List<TelehealthAuditItemDto> Events,
    List<TelehealthPrescriptionSummaryDto> Prescriptions
);

public record TelehealthAuditItemDto(
    DateTime TimestampUtc,
    string ParticipantId,
    string ParticipantName,
    string Role,
    string EventType,
    string? Details
);

public record TelehealthPrescriptionSummaryDto(
    string Id,
    string RxNumber,
    string DoctorName,
    string PatientName,
    DateTime IssuedAtUtc,
    List<TelehealthMedicationItemDto> Medications,
    string? Notes,
    string DigitalSignature,
    string VerificationUrl,
    string DispatchChannel,
    string Sha256Digest
);

public record VerifyAuditSealResponse(
    string RoomToken,
    string? CurrentSeal,
    string CalculatedSeal,
    bool IsValid,
    DateTime VerifiedAtUtc
);
