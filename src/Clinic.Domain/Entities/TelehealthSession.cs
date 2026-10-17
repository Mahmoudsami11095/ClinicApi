using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Clinic.Domain.Entities;

public class TelehealthSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ClinicId { get; set; } = string.Empty;
    public string AppointmentId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string RoomToken { get; set; } = Guid.NewGuid().ToString("N");
    public string Status { get; set; } = "WaitingForPatient"; // WaitingForPatient, Active, Completed, Cancelled
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public int DurationSeconds { get; set; } = 0;
    public string? ClinicalSummary { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Release v12.0.0 Multi-Clinic Telehealth & Co-Viewer Enhancements (BR-TELE-01..04)
    public string? InvitedSpecialistId { get; set; }
    public string? InvitedSpecialistName { get; set; }
    public string? InvitedSpecialty { get; set; }
    public string ActiveViewType { get; set; } = "None"; // "None", "Mesh3D", "Radiograph2D", "DICOM"
    public string? LastViewportSyncJson { get; set; }
    public string? Sha256AuditSeal { get; set; }
    public bool IsSha256Verified { get; set; } = false;

    public List<TelehealthAuditRecord> AuditTrail { get; set; } = new();
    public List<TelehealthPrescriptionRecord> IssuedPrescriptions { get; set; } = new();

    public string ComputeSessionAuditSha256()
    {
        var rawPayload = $"{RoomToken}|{DoctorId}|{PatientId}|{ClinicId}|{StartedAtUtc:O}|{EndedAtUtc:O}|{DurationSeconds}|{AuditTrail.Count}|{IssuedPrescriptions.Count}";
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(rawPayload);
        var hash = sha256.ComputeHash(bytes);
        return $"urn:telehealth:sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    public static string ComputePrescriptionSha256(string rxNumber, string doctorId, string patientId, DateTime issuedAt, int medCount)
    {
        var raw = $"{rxNumber}|{doctorId}|{patientId}|{issuedAt:O}|{medCount}";
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(raw);
        var hash = sha256.ComputeHash(bytes);
        return $"urn:rx:sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}

public class TelehealthAuditRecord
{
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string ParticipantId { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public string Role { get; set; } = "Doctor"; // Doctor, Patient, Specialist, LabTech
    public string EventType { get; set; } = string.Empty; // Joined, Left, OfferSent, ViewportSync, AnnotationAdded, PrescriptionIssued, SessionEnded
    public string? Details { get; set; }
}

public class TelehealthPrescriptionRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RxNumber { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
    public List<TelehealthPrescriptionItemRecord> Medications { get; set; } = new();
    public string? Notes { get; set; }
    public string DigitalSignature { get; set; } = string.Empty;
    public string VerificationUrl { get; set; } = string.Empty;
    public string DispatchChannel { get; set; } = "WhatsApp";
    public string Sha256Digest { get; set; } = string.Empty;
}

public class TelehealthPrescriptionItemRecord
{
    public string DrugName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
}
