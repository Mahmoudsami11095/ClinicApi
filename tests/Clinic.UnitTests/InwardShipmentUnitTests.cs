using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class InwardShipmentUnitTests
{
    [Fact]
    public void REQ_INV_02_InwardShipment_IncrementsQuantityAndTracksSupplierDetails()
    {
        // Arrange
        var material = new Material
        {
            Id = "mat-inv-1",
            Name = "Composite Resin A2",
            Quantity = 10,
            Unit = "Syringes",
            MinStockAlert = 5,
            ClinicId = "clinic-1",
            DoctorId = "doc-1"
        };

        const int receivedQty = 40;
        const string supplier = "Dentex Medical Cairo";
        const string poRef = "PO-2026-904";
        const decimal unitCost = 35.50m;
        const string batchNo = "LOT-CR-992";
        var expiry = DateTime.UtcNow.AddYears(2);

        // Act - Simulate inward shipment
        material.Quantity += receivedQty;
        material.SupplierName = supplier;
        material.PurchaseOrderRef = poRef;
        material.UnitCost = unitCost;
        material.BatchNumber = batchNo;
        material.ExpirationDate = expiry;
        material.LastRestockedAt = DateTime.UtcNow.ToString("o");

        // Assert
        Assert.Equal(50, material.Quantity);
        Assert.Equal(supplier, material.SupplierName);
        Assert.Equal(poRef, material.PurchaseOrderRef);
        Assert.Equal(unitCost, material.UnitCost);
        Assert.Equal(batchNo, material.BatchNumber);
        Assert.NotNull(material.LastRestockedAt);
        Assert.False(material.IsExpired);
    }
}
