using System;
using System.Linq;
using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Application.Services;
using Clinic.Domain.Entities;
using Moq;
using Xunit;

namespace Clinic.UnitTests;

public class EndodonticUnitTests
{
    private readonly Mock<IRadiologyCenterRepository> _centerRepoMock;
    private readonly Mock<IRadiologyRecordRepository> _recordRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly RadiologyService _service;

    public EndodonticUnitTests()
    {
        _centerRepoMock = new Mock<IRadiologyCenterRepository>();
        _recordRepoMock = new Mock<IRadiologyRecordRepository>();
        _userRepoMock = new Mock<IUserRepository>();

        _service = new RadiologyService(
            _centerRepoMock.Object,
            _recordRepoMock.Object,
            _userRepoMock.Object);
    }

    [Fact]
    public async Task REQ_ENDO_01_AnalyzeCanal_ReturnsValidLandmarksAndSchneiderAngle()
    {
        // Arrange
        var recordId = "rec-endo-01";
        _recordRepoMock.Setup(r => r.GetByIdAsync(recordId))
            .ReturnsAsync(new RadiologyRecord { Id = recordId, PatientId = "pat-10", ProcedureName = "Periapical Radiograph" });

        // Act
        var result = await _service.AnalyzeEndodonticCanalAsync(recordId, toothNumber: 36, canalName: "Mesio-Buccal (MB1)");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(36, result.ToothNumber);
        Assert.Equal("Mesio-Buccal (MB1)", result.Curvature.CanalName);
        Assert.True(result.Curvature.SchneiderAngleDegrees >= 0);
        Assert.True(result.Curvature.RadiusOfCurvatureMm > 0);
        Assert.Contains(result.Curvature.CurvatureClassification, new[] { "Mild", "Moderate", "Severe" });
        Assert.NotNull(result.RecommendedRecipe);
        Assert.NotEmpty(result.RecommendedRecipe.FileSequence);
    }

    [Fact]
    public async Task REQ_ENDO_01_SchneiderAngle_MildCurvature_ReturnsTruNatomyRecipe()
    {
        // Arrange: Points nearly collinear -> very small deviation (< 20 degrees)
        var recordId = "rec-endo-mild";
        var request = new RecalculateEndodonticCanalRequestDto
        {
            ToothNumber = 11,
            CanalName = "Central Incisor Main",
            PointA = new EndoPoint2DDto(100, 50),
            PointB = new EndoPoint2DDto(102, 150),
            PointC = new EndoPoint2DDto(105, 250),
            DiagnosisCondition = "VitalPulp",
            RadiographicRootLengthMm = 23.0
        };

        // Act
        var result = await _service.RecalculateEndodonticCanalAsync(recordId, request);

        // Assert
        Assert.True(result.Curvature.SchneiderAngleDegrees < 20.0);
        Assert.Equal("Mild", result.Curvature.CurvatureClassification);
        Assert.Equal("Low", result.Curvature.CyclicFatigueRiskLevel);
        Assert.Equal("TruNatomy System", result.RecommendedRecipe.SystemName);
        Assert.Null(result.RecommendedRecipe.CyclicFatigueWarning);
    }

    [Fact]
    public async Task REQ_ENDO_01_SchneiderAngle_SevereCurvature_TriggersCyclicFatigueWarningAndWaveOne()
    {
        // Arrange: Points with large angular deviation (> 35 degrees)
        var recordId = "rec-endo-severe";
        var request = new RecalculateEndodonticCanalRequestDto
        {
            ToothNumber = 46,
            CanalName = "Mesio-Lingual (ML)",
            PointA = new EndoPoint2DDto(100, 50),
            PointB = new EndoPoint2DDto(120, 160),
            PointC = new EndoPoint2DDto(220, 220), // sharp deviation > 35 degrees
            DiagnosisCondition = "NecroticWithApicalLesion",
            RadiographicRootLengthMm = 21.0
        };

        // Act
        var result = await _service.RecalculateEndodonticCanalAsync(recordId, request);

        // Assert
        Assert.True(result.Curvature.SchneiderAngleDegrees > 35.0);
        Assert.Equal("Severe", result.Curvature.CurvatureClassification);
        Assert.Equal("HighRiskOfSeparation", result.Curvature.CyclicFatigueRiskLevel);
        Assert.Equal("WaveOne Gold", result.RecommendedRecipe.SystemName);
        Assert.NotNull(result.RecommendedRecipe.CyclicFatigueWarning);
        Assert.Contains("Reciprocating", result.RecommendedRecipe.FileSequence[0].KinematicMotion);
    }

