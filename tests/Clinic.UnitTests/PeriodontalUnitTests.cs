using System.Collections.Generic;
using Clinic.Application.DTOs;
using Clinic.Application.Services;
using Xunit;

namespace Clinic.UnitTests;

public class PeriodontalUnitTests
{
    private static PeriodontalToothDto CreateTooth(int number, int ppd, int cal, bool bop, int furcation = 0, int mobility = 0)
    {
        var sites = new List<PeriodontalSiteDto>
        {
            new("Distobuccal", ppd, 0, cal, bop, false),
            new("Midbuccal", ppd, 0, cal, false, false),
            new("Mesiobuccal", ppd, 0, cal, bop, false),
            new("Distolingual", ppd, 0, cal, bop, false),
            new("Midlingual", ppd, 0, cal, false, false),
            new("Mesiolingual", ppd, 0, cal, bop, false)
        };

        return new PeriodontalToothDto(
            ToothNumber: number,
            IsMissing: false,
            IsImplant: false,
            FurcationGrade: furcation,
            MobilityGrade: mobility,
            Sites: sites
        );
    }

    [Fact]
    public void BR_PERIO_01_EvaluatePeriodontalStaging_PristineHealth_WhenNoLossAndLowBop()
    {
        // Arrange
        var teeth = new List<PeriodontalToothDto>
        {
            CreateTooth(11, 2, 0, false),
            CreateTooth(21, 2, 0, false)
        };

        // Act
        var result = PeriodontalAssessmentService.EvaluatePeriodontalStaging(
            patientAge: 30,
            boneLossPercentage: 0.0,
            isSmoker: false,
            cigarettesPerDay: 0,
            hbA1c: null,
            teeth: teeth
        );

        // Assert
        Assert.Equal("Health", result.Stage);
        Assert.Equal("Healthy", result.InflammationExtent);
        Assert.Equal(0.0, result.BopPercentage);
        Assert.Equal(0, result.MaxCalMm);
        Assert.Equal(2, result.MaxPpdMm);
    }

    [Fact]
    public void BR_PERIO_01_EvaluatePeriodontalStaging_StageI_InitialPeriodontitis()
    {
        // Max PPD = 4mm, Max CAL = 2mm
        var teeth = new List<PeriodontalToothDto>
        {
            CreateTooth(11, 3, 0, false),
            CreateTooth(21, 4, 2, false), // PPD 4, CAL 2
            CreateTooth(16, 3, 2, true)   // CAL 2
        };

        var result = PeriodontalAssessmentService.EvaluatePeriodontalStaging(
            patientAge: 35,
            boneLossPercentage: 8.0,
            isSmoker: false,
            cigarettesPerDay: 0,
            hbA1c: null,
            teeth: teeth
        );

        Assert.Equal("Stage I", result.Stage);
        Assert.Equal(2, result.MaxCalMm);
        Assert.Equal(4, result.MaxPpdMm);
        Assert.Equal("Grade A", result.Grade);
    }

    [Fact]
    public void BR_PERIO_01_EvaluatePeriodontalStaging_StageII_ModeratePeriodontitis()
    {
        // Max PPD = 5mm, Max CAL = 4mm
        var teeth = new List<PeriodontalToothDto>
        {
            CreateTooth(16, 5, 4, true), // PPD 5, CAL 4
            CreateTooth(26, 4, 0, false)
        };

        var result = PeriodontalAssessmentService.EvaluatePeriodontalStaging(
            patientAge: 40,
            boneLossPercentage: 20.0, // 20/40 = 0.50 -> Grade B
            isSmoker: false,
            cigarettesPerDay: 0,
            hbA1c: null,
            teeth: teeth
        );

        Assert.Equal("Stage II", result.Stage);
        Assert.Equal(4, result.MaxCalMm);
        Assert.Equal(5, result.MaxPpdMm);
        Assert.Equal("Grade B", result.Grade);
    }

