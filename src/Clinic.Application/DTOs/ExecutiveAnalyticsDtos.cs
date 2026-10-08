using System;
using System.Collections.Generic;

namespace Clinic.Application.DTOs;

public class ExecutiveNetworkSummaryDto
{
    public decimal TotalNetworkRevenue { get; set; }
    public decimal TotalCollectedRevenue { get; set; }
    public decimal TotalCommissionsPaid { get; set; }
    public decimal GrossOperatingMargin { get; set; }
    public double OperatingMarginPercentage { get; set; }

    public int TotalPatientEncounters { get; set; }
    public int TotalNewPatients { get; set; }
    public double NetworkRetentionRate { get; set; }
    public double NetworkChairUtilizationRate { get; set; }

    public int ActiveBranchCount { get; set; }
    public int ActiveDoctorCount { get; set; }
    public DateTime AggregatedAt { get; set; } = DateTime.UtcNow;
}

public class BranchBenchmarkDto
{
    public string ClinicId { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int MonthlyVisits { get; set; }
    public double AvgChairTurnaroundMins { get; set; }
    public double ChairUtilizationRate { get; set; }
    public int LowStockCount { get; set; }
    public int PendingTransfersCount { get; set; }
}

public class DoctorProductivityDto
{
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public int TotalProcedures { get; set; }
    public decimal TotalGrossRevenue { get; set; }
    public decimal NetDoctorCommission { get; set; }
    public double AvgEncounterMins { get; set; }
    public string TierBadge { get; set; } = "High Efficiency";
}

public class SupplyChainVelocityDto
{
    public string MaterialId { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int TotalStockAcrossBranches { get; set; }
    public double DailyConsumptionRate { get; set; }
    public int EstimatedDaysRemaining { get; set; }
    public string? UrgentRestockClinicId { get; set; }
    public string? UrgentRestockClinicName { get; set; }
}
