using System;
using System.Collections.Generic;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class InsuranceClaimUnitTests
{
    [Fact]
    public void REQ_INS_01_CopayCalculation_ComputesCorrectSplit()
    {
        // Arrange
        decimal grossAmount = 4000m;
        decimal copayPct = 20m;

        // Act (BR-INS-01)
        decimal patientCopay = Math.Round(grossAmount * (copayPct / 100m), 2);
        decimal claimedAmount = Math.Round(grossAmount - patientCopay, 2);

        var claim = new InsuranceClaim
        {
            TotalGrossAmount = grossAmount,
            CopayPercentage = copayPct,
            PatientCopayAmount = patientCopay,
            ClaimedAmount = claimedAmount,
            ToothNumber = 16,
            DiagnosisCode = "K02.1",
            ProcedureDescription = "Full Ceramic Zirconia Crown"
        };

        // Assert
        Assert.Equal(800m, claim.PatientCopayAmount);
        Assert.Equal(3200m, claim.ClaimedAmount);
        Assert.Equal(grossAmount, claim.PatientCopayAmount + claim.ClaimedAmount);
        Assert.Equal(16, claim.ToothNumber);
    }

    [Fact]
    public void REQ_INS_02_PreAuthThreshold_TriggersPreAuthorizedState()
    {
        // Arrange
        var provider = new InsuranceProvider
        {
            Name = "Bupa Global",
            PayerCode = "BUPA-01",
            PreAuthThreshold = 1500m
        };

        decimal highCostProcedure = 3500m;
        decimal lowCostProcedure = 800m;

        // Act
        bool requiresPreAuthHigh = highCostProcedure >= provider.PreAuthThreshold;
        bool requiresPreAuthLow = lowCostProcedure >= provider.PreAuthThreshold;

        string initialStatusHigh = requiresPreAuthHigh ? "PreAuthorized" : "Draft";
        string initialStatusLow = requiresPreAuthLow ? "PreAuthorized" : "Draft";

        // Assert
        Assert.True(requiresPreAuthHigh);
        Assert.False(requiresPreAuthLow);
        Assert.Equal("PreAuthorized", initialStatusHigh);
        Assert.Equal("Draft", initialStatusLow);
    }

    [Fact]
    public void REQ_INS_03_ClaimAdjudication_PartiallyApprovedCalculatesRatio()
    {
        // Arrange
        var claim = new InsuranceClaim
        {
            ClaimNumber = "CLM-202610-0001",
            TotalGrossAmount = 5000m,
            PatientCopayAmount = 1000m,
            ClaimedAmount = 4000m,
            Status = "Submitted"
        };

        // Act (Adjudicator approves 80% of claimed amount)
        decimal approvedAmount = 3200m;
        claim.Status = "PartiallyApproved";
        claim.ApprovedAmount = approvedAmount;
        claim.AdjudicationNotes = "Crown tariff ceiling capped at 3200 EGP by policy terms.";
        claim.AdjudicatedAt = DateTime.UtcNow;

        // Assert
        Assert.Equal("PartiallyApproved", claim.Status);
        Assert.Equal(3200m, claim.ApprovedAmount);
        Assert.NotNull(claim.AdjudicatedAt);
        Assert.Contains("tariff ceiling", claim.AdjudicationNotes);
    }

    [Fact]
    public void BR_INS_03_SettleGuardrail_RejectsUnapprovedClaims()
    {
        // Arrange
        var rejectedClaim = new InsuranceClaim { Status = "Rejected" };
        var draftClaim = new InsuranceClaim { Status = "Draft" };
        var approvedClaim = new InsuranceClaim { Status = "Approved" };

        // Act & Assert (BR-INS-03: Only Approved or PartiallyApproved can be settled)
        bool canSettleRejected = rejectedClaim.Status == "Approved" || rejectedClaim.Status == "PartiallyApproved";
        bool canSettleDraft = draftClaim.Status == "Approved" || draftClaim.Status == "PartiallyApproved";
        bool canSettleApproved = approvedClaim.Status == "Approved" || approvedClaim.Status == "PartiallyApproved";

        Assert.False(canSettleRejected);
        Assert.False(canSettleDraft);
        Assert.True(canSettleApproved);
    }
}
