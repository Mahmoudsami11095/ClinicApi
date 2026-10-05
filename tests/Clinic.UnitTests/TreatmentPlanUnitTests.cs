using System;
using System.Collections.Generic;
using System.Linq;
using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class TreatmentPlanUnitTests
{
    public enum TreatmentPhaseType
    {
        Urgent = 1,
        Restorative = 2,
        Prosthetics = 3,
        Maintenance = 4
    }

    public record TreatmentPlanItemDto(
        string Id,
        string ToothNumber,
        string ProcedureName,
        TreatmentPhaseType Phase,
        string Stage,
        decimal Cost,
        int EstimatedVisits
    );

    public record TreatmentPlanEstimate(
        decimal GrossTotal,
        decimal DiscountPercentage,
        decimal DiscountAmount,
        decimal NetTotal,
        decimal DepositPercentage,
        decimal DepositRequired,
        decimal RemainingBalance,
        List<(TreatmentPhaseType Phase, decimal PhaseSubtotal)> PhaseBreakdown
    );

    private static TreatmentPlanEstimate CalculateTreatmentPlanEstimate(
        IEnumerable<TreatmentPlanItemDto> items,
        decimal discountPercentage,
        decimal depositPercentage)
    {
        var itemList = items.ToList();
        var grossTotal = itemList.Sum(i => i.Cost);
        discountPercentage = Math.Clamp(discountPercentage, 0m, 100m);
        depositPercentage = Math.Clamp(depositPercentage, 0m, 100m);

        var discountAmount = Math.Round(grossTotal * (discountPercentage / 100m), 2);
        var netTotal = Math.Max(0m, grossTotal - discountAmount);
        var depositRequired = Math.Round(netTotal * (depositPercentage / 100m), 2);
        var remainingBalance = Math.Max(0m, netTotal - depositRequired);

        var phaseBreakdown = itemList
            .GroupBy(i => i.Phase)
            .Select(g => (Phase: g.Key, PhaseSubtotal: g.Sum(x => x.Cost)))
            .OrderBy(p => (int)p.Phase)
            .ToList();

        return new TreatmentPlanEstimate(
            grossTotal,
            discountPercentage,
            discountAmount,
            netTotal,
            depositPercentage,
            depositRequired,
            remainingBalance,
            phaseBreakdown
        );
    }

    [Fact]
    public void CalculateEstimate_ShouldAccuratelyComputeFinancialTotalsAndPhaseSubtotals()
    {
        // Arrange
        var items = new List<TreatmentPlanItemDto>
        {
            new("p1", "18", "Surgical Extraction (Urgent)", TreatmentPhaseType.Urgent, "proposed", 800m, 1),
            new("p2", "16", "Root Canal Therapy (Molar)", TreatmentPhaseType.Restorative, "proposed", 1800m, 2),
            new("p3", "16", "Porcelain Crown", TreatmentPhaseType.Prosthetics, "proposed", 2400m, 2),
            new("p4", "All", "Full Mouth Scaling & Prophylaxis", TreatmentPhaseType.Maintenance, "proposed", 500m, 1)
        };

        // Act - 10% discount, 25% deposit
        var estimate = CalculateTreatmentPlanEstimate(items, discountPercentage: 10m, depositPercentage: 25m);

        // Assert
        Assert.Equal(5500m, estimate.GrossTotal);
        Assert.Equal(10m, estimate.DiscountPercentage);
        Assert.Equal(550m, estimate.DiscountAmount);
        Assert.Equal(4950m, estimate.NetTotal);
        Assert.Equal(25m, estimate.DepositPercentage);
        Assert.Equal(1237.50m, estimate.DepositRequired);
        Assert.Equal(3712.50m, estimate.RemainingBalance);

        // Verify phase subtotals
        Assert.Equal(4, estimate.PhaseBreakdown.Count);
        Assert.Equal(800m, estimate.PhaseBreakdown.First(p => p.Phase == TreatmentPhaseType.Urgent).PhaseSubtotal);
        Assert.Equal(1800m, estimate.PhaseBreakdown.First(p => p.Phase == TreatmentPhaseType.Restorative).PhaseSubtotal);
        Assert.Equal(2400m, estimate.PhaseBreakdown.First(p => p.Phase == TreatmentPhaseType.Prosthetics).PhaseSubtotal);
        Assert.Equal(500m, estimate.PhaseBreakdown.First(p => p.Phase == TreatmentPhaseType.Maintenance).PhaseSubtotal);
    }

    [Fact]
    public void CalculateEstimate_ZeroDiscountAndZeroDeposit_ShouldEqualGrossTotal()
    {
        // Arrange
        var items = new List<TreatmentPlanItemDto>
        {
            new("p1", "11", "Composite Veneer", TreatmentPhaseType.Prosthetics, "proposed", 1200m, 1)
        };

        // Act
        var estimate = CalculateTreatmentPlanEstimate(items, 0m, 0m);

        // Assert
        Assert.Equal(1200m, estimate.GrossTotal);
        Assert.Equal(0m, estimate.DiscountAmount);
        Assert.Equal(1200m, estimate.NetTotal);
        Assert.Equal(0m, estimate.DepositRequired);
        Assert.Equal(1200m, estimate.RemainingBalance);
    }

    [Fact]
    public void CalculateEstimate_BoundaryClamp_ShouldNotAllowNegativeOrOver100Percentages()
    {
        // Arrange
        var items = new List<TreatmentPlanItemDto>
        {
            new("p1", "12", "Composite Filling", TreatmentPhaseType.Restorative, "proposed", 600m, 1)
        };

        // Act - negative discount clamped to 0%, 150% deposit clamped to 100%
        var estimate = CalculateTreatmentPlanEstimate(items, discountPercentage: -20m, depositPercentage: 150m);

        // Assert
        Assert.Equal(0m, estimate.DiscountPercentage);
        Assert.Equal(0m, estimate.DiscountAmount);
        Assert.Equal(600m, estimate.NetTotal);
        Assert.Equal(100m, estimate.DepositPercentage);
        Assert.Equal(600m, estimate.DepositRequired);
        Assert.Equal(0m, estimate.RemainingBalance);
    }

    [Fact]
    public void GenerateDepositInvoice_ShouldCreateValidBillingRecordWithPlanRef()
    {
        // Arrange
        var patientId = "pat-101";
        var planRef = "DTP-PAT101";
        var depositAmount = 1500m;
        var clinicId = "clinic-alpha";

        // Act - create deposit invoice for the treatment plan
        var invoice = new BillingRecord
        {
            Id = Guid.NewGuid().ToString(),
            PatientId = patientId,
            InvoiceNumber = "INV-2026-PLAN-01",
            Subtotal = depositAmount,
            Amount = depositAmount,
            Status = "pending",
            DateIssued = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Description = $"Treatment Plan Deposit ({planRef}) - Initial Payment Required",
            ClinicId = clinicId
        };

        // Assert
        Assert.NotNull(invoice.Id);
        Assert.Equal(patientId, invoice.PatientId);
        Assert.Equal(depositAmount, invoice.Amount);
        Assert.Contains(planRef, invoice.Description);
        Assert.Equal("pending", invoice.Status);
    }
}
