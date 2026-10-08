using System;

namespace Clinic.Domain.Entities;

public class InformedConsentDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DocumentNumber { get; set; } = string.Empty; // CNS-YYYYMM-XXXX

    public string ClinicId { get; set; } = string.Empty;
    public ClinicEntity Clinic { get; set; } = null!;

    public string PatientId { get; set; } = string.Empty;
    public Patient Patient { get; set; } = null!;

    public string DoctorId { get; set; } = string.Empty;
    public Doctor Doctor { get; set; } = null!;

    public string? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public string ProcedureType { get; set; } = string.Empty; // DentalImplant, SurgicalExtraction, RootCanal, Orthodontics, SedationAnesthesia, AestheticBotox
    public string ProcedureName { get; set; } = string.Empty;
    public int? ToothNumber { get; set; }

    public string ClinicalRiskDisclosures { get; set; } = "[]"; // JSON array of risks
    public string? SpecialMedicalCautions { get; set; }

    public string? PatientSignatureBase64 { get; set; }
    public string? DoctorSignatureBase64 { get; set; }
    public string SignatoryName { get; set; } = string.Empty;
    public string SignatoryRelationship { get; set; } = "Self"; // Self, Parent, LegalGuardian

    public string? DocumentSha256Checksum { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, PendingSignature, SignedByPatient, CountersignedByDoctor, ArchivedLocked

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SignedAt { get; set; }
    public DateTime? CountersignedAt { get; set; }

    public bool IsDeleted { get; set; } = false;
}
