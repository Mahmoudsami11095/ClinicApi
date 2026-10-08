using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Clinic.Application.DTOs;

public class ConsentTemplateDto
{
    public string ProcedureType { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public List<string> StandardRisks { get; set; } = new();
    public string DescriptionEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
}

public class CreateInformedConsentDto
{
    [Required]
    public string ClinicId { get; set; } = string.Empty;

    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public string DoctorId { get; set; } = string.Empty;

    public string? AppointmentId { get; set; }

    [Required]
    public string ProcedureType { get; set; } = string.Empty;

    [Required]
    public string ProcedureName { get; set; } = string.Empty;

    public int? ToothNumber { get; set; }

    public List<string> ClinicalRiskDisclosures { get; set; } = new();
    public string? SpecialMedicalCautions { get; set; }
}

public class SignPatientConsentDto
{
    [Required]
    public string PatientSignatureBase64 { get; set; } = string.Empty;

    [Required]
    public string SignatoryName { get; set; } = string.Empty;

    public string SignatoryRelationship { get; set; } = "Self"; // "Self", "Parent", "LegalGuardian"
}

public class DoctorCountersignDto
{
    [Required]
    public string DoctorSignatureBase64 { get; set; } = string.Empty;

    public string? DoctorSyndicateNumber { get; set; }
}

public class InformedConsentResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;

    public string ClinicId { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;

    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;

    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;

    public string? AppointmentId { get; set; }
    public string ProcedureType { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public int? ToothNumber { get; set; }

    public List<string> ClinicalRiskDisclosures { get; set; } = new();
    public string? SpecialMedicalCautions { get; set; }

    public string? PatientSignatureBase64 { get; set; }
    public string? DoctorSignatureBase64 { get; set; }
    public string SignatoryName { get; set; } = string.Empty;
    public string SignatoryRelationship { get; set; } = "Self";

    public string? DocumentSha256Checksum { get; set; }
    public string Status { get; set; } = "Draft";

    public DateTime CreatedAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime? CountersignedAt { get; set; }
}
