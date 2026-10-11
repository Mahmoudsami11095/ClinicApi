using System;
using System.Collections.Generic;

namespace Clinic.Domain.Entities;

public class PaymentPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PlanNumber { get; set; } = string.Empty; // PLAN-YYYYMM-XXXX

    public string ClinicId { get; set; } = string.Empty;
    public ClinicEntity Clinic { get; set; } = null!;

    public string PatientId { get; set; } = string.Empty;
    public Patient Patient { get; set; } = null!;

    public string DoctorId { get; set; } = string.Empty;
    public Doctor Doctor { get; set; } = null!;

    public string? TreatmentPlanId { get; set; }
    public string? BillingRecordId { get; set; }

    public string Title { get; set; } = "Comprehensive Dental Treatment Financing";

    public decimal TotalAmount { get; set; }
    public decimal DownPayment { get; set; }
    public decimal FinancedAmount { get; set; }
    public decimal InterestRate { get; set; } = 0.0m;
    public decimal TotalPayable { get; set; }

    public int NumberOfInstallments { get; set; } = 6;
    public string InstallmentFrequency { get; set; } = "Monthly"; // Monthly, BiWeekly
    public string StartDate { get; set; } = string.Empty; // YYYY-MM-DD

    public string Status { get; set; } = "Active"; // Draft, Active, Completed, AtRisk, Defaulted, Cancelled
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation collection
    public List<PaymentPlanInstallment> Installments { get; set; } = new();
}
