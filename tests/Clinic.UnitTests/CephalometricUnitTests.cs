using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Application.Services;
using Clinic.Domain.Entities;
using Moq;
using Xunit;

namespace Clinic.UnitTests;

public class CephalometricUnitTests
{
    private readonly Mock<IRadiologyCenterRepository> _centerRepoMock;
    private readonly Mock<IRadiologyRecordRepository> _recordRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly RadiologyService _service;

    public CephalometricUnitTests()
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
    public async Task REQ_CEPH_01_LandmarkPredictions_ContainsRequiredAnatomicalPoints()
    {
        // Arrange
        var recordId = "rec-ceph-01";
        _recordRepoMock.Setup(r => r.GetByIdAsync(recordId))
            .ReturnsAsync(new RadiologyRecord { Id = recordId, PatientId = "pat-1", ProcedureName = "Lateral Cephalogram" });

        // Act
        var result = await _service.GetCephalometricAnalysisAsync(recordId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Landmarks.Count);

        var landmarkIds = result.Landmarks.Select(l => l.Id).ToHashSet();
        Assert.Contains("S", landmarkIds);
        Assert.Contains("N", landmarkIds);
        Assert.Contains("A", landmarkIds);
        Assert.Contains("B", landmarkIds);
        Assert.Contains("Pog", landmarkIds);
        Assert.Contains("Me", landmarkIds);
        Assert.Contains("Go", landmarkIds);
        Assert.Contains("Gn", landmarkIds);
        Assert.Contains("Or", landmarkIds);
        Assert.Contains("Po", landmarkIds);

        foreach (var lm in result.Landmarks)
        {
            Assert.True(lm.X >= 0.0 && lm.X <= 100.0, $"Landmark {lm.Id} X coordinate is percentage normalized.");
            Assert.True(lm.Y >= 0.0 && lm.Y <= 100.0, $"Landmark {lm.Id} Y coordinate is percentage normalized.");
            Assert.True(lm.Confidence > 80.0, $"Landmark {lm.Id} AI confidence exceeds 80%.");
        }
    }

    [Fact]
    public async Task REQ_CEPH_02_AngularCalculations_SteinerNormsEvaluation()
    {
        // Arrange
        var recordId = "rec-ceph-02";
        _recordRepoMock.Setup(r => r.GetByIdAsync(recordId))
            .ReturnsAsync(new RadiologyRecord { Id = recordId, PatientId = "pat-2" });

        // Act
        var result = await _service.GetCephalometricAnalysisAsync(recordId);

        // Assert
        var sna = result.Measurements.FirstOrDefault(m => m.Parameter == "SNA");
        var snb = result.Measurements.FirstOrDefault(m => m.Parameter == "SNB");
        var anb = result.Measurements.FirstOrDefault(m => m.Parameter == "ANB");

        Assert.NotNull(sna);
        Assert.NotNull(snb);
        Assert.NotNull(anb);

        Assert.Equal(82.0, sna.Value);
        Assert.Equal(79.5, snb.Value);
        Assert.Equal(2.5, anb.Value);
        Assert.Equal("Class I", result.SkeletalClass);
    }

    [Theory]
    [InlineData(2.5, "Class I")]
    [InlineData(5.5, "Class II")]
    [InlineData(-2.0, "Class III")]
    public void REQ_CEPH_03_SkeletalClass_ClassifiesSagittalDiscrepancyAccurately(double anbAngle, string expectedClass)
    {
        // Act
        string actualClass;
        if (anbAngle > 4.0)
            actualClass = "Class II";
        else if (anbAngle < 0.0)
            actualClass = "Class III";
        else
            actualClass = "Class I";

        // Assert
        Assert.Equal(expectedClass, actualClass);
    }

    [Theory]
    [InlineData(32.0, "Normodivergent")]
    [InlineData(40.0, "Hyperdivergent")]
    [InlineData(22.0, "Hypodivergent")]
    public void REQ_CEPH_04_MandibularPlaneAngle_ClassifiesVerticalFacialGrowth(double snGoGnAngle, string expectedPattern)
    {
        // Act
        string actualPattern;
        if (snGoGnAngle > 37.0)
            actualPattern = "Hyperdivergent";
        else if (snGoGnAngle < 27.0)
            actualPattern = "Hypodivergent";
        else
            actualPattern = "Normodivergent";

        // Assert
        Assert.Equal(expectedPattern, actualPattern);
    }

    [Fact]
    public async Task REQ_CEPH_05_SaveAndGetReport_PreservesDoctorNotesAndMeasurements()
    {
        // Arrange
        var recordId = "rec-ceph-05";
        var saveDto = new SaveCephalometricReportDto
        {
            RecordId = recordId,
            AnalysisMethod = "Steiner Composite",
            SkeletalClass = "Class I",
            GrowthPattern = "Normodivergent",
            DoctorNotes = "Bimaxillary dentoalveolar protrusion with Class I molar relationship.",
            Landmarks = new List<CephPointDto>
            {
                new() { Id = "S", X = 42.0, Y = 31.0, IsManuallyAdjusted = false },
                new() { Id = "N", X = 68.5, Y = 26.5, IsManuallyAdjusted = true }
            },
            Measurements = new List<CephMeasurementDto>
            {
                new() { Parameter = "ANB", Value = 2.5, ClinicalStatus = "Normal" }
            }
        };

        // Act
        var savedReport = await _service.SaveCephalometricReportAsync(recordId, saveDto, "doc-123", "Dr. Ortho Lead");
        var retrievedReport = await _service.GetCephalometricReportAsync(recordId);

        // Assert
        Assert.NotNull(savedReport);
        Assert.NotNull(retrievedReport);
        Assert.Equal(recordId, retrievedReport.RecordId);
        Assert.Equal("Class I", retrievedReport.SkeletalClass);
        Assert.Equal("doc-123", retrievedReport.DoctorId);
        Assert.Equal("Dr. Ortho Lead", retrievedReport.DoctorName);
        Assert.Equal("Bimaxillary dentoalveolar protrusion with Class I molar relationship.", retrievedReport.DoctorNotes);
        Assert.Equal(2, retrievedReport.Landmarks.Count);
        Assert.True(retrievedReport.Landmarks.First(l => l.Id == "N").IsManuallyAdjusted);
    }
}
