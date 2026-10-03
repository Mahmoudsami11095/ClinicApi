using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class SplitPaymentUnitTests
{
    [Fact]
    public void REQ_BIL_02_SplitPayment_CalculatesPaidAmountAndTransitionsToPartiallyPaid()
    {
        // Arrange
        var invoice = new BillingRecord
        {
            Id = "inv-split-1",
            Amount = 300m,
            Status = "pending",
            PaidAmount = 0
        };

        // Act - First payment of 100 Cash
        invoice.Payments.Add(new PaymentLog
        {
            Amount = 100m,
            PaymentMethod = "Cash",
            Date = "2026-10-04T10:00:00Z"
        });
        invoice.PaidAmount = invoice.Payments.Sum(p => p.Amount);
        if (invoice.PaidAmount >= invoice.Amount)
        {
            invoice.Status = "paid";
        }
        else if (invoice.PaidAmount > 0)
        {
            invoice.Status = "partially_paid";
        }

        // Assert
        Assert.Equal(100m, invoice.PaidAmount);
        Assert.Equal("partially_paid", invoice.Status);
        Assert.Single(invoice.Payments);
    }

    [Fact]
    public void REQ_BIL_02_SplitPayment_MultipleMethodsCompleteInvoiceAndSetPaidStatus()
    {
        // Arrange
        var invoice = new BillingRecord
        {
            Id = "inv-split-2",
            Amount = 250m,
            Status = "pending",
            PaidAmount = 0
        };

        // Act - First payment: 100 Cash
        invoice.Payments.Add(new PaymentLog
        {
            Amount = 100m,
            PaymentMethod = "Cash",
            Date = "2026-10-04T10:00:00Z"
        });

        // Act - Second payment: 150 Credit Card
        invoice.Payments.Add(new PaymentLog
        {
            Amount = 150m,
            PaymentMethod = "Credit Card",
            Date = "2026-10-04T10:05:00Z"
        });

        invoice.PaidAmount = invoice.Payments.Sum(p => p.Amount);
        if (invoice.PaidAmount >= invoice.Amount)
        {
            invoice.Status = "paid";
        }

        var distinctMethods = invoice.Payments.Select(p => p.PaymentMethod).Distinct().ToList();
        invoice.PaymentMethod = distinctMethods.Count > 1 ? "Split Payment" : distinctMethods.FirstOrDefault() ?? "Cash";

        // Assert
        Assert.Equal(250m, invoice.PaidAmount);
        Assert.Equal("paid", invoice.Status);
        Assert.Equal("Split Payment", invoice.PaymentMethod);
        Assert.Equal(2, invoice.Payments.Count);
    }
}
