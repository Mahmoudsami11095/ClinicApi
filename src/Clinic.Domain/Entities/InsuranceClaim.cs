using System;

namespace Clinic.Domain.Entities;

public class InsuranceClaim
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ClaimNumber { get; set; } = string.Empty; // CLM-YYYYMM-XXXX

    public string ClinicId { get; set; } = string.Empty;
    public ClinicEntity Clinic { get; set; } = null!;

    public string PatientId { get; set; } = string.Empty;
    public Patient Patient { get; set; } = null!;

    public string DoctorId { get; set; } = string.Empty;
    public Doctor Doctor { get; set; } = null!;

    public string InsuranceProviderId { get; set; } = string.Empty;
    public InsuranceProvider InsuranceProvider { get; set; } = null!;

    public string PolicyNumber { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public int? ToothNumber { get; set; }
    public string DiagnosisCode { get; set; } = string.Empty; // e.g. K02.1
    public string ProcedureDescription { get; set; } = string.Empty;

    public decimal TotalGrossAmount { get; set; }
    public decimal CopayPercentage { get; set; } = 20m;
    public decimal PatientCopayAmount { get; set; }
    public decimal ClaimedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }

    public string Status { get; set; } = "Draft"; // Draft, Submitted, PreAuthorized, Approved, PartiallyApproved, Rejected, Settled
    public string? PreAuthNotes { get; set; }
    public string? AdjudicationNotes { get; set; }
    public string? RejectionReason { get; set; }
    public string ClaimFileUrls { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? AdjudicatedAt { get; set; }
    public DateTime? SettledAt { get; set; }

    public bool IsDeleted { get; set; } = false;
}
