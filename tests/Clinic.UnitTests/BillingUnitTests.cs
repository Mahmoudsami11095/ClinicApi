using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Xunit;

namespace Clinic.UnitTests;

public class BillingUnitTests
{
    [Fact]
    public void BillingRecord_Initialization_ShouldDefaultToZeroAmountAndEmptyPayments()
    {
        // Arrange & Act
        var bill = new BillingRecord();

        // Assert
        Assert.Equal(0m, bill.Amount);
        Assert.Null(bill.PaidAmount);
        Assert.NotNull(bill.Payments);
        Assert.Empty(bill.Payments);
    }

    [Fact]
    public void BillingRecord_SplitPayment_ShouldAccuratelyTrackCumulativePaidAmount()
    {
        // Arrange
        var bill = new BillingRecord
        {
            Id = "bill-001",
            Amount = 1000m,
            PaidAmount = 0m,
            Status = nameof(BillingStatus.Pending)
        };

        // Act - Payment 1 (Cash $600)
        bill.Payments.Add(new PaymentLog
        {
            Amount = 600m,
            PaymentMethod = "Cash",
            Date = "2026-09-25"
        });
        bill.PaidAmount = bill.Payments.Sum(p => p.Amount);
        bill.Status = bill.PaidAmount < bill.Amount ? nameof(BillingStatus.PartiallyPaid) : nameof(BillingStatus.Paid);

        // Assert Step 1
        Assert.Equal(600m, bill.PaidAmount);
        Assert.Equal(nameof(BillingStatus.PartiallyPaid), bill.Status);
        Assert.Equal(400m, bill.Amount - (bill.PaidAmount ?? 0m)); // Remaining balance

        // Act - Payment 2 (Card $400)
        bill.Payments.Add(new PaymentLog
        {
            Amount = 400m,
            PaymentMethod = "Credit Card",
            Date = "2026-09-25"
        });
        bill.PaidAmount = bill.Payments.Sum(p => p.Amount);
        bill.Status = bill.PaidAmount >= bill.Amount ? nameof(BillingStatus.Paid) : nameof(BillingStatus.PartiallyPaid);

        // Assert Step 2
        Assert.Equal(1000m, bill.PaidAmount);
        Assert.Equal(nameof(BillingStatus.Paid), bill.Status);
        Assert.Equal(0m, bill.Amount - (bill.PaidAmount ?? 0m));
    }
}
