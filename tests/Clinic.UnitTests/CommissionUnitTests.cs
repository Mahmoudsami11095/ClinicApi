using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Clinic.Domain.Helpers;
using Xunit;

namespace Clinic.UnitTests;

public class CommissionUnitTests
{
    [Fact]
    public void CalculateNetCommission_BeforeCommission_DeductsLabFeeBeforeApplyingRate()
    {
        // Net Revenue = Gross - LabFee; Commission = Net * Rate
        // 1000 EGP Gross, 200 EGP Lab Fee, 30% Rate => (1000 - 200) * 0.30 = 240 EGP
        var commission = CommissionCalculator.CalculateNetCommission(1000m, 200m, 30.0m, "BeforeCommission");
        Assert.Equal(240.00m, commission);
    }

    [Fact]
    public void CalculateNetCommission_AfterCommission_DeductsLabFeeDirectlyFromCommission()
    {
        // Commission = (Gross * Rate) - LabFee
        // 1000 EGP Gross, 100 EGP Lab Fee, 30% Rate => (1000 * 0.30) - 100 = 200 EGP
        var commission = CommissionCalculator.CalculateNetCommission(1000m, 100m, 30.0m, "AfterCommission");
        Assert.Equal(200.00m, commission);
    }

    [Fact]
    public void CalculateNetCommission_None_ClinicAbsorbsLabFee()
    {
        // Commission = Gross * Rate
        // 1000 EGP Gross, 250 EGP Lab Fee, 35% Rate => 1000 * 0.35 = 350 EGP
        var commission = CommissionCalculator.CalculateNetCommission(1000m, 250m, 35.0m, "None");
        Assert.Equal(350.00m, commission);
    }

    [Fact]
    public void CalculateNetCommission_LabFeeExceedsGross_DoesNotReturnNegative()
    {
        // Gross 100, Lab fee 150 => should be 0.00m, never negative
        var commBefore = CommissionCalculator.CalculateNetCommission(100m, 150m, 30.0m, "BeforeCommission");
        var commAfter = CommissionCalculator.CalculateNetCommission(100m, 150m, 30.0m, "AfterCommission");

        Assert.Equal(0.00m, commBefore);
        Assert.Equal(0.00m, commAfter);
    }

    [Fact]
    public void CalculateNetCommission_ZeroGross_ReturnsZero()
    {
        var comm = CommissionCalculator.CalculateNetCommission(0m, 50m, 30.0m, "BeforeCommission");
        Assert.Equal(0.00m, comm);
    }

    [Theory]
    [InlineData("Zirconia Crown on tooth 16", "Restorative", 250.0)]
    [InlineData("Full Arch Denture Acrylic", "Prosthodontics", 300.0)]
    [InlineData("Titanium Implant Placement", "Implantology", 200.0)]
    [InlineData("Clear Aligners Phase 1", "Orthodontics", 150.0)]
    [InlineData("Routine Consultation & Exam", "General", 0.0)]
    public void CalculateLabFee_AccuratelyCalculatesLabDeductions(string description, string category, decimal expectedFee)
    {
        var fee = CommissionCalculator.CalculateLabFee(1000m, category, description);
        Assert.Equal(expectedFee, fee);
    }

    [Theory]
    [InlineData("Molar Root Canal Therapy tooth 46", "Dentist", "Endodontics")]
    [InlineData("Ceramic Crown Cementation", "Dentist", "Implantology")]
    [InlineData("Fixed Metal Braces Adjustment", "Dentist", "Orthodontics")]
    [InlineData("Surgical Wisdom Tooth Extraction", "Dentist", "Surgery")]
    [InlineData("Class II Composite Restoration", "Dentist", "Restorative")]
    [InlineData("Ultrasonic Scaling and Polishing", "Dentist", "Preventive")]
    [InlineData("General Checkup", "Orthodontist", "Orthodontics")]
    [InlineData("General Checkup", "General Practice", "General Consultation")]
    public void DeduceCategory_CorrectlyCategorizesClinicalProcedures(string description, string doctorSpec, string expectedCategory)
    {
        var category = CommissionCalculator.DeduceCategory(description, doctorSpec);
        Assert.Equal(expectedCategory, category);
    }
}
