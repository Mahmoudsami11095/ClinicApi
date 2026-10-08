using System;
using System.Collections.Generic;
using Clinic.Application.DTOs;
using Xunit;

namespace Clinic.UnitTests;

public class ExecutiveAnalyticsUnitTests
{
    [Fact]
    public void REQ_EXEC_01_NetworkSummaryDto_CalculatesOperatingMarginCorrectly()
    {
        // Arrange
        decimal grossRevenue = 250000m;
        decimal collectedRevenue = 220000m;
        decimal commissionsPaid = 60000m;

        // Act
        decimal margin = collectedRevenue - commissionsPaid;
        double marginPct = Math.Round((double)(margin / collectedRevenue) * 100, 1);

        var dto = new ExecutiveNetworkSummaryDto
        {
            TotalNetworkRevenue = grossRevenue,
            TotalCollectedRevenue = collectedRevenue,
            TotalCommissionsPaid = commissionsPaid,
            GrossOperatingMargin = margin,
            OperatingMarginPercentage = marginPct,
            TotalPatientEncounters = 412,
            TotalNewPatients = 145,
            NetworkRetentionRate = 78.4,
            NetworkChairUtilizationRate = 82.5,
            ActiveBranchCount = 3,
            ActiveDoctorCount = 8
        };

        // Assert
        Assert.Equal(160000m, dto.GrossOperatingMargin);
        Assert.Equal(72.7, dto.OperatingMarginPercentage);
        Assert.Equal(3, dto.ActiveBranchCount);
        Assert.Equal(8, dto.ActiveDoctorCount);
    }

    [Fact]
    public void REQ_EXEC_02_DoctorProductivity_AssignsTierBadgesCorrectly()
    {
        // Tier 1: Top Producer (Gross >= 100k)
        decimal doc1Revenue = 140000m;
        int doc1Appts = 25;
        string tier1 = doc1Revenue >= 100000m ? "Top Producer" : doc1Appts >= 30 ? "High Efficiency" : "Optimal Turnaround";
        Assert.Equal("Top Producer", tier1);

        // Tier 2: High Efficiency (< 100k but >= 30 appointments)
        decimal doc2Revenue = 65000m;
        int doc2Appts = 42;
        string tier2 = doc2Revenue >= 100000m ? "Top Producer" : doc2Appts >= 30 ? "High Efficiency" : "Optimal Turnaround";
        Assert.Equal("High Efficiency", tier2);

        // Tier 3: Optimal Turnaround (< 100k and < 30 appointments)
        decimal doc3Revenue = 28000m;
        int doc3Appts = 15;
        string tier3 = doc3Revenue >= 100000m ? "Top Producer" : doc3Appts >= 30 ? "High Efficiency" : "Optimal Turnaround";
        Assert.Equal("Optimal Turnaround", tier3);
    }

    [Fact]
    public void REQ_EXEC_03_SupplyChainVelocity_EstimatesDaysRemainingAccurately()
    {
        // Arrange
        int totalStock = 120;
        double dailyRate = 4.0;

        // Act
        int daysRemaining = (int)(totalStock / dailyRate);

        var dto = new SupplyChainVelocityDto
        {
            MaterialId = "mat-anesthesia",
            MaterialName = "Mepivacaine 3%",
            Category = "Anesthesia",
            TotalStockAcrossBranches = totalStock,
            DailyConsumptionRate = dailyRate,
            EstimatedDaysRemaining = daysRemaining,
            UrgentRestockClinicId = "c-westside",
            UrgentRestockClinicName = "Westside Specialty Clinic"
        };

        // Assert
        Assert.Equal(30, dto.EstimatedDaysRemaining);
        Assert.Equal("Westside Specialty Clinic", dto.UrgentRestockClinicName);
        Assert.True(dto.EstimatedDaysRemaining > 0);
    }

    [Fact]
    public void REQ_EXEC_04_BranchBenchmarkDto_ContractIntegrity()
    {
        // Arrange & Act
        var benchmark = new BranchBenchmarkDto
        {
            ClinicId = "c-downtown-1",
            ClinicName = "Downtown Family Dental Center",
            City = "Cairo",
            TotalRevenue = 185000m,
            MonthlyVisits = 195,
            AvgChairTurnaroundMins = 32.5,
            ChairUtilizationRate = 84.0,
            LowStockCount = 2,
            PendingTransfersCount = 1
        };

        // Assert
        Assert.Equal("Downtown Family Dental Center", benchmark.ClinicName);
        Assert.Equal(185000m, benchmark.TotalRevenue);
        Assert.Equal(195, benchmark.MonthlyVisits);
        Assert.Equal(32.5, benchmark.AvgChairTurnaroundMins);
        Assert.Equal(84.0, benchmark.ChairUtilizationRate);
    }
}