    [Fact]
    public async Task REQ_ENDO_02_WorkingLength_VitalPulp_Applies0_5mmSafetyOffset()
    {
        // Arrange
        var recordId = "rec-endo-vital";
        var request = new RecalculateEndodonticCanalRequestDto
        {
            ToothNumber = 24,
            CanalName = "Buccal",
            RadiographicRootLengthMm = 22.0,
            DiagnosisCondition = "VitalPulp"
        };

        // Act
        var result = await _service.RecalculateEndodonticCanalAsync(recordId, request);

        // Assert
        Assert.Equal(0.5, result.WorkingLength.ApicalSafetyOffsetMm);
        Assert.Equal(21.5, result.WorkingLength.CalculatedWorkingLengthMm); // 22.0 - 0.5
        Assert.Equal("StandardVitalApicalConstriction", result.WorkingLength.SafetyStatus);
    }

    [Fact]
    public async Task REQ_ENDO_02_WorkingLength_NecroticLesion_Applies1_0mmSafetyOffset()
    {
        // Arrange
        var recordId = "rec-endo-necrotic";
        var request = new RecalculateEndodonticCanalRequestDto
        {
            ToothNumber = 36,
            CanalName = "Distal",
            RadiographicRootLengthMm = 21.5,
            DiagnosisCondition = "NecroticWithApicalLesion"
        };

        // Act
        var result = await _service.RecalculateEndodonticCanalAsync(recordId, request);

        // Assert
        Assert.Equal(1.0, result.WorkingLength.ApicalSafetyOffsetMm);
        Assert.Equal(20.5, result.WorkingLength.CalculatedWorkingLengthMm); // 21.5 - 1.0
        Assert.Equal("EnhancedLesionSafetyBuffer", result.WorkingLength.SafetyStatus);
    }

    [Fact]
    public async Task REQ_ENDO_04_SaveEndodonticReport_PersistsClinicianVerificationAndNotes()
    {
        // Arrange
        var recordId = "rec-endo-save";
        _recordRepoMock.Setup(r => r.GetByIdAsync(recordId))
            .ReturnsAsync(new RadiologyRecord { Id = recordId, PatientId = "pat-22", ProcedureName = "PA Root Canal" });

        var saveRequest = new SaveEndodonticAnalysisRequestDto
        {
            ToothNumber = 36,
            CanalName = "Mesio-Buccal (MB1)",
            SchneiderAngleDegrees = 27.5,
            WorkingLengthMm = 20.5,
            SelectedFileSystem = "ProTaper Ultimate",
            DoctorNotes = "Established glide path with #10 hand file. Apex confirmed at 0.0 with Root ZX.",
            IsVerified = true
        };

        // Act
        var saved = await _service.SaveEndodonticReportAsync(recordId, saveRequest, "doc-dr-ahmed", "Dr. Ahmed Endodontist");

        // Assert
        Assert.NotNull(saved);
        Assert.True(saved.IsVerifiedByDoctor);
        Assert.Equal("Dr. Ahmed Endodontist", saved.VerifiedByDoctorName);
        Assert.Equal(27.5, saved.Curvature.SchneiderAngleDegrees);
        Assert.Equal(20.5, saved.WorkingLength.CalculatedWorkingLengthMm);
        Assert.Equal("Established glide path with #10 hand file. Apex confirmed at 0.0 with Root ZX.", saved.ClinicalNotes);
    }
}
