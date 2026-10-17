using System;
using System.Collections.Generic;
using System.Security.Claims;
using Clinic.API.Controllers;
using Clinic.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Clinic.IntegrationTests;

public class PeriodontalIntegrationTests
{
    private readonly PeriodontalController _controller;

    public PeriodontalIntegrationTests()
    {
        _controller = new PeriodontalController();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "doc-perio-1"),
            new Claim(ClaimTypes.Name, "Dr. Perio Specialist"),
            new Claim(ClaimTypes.Role, "doctor")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public void BR_PERIO_01_GetLatestExam_ReturnsSeededExamForPatient()
    {
        // Act
        var result = _controller.GetLatestExam("pat-201");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var exam = Assert.IsType<PeriodontalExamDto>(okResult.Value);

        Assert.Equal("pat-201", exam.PatientId);
        Assert.NotEmpty(exam.Teeth);
        Assert.NotNull(exam.Assessment);
        Assert.True(exam.Assessment.TotalSitesExamined > 0);
    }

    [Fact]
    public void BR_PERIO_02_SaveExam_UpdatesAndRecalculatesStaging()
    {
        // Arrange
        var teeth = new List<PeriodontalToothDto>
        {
            new PeriodontalToothDto(
                ToothNumber: 11,
                IsMissing: false,
                IsImplant: false,
                FurcationGrade: 0,
                MobilityGrade: 0,
                Sites: new List<PeriodontalSiteDto>
                {
                    new("Distobuccal", 6, 1, 7, true, false), // CAL 7mm, PPD 6mm
                    new("Midbuccal", 3, 0, 3, false, false),
                    new("Mesiobuccal", 5, 0, 5, true, false),
                    new("Distolingual", 6, 1, 7, true, false),
                    new("Midlingual", 3, 0, 3, false, false),
                    new("Mesiolingual", 5, 0, 5, true, false)
                }
            )
        };

        var request = new SavePeriodontalExamRequest(
            PatientAge: 52,
            ExaminerName: "Dr. Perio Specialist",
            IsSmoker: true,
            CigarettesPerDay: 15,
            HbA1c: 7.8,
            BoneLossPercentage: 55.0,
            Teeth: teeth,
            VoiceNotes: "Voice input recorded: deep pocket on tooth 11."
        );

        // Act
        var result = _controller.SaveExam("pat-test-perio-99", request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var exam = Assert.IsType<PeriodontalExamDto>(okResult.Value);

        Assert.Equal("pat-test-perio-99", exam.PatientId);
        Assert.Equal("Stage III", exam.Assessment.Stage);
        Assert.Equal("Grade C", exam.Assessment.Grade);
        Assert.Equal(7, exam.Assessment.MaxCalMm);
        Assert.Equal(6, exam.Assessment.MaxPpdMm);
    }

    [Fact]
    public void BR_PERIO_03_CalculateStaging_ReturnsCorrectMetricsWithoutSaving()
    {
        // Arrange
        var request = new CalculatePerioStagingRequest(
            PatientAge: 35,
            BoneLossPercentage: 0.0,
            IsSmoker: false,
            CigarettesPerDay: 0,
            HbA1c: null,
            Teeth: new List<PeriodontalToothDto>
            {
                new PeriodontalToothDto(
                    ToothNumber: 21,
                    IsMissing: false,
                    IsImplant: false,
                    FurcationGrade: 0,
                    MobilityGrade: 0,
                    Sites: new List<PeriodontalSiteDto>
                    {
                        new("Distobuccal", 3, 0, 0, false, false),
                        new("Midbuccal", 2, 0, 0, false, false),
                        new("Mesiobuccal", 3, 0, 0, false, false),
                        new("Distolingual", 3, 0, 0, false, false),
                        new("Midlingual", 2, 0, 0, false, false),
                        new("Mesiolingual", 3, 0, 0, false, false)
                    }
                )
            }
        );

        // Act
        var result = _controller.CalculateStaging(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var assessment = Assert.IsType<PerioStagingAssessmentDto>(okResult.Value);

        Assert.Equal("Health", assessment.Stage);
        Assert.Equal("Grade A", assessment.Grade);
        Assert.Equal(0.0, assessment.BopPercentage);
    }
}
