using System;

namespace Clinic.Domain.Entities;

public class PaymentPlanInstallment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string PaymentPlanId { get; set; } = string.Empty;
    public PaymentPlan PaymentPlan { get; set; } = null!;

    public int InstallmentNumber { get; set; }
    public string DueDate { get; set; } = string.Empty; // YYYY-MM-DD

    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; } = 0.0m;

    public string Status { get; set; } = "Pending"; // Pending, PartiallyPaid, Paid, Overdue
    public string? PaidAt { get; set; }
    public string? PaymentMethod { get; set; } // Cash, Card, BankTransfer, Tabby, Tamara
    public string? TransactionReference { get; set; }
    public string? ReminderSentAt { get; set; }
    public string? Notes { get; set; }
}
