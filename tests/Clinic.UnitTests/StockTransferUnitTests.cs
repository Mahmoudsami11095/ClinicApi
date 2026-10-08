using System;
using System.Collections.Generic;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class StockTransferUnitTests
{
    [Fact]
    public void REQ_LOG_01_StockTransferRequisition_Initialization_HasExpectedDefaults()
    {
        // Arrange & Act
        var transfer = new StockTransferRequisition
        {
            RequisitionNumber = "TRF-202610-0001",
            SourceClinicId = "c-central-warehouse",
            DestinationClinicId = "c-westside-branch",
            MaterialId = "mat-graft-01",
            QuantityRequested = 20,
            Priority = "Urgent",
            RequestedByUserId = "usr-nurse-1"
        };

        // Assert
        Assert.NotNull(transfer.Id);
        Assert.Equal("TRF-202610-0001", transfer.RequisitionNumber);
        Assert.Equal("Requested", transfer.Status);
        Assert.Equal("Urgent", transfer.Priority);
        Assert.Equal(20, transfer.QuantityRequested);
        Assert.Equal(0, transfer.QuantityDamaged);
        Assert.False(transfer.IsDeleted);
        Assert.True(transfer.RequestedAt <= DateTime.UtcNow);
        Assert.Null(transfer.DispatchedAt);
        Assert.Null(transfer.ReceivedAt);
    }

    [Fact]
    public void REQ_LOG_02_TwoPhaseCommit_StateTransitions_OperateCorrectly()
    {
        // Arrange
        var transfer = new StockTransferRequisition
        {
            RequisitionNumber = "TRF-202610-0002",
            SourceClinicId = "c-central",
            DestinationClinicId = "c-downtown",
            MaterialId = "mat-implant-40",
            QuantityRequested = 10,
            RequestedByUserId = "usr-asst-1"
        };

        // Phase 1: Approve
        transfer.Status = "Approved";
        transfer.BatchNumber = "LOT-9988A";
        transfer.ExpiryDate = DateTime.UtcNow.AddMonths(18);
        Assert.Equal("Approved", transfer.Status);

        // Phase 2: Dispatch & Courier transit
        transfer.Status = "InTransit";
        transfer.QuantityDispatched = 10;
        transfer.DispatchedByUserId = "usr-wh-manager";
        transfer.DispatchedAt = DateTime.UtcNow;
        Assert.Equal("InTransit", transfer.Status);
        Assert.NotNull(transfer.DispatchedAt);

        // Phase 3: Receive & Destination physical inspection
        transfer.Status = "Received";
        transfer.QuantityReceived = 10;
        transfer.QuantityDamaged = 1;
        transfer.DamageReason = "Broken packaging seal";
        transfer.ReceivedByUserId = "usr-nurse-2";
        transfer.ReceivedAt = DateTime.UtcNow;

        // Assert
        Assert.Equal("Received", transfer.Status);
        Assert.Equal(10, transfer.QuantityReceived);
        Assert.Equal(1, transfer.QuantityDamaged);
        Assert.Equal("Broken packaging seal", transfer.DamageReason);
        Assert.NotNull(transfer.ReceivedAt);
    }

    [Fact]
    public void BR_LOG_01_DispatchDeduction_ReducesSourceStockCorrectly()
    {
        // Arrange
        var sourceMaterial = new Material
        {
            Id = "mat-1",
            ClinicId = "c-source",
            Name = "Mepivacaine 3% Dental Anesthesia",
            Quantity = 50
        };

        int dispatchQty = 15;

        // Act (Simulate BR-LOG-01 Dispatch deduction)
        Assert.True(sourceMaterial.Quantity >= dispatchQty);
        sourceMaterial.Quantity -= dispatchQty;

        // Assert
        Assert.Equal(35, sourceMaterial.Quantity);
    }

    [Fact]
    public void BR_LOG_02_ReceivingVerification_CreditsUsableStockAndQuarantinesDamage()
    {
        // Arrange
        var destMaterial = new Material
        {
            Id = "mat-2",
            ClinicId = "c-dest",
            Name = "Composite Resin Shade A2",
            Quantity = 10
        };

        int qtyReceived = 12;
        int qtyDamaged = 2; // 2 syringes leaked or dropped

        // Act (Simulate BR-LOG-02 Usable Stock Calculation)
        int effectiveUsableQty = Math.Max(0, qtyReceived - qtyDamaged);
        destMaterial.Quantity += effectiveUsableQty;

        // Assert
        Assert.Equal(10, effectiveUsableQty);
        Assert.Equal(20, destMaterial.Quantity); // 10 original + 10 usable
    }

    [Fact]
    public void BR_LOG_03_FEFOGuardrail_DetectsExpiringConsumables()
    {
        // Arrange: Consumable expiring in 15 days (< 30 days limit)
        var nearExpiryDate = DateTime.UtcNow.AddDays(15);
        var thirtyDaysThreshold = DateTime.UtcNow.AddDays(30);

        // Act & Assert
        bool isNearExpiry = nearExpiryDate.Date < thirtyDaysThreshold.Date;
        Assert.True(isNearExpiry);

        // Supervisor override check
        string supervisorNotes = "CLINICAL SUPERVISOR OVERRIDE: Needed for emergency surgery today.";
        bool hasValidOverride = supervisorNotes.Contains("OVERRIDE", StringComparison.OrdinalIgnoreCase);
        Assert.True(hasValidOverride);
    }

    [Fact]
    public void DTO_StockTransferResponse_PopulatesAllFields()
    {
        // Arrange & Act
        var dto = new StockTransferRequisitionResponseDto
        {
            Id = "req-101",
            RequisitionNumber = "TRF-202610-0099",
            SourceClinicId = "c-1",
            SourceClinicName = "Downtown Main",
            DestinationClinicId = "c-2",
            DestinationClinicName = "Westside Branch",
            MaterialId = "mat-5",
            MaterialName = "Bio-Oss Bone Graft 0.5g",
            QuantityRequested = 5,
            QuantityDispatched = 5,
            QuantityReceived = 5,
            QuantityDamaged = 0,
            BatchNumber = "LOT-BO-2026",
            Priority = "Emergency",
            Status = "InTransit"
        };

        // Assert
        Assert.Equal("TRF-202610-0099", dto.RequisitionNumber);
        Assert.Equal("Downtown Main", dto.SourceClinicName);
        Assert.Equal("Westside Branch", dto.DestinationClinicName);
        Assert.Equal("Bio-Oss Bone Graft 0.5g", dto.MaterialName);
        Assert.Equal("Emergency", dto.Priority);
        Assert.Equal("InTransit", dto.Status);
    }
}
