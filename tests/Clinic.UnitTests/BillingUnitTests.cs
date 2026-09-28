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

    [Fact]
    public void BillingRecord_DiscountCalculation_ShouldCorrectlyDeductFromSubtotal()
    {
        // Arrange
        const decimal subtotal = 500m;
        const decimal discountPercentage = 15m;
        var discountAmount = subtotal * (discountPercentage / 100m);
        var netPayable = subtotal - discountAmount;

        // Act
        var bill = new BillingRecord
        {
            Id = "bill-disc-01",
            Subtotal = subtotal,
            DiscountPercentage = discountPercentage,
            DiscountAmount = discountAmount,
            DiscountReason = "Senior Citizen Courtesy",
            DiscountAuthorizedBy = "Dr. Robert Smith (PIN Verified)",
            Amount = netPayable,
            Status = nameof(BillingStatus.Pending)
        };

        // Assert
        Assert.Equal(500m, bill.Subtotal);
        Assert.Equal(15m, bill.DiscountPercentage);
        Assert.Equal(75m, bill.DiscountAmount);
        Assert.Equal(425m, bill.Amount);
        Assert.Equal("Dr. Robert Smith (PIN Verified)", bill.DiscountAuthorizedBy);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(5, true)]
    [InlineData(10, true)]
    [InlineData(15, false)]
    [InlineData(25, false)]
    [InlineData(50, false)]
    public void BR_FIN_01_ReceptionistDiscountCap_EvaluatesCorrectly(decimal discountPct, bool isAllowedWithoutDoctorPIN)
    {
        // BR-FIN-01: Receptionists may apply courtesy discounts only up to 10%.
        // Any discount exceeding this threshold requires Doctor or Admin PIN verification.
        const decimal receptionistCap = 10m;
        var requiresApproval = discountPct > receptionistCap;

        Assert.Equal(isAllowedWithoutDoctorPIN, !requiresApproval);
    }

    [Fact]
    public void BR_FIN_01_HighDiscount_WithDoctorAuthorization_IsPermitted()
    {
        // Arrange
        var bill = new BillingRecord
        {
            Subtotal = 1200m,
            DiscountPercentage = 25m,
            DiscountAmount = 300m,
            Amount = 900m,
            DiscountReason = "Clinical Hardship Relief",
            DiscountAuthorizedBy = "Dr. Mahmoud (PIN Verified)"
        };

        // Assert
        Assert.True(bill.DiscountPercentage > 10m);
        Assert.False(string.IsNullOrWhiteSpace(bill.DiscountAuthorizedBy));
        Assert.Equal(900m, bill.Amount);
    }
}
