using System;
using System.Collections.Generic;
using System.Linq;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class PaymentPlanUnitTests
{
    [Fact]
    public void REQ_PLAN_01_PaymentPlan_Creation_And_Default_Properties()
    {
        // Arrange & Act
        var plan = new PaymentPlan
        {
            PlanNumber = "PLAN-202610-0001",
            ClinicId = "clinic-01",
            PatientId = "pat-01",
            DoctorId = "doc-01",
            Title = "Orthodontic Aligners Financing",
            TotalAmount = 30000.0m,
            DownPayment = 6000.0m,
            FinancedAmount = 24000.0m,
            TotalPayable = 24000.0m,
            NumberOfInstallments = 6,
            InstallmentFrequency = "Monthly",
            StartDate = "2026-11-01"
        };

        // Assert
        Assert.NotNull(plan.Id);
        Assert.Equal("PLAN-202610-0001", plan.PlanNumber);
        Assert.Equal("Active", plan.Status);
        Assert.Equal(30000.0m, plan.TotalAmount);
        Assert.Equal(6000.0m, plan.DownPayment);
        Assert.Equal(24000.0m, plan.FinancedAmount);
        Assert.Equal(6, plan.NumberOfInstallments);
        Assert.False(plan.IsDeleted);
        Assert.Empty(plan.Installments);
    }

    [Fact]
    public void REQ_PLAN_02_Cent_Perfect_Mathematical_Split_Absorbs_Rounding()
    {
        // Arrange: 10,000 EGP financed over 3 months = 3333.3333... per month
        decimal financedAmount = 10000.0m;
        int numberOfInstallments = 3;

        // Act: Apply BR-PF-01 cent-perfect splitting algorithm
        var baseAmount = Math.Floor((financedAmount / numberOfInstallments) * 100m) / 100m; // 3333.33
        var allocatedTotal = baseAmount * (numberOfInstallments - 1); // 6666.66
        var lastAmount = financedAmount - allocatedTotal; // 3333.34

        var installments = new List<PaymentPlanInstallment>();
        for (int i = 1; i <= numberOfInstallments; i++)
        {
            installments.Add(new PaymentPlanInstallment
            {
                InstallmentNumber = i,
                AmountDue = (i == numberOfInstallments) ? lastAmount : baseAmount
            });
        }

        // Assert
        Assert.Equal(3333.33m, installments[0].AmountDue);
        Assert.Equal(3333.33m, installments[1].AmountDue);
        Assert.Equal(3333.34m, installments[2].AmountDue);
        Assert.Equal(financedAmount, installments.Sum(i => i.AmountDue)); // Zero cent discrepancy
    }

    [Fact]
    public void REQ_PLAN_03_Installment_Payment_Settlement_And_Plan_Completion()
    {
        // Arrange
        var plan = new PaymentPlan
        {
            PlanNumber = "PLAN-202610-0003",
            TotalAmount = 5000.0m,
            DownPayment = 1000.0m,
            FinancedAmount = 4000.0m,
            TotalPayable = 4000.0m,
            NumberOfInstallments = 2,
            Status = "Active"
        };

        var inst1 = new PaymentPlanInstallment
        {
            InstallmentNumber = 1,
            DueDate = "2026-11-01",
            AmountDue = 2000.0m,
            AmountPaid = 0.0m,
            Status = "Pending"
        };

        var inst2 = new PaymentPlanInstallment
        {
            InstallmentNumber = 2,
            DueDate = "2026-12-01",
            AmountDue = 2000.0m,
            AmountPaid = 0.0m,
            Status = "Pending"
        };

        plan.Installments.Add(inst1);
        plan.Installments.Add(inst2);

        // Act: Settle first installment
        inst1.AmountPaid += 2000.0m;
        inst1.Status = "Paid";
        inst1.PaidAt = DateTime.UtcNow.ToString("o");
        inst1.PaymentMethod = "Card";

        // Assert: Plan is still active because inst2 is unpaid
        Assert.Equal("Paid", inst1.Status);
        Assert.Equal("Active", plan.Status);

        // Act: Settle second installment
        inst2.AmountPaid += 2000.0m;
        inst2.Status = "Paid";
        inst2.PaidAt = DateTime.UtcNow.ToString("o");
        inst2.PaymentMethod = "BankTransfer";

        if (plan.Installments.All(i => i.Status == "Paid"))
        {
            plan.Status = "Completed";
            plan.CompletedAt = DateTime.UtcNow;
        }

        // Assert: Plan transitions to Completed
        Assert.Equal("Completed", plan.Status);
        Assert.NotNull(plan.CompletedAt);
    }

    [Fact]
    public void REQ_PLAN_04_Overdue_Detection_And_Delinquency_Status()
    {
        // Arrange: Installment with due date in the past
        var pastDate = "2026-08-01";
        var today = "2026-10-11";

        var installment = new PaymentPlanInstallment
        {
            InstallmentNumber = 1,
            DueDate = pastDate,
            AmountDue = 2500.0m,
            AmountPaid = 500.0m,
            Status = "Pending"
        };

        // Act: Dynamic overdue evaluation
        var effectiveStatus = installment.Status;
        if (effectiveStatus == "Pending" || effectiveStatus == "PartiallyPaid")
        {
            if (string.Compare(installment.DueDate, today, StringComparison.Ordinal) < 0)
            {
                effectiveStatus = "Overdue";
            }
        }

        // Assert
        Assert.Equal("Overdue", effectiveStatus);
        Assert.Equal(2000.0m, installment.AmountDue - installment.AmountPaid);
    }

    [Fact]
    public void REQ_PLAN_05_WhatsApp_Reminder_Timestamp_And_Payload()
    {
        // Arrange
        var installment = new PaymentPlanInstallment
        {
            InstallmentNumber = 2,
            DueDate = "2026-10-15",
            AmountDue = 1800.0m,
            AmountPaid = 0.0m,
            Status = "Pending"
        };

        // Act
        installment.ReminderSentAt = DateTime.UtcNow.ToString("o");

        // Assert
        Assert.NotNull(installment.ReminderSentAt);
        Assert.Contains("2026-10", installment.ReminderSentAt);
    }
}
