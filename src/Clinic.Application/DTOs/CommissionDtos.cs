using System.Text.Json.Serialization;

namespace Clinic.Application.DTOs;

public class DoctorCommissionPlanDto
{
    public string Id { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string? DoctorName { get; set; }
    public string? ClinicId { get; set; }
    public decimal DefaultCommissionRate { get; set; }
    public string LabFeeDeductionType { get; set; } = "BeforeCommission";
    public Dictionary<string, decimal> SpecialtyRates { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateOrUpdateCommissionPlanDto
{
    public string DoctorId { get; set; } = string.Empty;
    public string? ClinicId { get; set; }
    public decimal DefaultCommissionRate { get; set; } = 30.0m;
    public string LabFeeDeductionType { get; set; } = "BeforeCommission";
    public Dictionary<string, decimal>? SpecialtyRates { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CommissionAnalyticsDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalGrossRevenue { get; set; }
    public decimal TotalLabFeesDeducted { get; set; }
    public decimal TotalNetCommission { get; set; }
    public decimal TotalClinicRetainedRevenue { get; set; }
    public int DoctorCount { get; set; }
    public int ProcedureCount { get; set; }
    public List<DoctorCommissionSummaryDto> Doctors { get; set; } = new();
    public List<CommissionItemDto> EncounterItems { get; set; } = new();
}

public class DoctorCommissionSummaryDto
{
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public decimal GrossRevenue { get; set; }
    public decimal LabFeesDeducted { get; set; }
    public decimal NetCommission { get; set; }
    public decimal ClinicShare { get; set; }
    public decimal EffectiveRate { get; set; }
    public int TotalProcedures { get; set; }
    public bool HasActivePlan { get; set; }
}

public class CommissionItemDto
{
    public string Id { get; set; } = string.Empty;
    public string? BillingRecordId { get; set; }
    public string? AppointmentId { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string ServiceCategory { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal LabFee { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal ClinicAmount { get; set; }
}

public class CommissionPayoutDto
{
    public string Id { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string? ClinicId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalGrossRevenue { get; set; }
    public decimal TotalLabFeesDeducted { get; set; }
    public decimal TotalNetCommission { get; set; }
    public decimal ClinicRetainedRevenue { get; set; }
    public string Status { get; set; } = "Draft";
    public string? PaymentReference { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CommissionPayoutItemDto> Items { get; set; } = new();
}

public class CommissionPayoutItemDto
{
    public string Id { get; set; } = string.Empty;
    public string? BillingRecordId { get; set; }
    public string? AppointmentId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string ServiceCategory { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public decimal LabFee { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public DateTime ServiceDate { get; set; }
}

public class CreateCommissionPayoutDto
{
    public string DoctorId { get; set; } = string.Empty;
    public string? ClinicId { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public string? Notes { get; set; }
}

public class SettleCommissionPayoutDto
{
    public string PaymentReference { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