    [Fact]
    public void BR_PERIO_01_EvaluatePeriodontalStaging_StageIII_SeverePeriodontitis()
    {
        // Max PPD = 7mm, Max CAL = 6mm, Furcation Grade 1
        var teeth = new List<PeriodontalToothDto>
        {
            CreateTooth(16, 7, 6, true, furcation: 1), // PPD 7, CAL 6
            CreateTooth(46, 6, 5, true)
        };

        var result = PeriodontalAssessmentService.EvaluatePeriodontalStaging(
            patientAge: 50,
            boneLossPercentage: 45.0,
            isSmoker: false,
            cigarettesPerDay: 0,
            hbA1c: null,
            teeth: teeth
        );

        Assert.Equal("Stage III", result.Stage);
        Assert.Equal(6, result.MaxCalMm);
        Assert.Equal(7, result.MaxPpdMm);
        Assert.Equal(1, result.FurcationTeethCount);
    }

    [Fact]
    public void BR_PERIO_01_EvaluatePeriodontalStaging_StageIV_AdvancedPeriodontitis()
    {
        // Max CAL >= 5, with severe furcation and mobile teeth
        var teeth = new List<PeriodontalToothDto>
        {
            CreateTooth(16, 8, 6, true, furcation: 2, mobility: 2),
            CreateTooth(26, 7, 6, true, furcation: 2, mobility: 2),
            CreateTooth(36, 8, 6, true, furcation: 2, mobility: 2),
            CreateTooth(46, 7, 6, true, furcation: 1, mobility: 2)
        };

        var result = PeriodontalAssessmentService.EvaluatePeriodontalStaging(
            patientAge: 60,
            boneLossPercentage: 70.0,
            isSmoker: true,
            cigarettesPerDay: 15,
            hbA1c: 8.2,
            teeth: teeth
        );

        Assert.Equal("Stage IV", result.Stage);
        Assert.Equal("Grade C", result.Grade);
        Assert.True(result.FurcationTeethCount >= 3);
        Assert.True(result.MobileTeethCount >= 4);
    }

    [Fact]
    public void BR_PERIO_01_Grading_TriggersGradeC_OnHeavySmokingOrHighHbA1c()
    {
        var teeth = new List<PeriodontalToothDto> { CreateTooth(11, 3, 0, false) };

        // Even with low bone loss ratio, >=10 cigarettes/day mandates Grade C
        var smokerResult = PeriodontalAssessmentService.EvaluatePeriodontalStaging(
            patientAge: 40,
            boneLossPercentage: 5.0,
            isSmoker: true,
            cigarettesPerDay: 12,
            hbA1c: 5.4,
            teeth: teeth
        );
        Assert.Equal("Grade C", smokerResult.Grade);

        // Even without smoking, HbA1c >= 7.0% mandates Grade C
        var diabeticResult = PeriodontalAssessmentService.EvaluatePeriodontalStaging(
            patientAge: 50,
            boneLossPercentage: 10.0,
            isSmoker: false,
            cigarettesPerDay: 0,
            hbA1c: 7.5,
            teeth: teeth
        );
        Assert.Equal("Grade C", diabeticResult.Grade);
    }

    [Fact]
    public void BR_PERIO_02_BopPercentage_CalculatesExactProportionAndInflammationThresholds()
    {
        // 2 teeth x 6 sites = 12 sites total
        // Tooth 1: 4 bleeding sites. Tooth 2: 0 bleeding sites. Total bleeding = 4 / 12 = 33.3% (> 30% Generalized)
        var t1 = CreateTooth(11, 3, 0, true);
        var t2 = CreateTooth(21, 3, 0, false);
        var teeth = new List<PeriodontalToothDto> { t1, t2 };

        var result = PeriodontalAssessmentService.EvaluatePeriodontalStaging(
            patientAge: 25,
            boneLossPercentage: 0.0,
            isSmoker: false,
            cigarettesPerDay: 0,
            hbA1c: null,
            teeth: teeth
        );

        Assert.Equal(33.3, result.BopPercentage);
        Assert.Equal("GeneralizedGingivitis", result.InflammationExtent);
        Assert.Equal("Gingivitis", result.Stage);
    }
}
