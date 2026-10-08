using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Clinic.Application.DTOs;

public class InsuranceProviderDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PayerCode { get; set; } = string.Empty;
    public decimal PreAuthThreshold { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; }
}

public class CreateInsuranceProviderDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string PayerCode { get; set; } = string.Empty;

    [Range(0, 1000000)]
    public decimal PreAuthThreshold { get; set; } = 1500m;

    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public class CreateInsuranceClaimDto
{
    [Required]
    public string ClinicId { get; set; } = string.Empty;

    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public string DoctorId { get; set; } = string.Empty;

    [Required]
    public string InsuranceProviderId { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string PolicyNumber { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string MemberId { get; set; } = string.Empty;

    public int? ToothNumber { get; set; }

    [Required, MaxLength(50)]
    public string DiagnosisCode { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string ProcedureDescription { get; set; } = string.Empty;

    [Range(1, 1000000)]
    public decimal TotalGrossAmount { get; set; }

    [Range(0, 100)]
    public decimal CopayPercentage { get; set; } = 20m;

    public string? PreAuthNotes { get; set; }
    public List<string>? ClaimFileUrls { get; set; }
}

public class AdjudicateClaimDto
{
    [Required]
    public string Status { get; set; } = "Approved"; // "Approved", "PartiallyApproved", "Rejected", "PreAuthorized"

    public decimal? ApprovedAmount { get; set; }
    public string? AdjudicationNotes { get; set; }
    public string? RejectionReason { get; set; }
}

public class InsuranceClaimResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;

    public string ClinicId { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;

    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;

    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;

    public string InsuranceProviderId { get; set; } = string.Empty;
    public string InsuranceProviderName { get; set; } = string.Empty;

    public string PolicyNumber { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public int? ToothNumber { get; set; }
    public string DiagnosisCode { get; set; } = string.Empty;
    public string ProcedureDescription { get; set; } = string.Empty;

    public decimal TotalGrossAmount { get; set; }
    public decimal CopayPercentage { get; set; }
    public decimal PatientCopayAmount { get; set; }
    public decimal ClaimedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }

    public string Status { get; set; } = "Draft";
    public string? PreAuthNotes { get; set; }
    public string? AdjudicationNotes { get; set; }
    public string? RejectionReason { get; set; }
    public List<string> ClaimFileUrls { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? AdjudicatedAt { get; set; }
    public DateTime? SettledAt { get; set; }
}

public class InsuranceClaimsSummaryDto
{
    public int TotalClaimsCount { get; set; }
    public decimal TotalClaimedAmount { get; set; }
    public decimal TotalApprovedAmount { get; set; }
    public int PendingPreAuthCount { get; set; }
    public int RejectionCount { get; set; }
}

// ── AI Insurance Pre-Authorization & Claim Package Generator (Release v4.2.0) ──
public class GenerateAiClaimDto
{
    [Required]
    public string RadiologyRecordId { get; set; } = string.Empty;

    public string? ClinicId { get; set; }
    public string? PatientId { get; set; }
    public string? DoctorId { get; set; }

    [Required]
    public string InsuranceProviderId { get; set; } = string.Empty;

    public string PolicyNumber { get; set; } = "POL-AI-9942";
    public string MemberId { get; set; } = "MEM-7731";

    public List<string> AcceptedFindingIds { get; set; } = new();
    public string? DoctorClinicalNotes { get; set; }
}

public class RealtimeEligibilityResponseDto
{
    public string ClaimId { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string PayerCode { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public bool IsEligible { get; set; } = true;
    public string EligibilityStatus { get; set; } = "Active - Full In-Network Dental Coverage";
    public decimal CopayPercentage { get; set; } = 20.0m;
    public decimal PatientDeductibleRemaining { get; set; } = 50.0m;
    public bool PreAuthRequired { get; set; } = true;
    public string PreAuthStatus { get; set; } = "Pre-Authorized";
    public string AuthorizationToken { get; set; } = string.Empty;
    public DateTime InquiryTimestamp { get; set; } = DateTime.UtcNow;
}

public class ClaimPacketProcedureDto
{
    public string CdtCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ToothNumber { get; set; } = string.Empty;
    public string DiagnosisCode { get; set; } = string.Empty; // ICD-10
    public decimal Fee { get; set; }
}

public class ClaimPacketPdfResponseDto
{
    public string ClaimId { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public string VerificationHash { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string PayerCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorLicenseNumber { get; set; } = "EGY-DEN-44910";
    public decimal TotalGrossAmount { get; set; }
    public decimal PatientCopayAmount { get; set; }
    public decimal InsurancePayableAmount { get; set; }
    public string RadiographUrl { get; set; } = string.Empty;
    public int AiFindingsCount { get; set; }
    public List<ClaimPacketProcedureDto> Procedures { get; set; } = new();
    public string QrVerificationPayload { get; set; } = string.Empty;
    public DateTime SignedAtUtc { get; set; } = DateTime.UtcNow;
    public string PreAuthStatus { get; set; } = "PreAuthorized";
}
