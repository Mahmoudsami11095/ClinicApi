using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Clinic.Application.DTOs;

public class CreatePaymentPlanDto
{
    [Required]
    public string ClinicId { get; set; } = string.Empty;

    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public string DoctorId { get; set; } = string.Empty;

    public string? TreatmentPlanId { get; set; }
    public string? BillingRecordId { get; set; }

    [Required]
    public string Title { get; set; } = "Dental Financing Plan";

    [Range(1, 1000000)]
    public decimal TotalAmount { get; set; }

    [Range(0, 1000000)]
    public decimal DownPayment { get; set; } = 0.0m;

    [Range(0, 100)]
    public decimal InterestRate { get; set; } = 0.0m;

    [Range(1, 60)]
    public int NumberOfInstallments { get; set; } = 6;

    public string InstallmentFrequency { get; set; } = "Monthly"; // Monthly, BiWeekly

    [Required]
    public string StartDate { get; set; } = string.Empty; // YYYY-MM-DD

    public string? Notes { get; set; }
}

public class RecordInstallmentPaymentDto
{
    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = "Card"; // Cash, Card, BankTransfer, Tabby, Tamara

    public string? TransactionReference { get; set; }
    public string? Notes { get; set; }
}

public class PaymentPlanInstallmentDto
{
    public string Id { get; set; } = string.Empty;
    public string PaymentPlanId { get; set; } = string.Empty;
    public int InstallmentNumber { get; set; }
    public string DueDate { get; set; } = string.Empty;
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionReference { get; set; }
    public string? ReminderSentAt { get; set; }
    public string? Notes { get; set; }
}

public class PaymentPlanDto
{
    public string Id { get; set; } = string.Empty;
    public string PlanNumber { get; set; } = string.Empty;

    public string ClinicId { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;

    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;

    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;

    public string? TreatmentPlanId { get; set; }
    public string? BillingRecordId { get; set; }

    public string Title { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }
    public decimal DownPayment { get; set; }
    public decimal FinancedAmount { get; set; }
    public decimal InterestRate { get; set; }
    public decimal TotalPayable { get; set; }

    public decimal TotalPaid { get; set; }
    public decimal RemainingBalance { get; set; }

    public int NumberOfInstallments { get; set; }
    public string InstallmentFrequency { get; set; } = "Monthly";
    public string StartDate { get; set; } = string.Empty;

    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public List<PaymentPlanInstallmentDto> Installments { get; set; } = new();
}

public class PaymentPlanSummaryDto
{
    public int TotalPlans { get; set; }
    public int ActivePlans { get; set; }
    public int CompletedPlans { get; set; }
    public int AtRiskPlans { get; set; }

    public decimal TotalFinancedCapital { get; set; }
    public decimal TotalCollectedAmount { get; set; }
    public decimal TotalOutstandingReceivables { get; set; }
    public decimal TotalOverdueArrears { get; set; }

    public double CollectionRatePercentage { get; set; }
}
