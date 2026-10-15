using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Clinic.API.Controllers;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Clinic.UnitTests;

public class PostOpTriageUnitTests
{
    private readonly Mock<IWhatsAppNotificationService> _whatsAppMock;
    private readonly PostOpTriageController _controller;

    public PostOpTriageUnitTests()
    {
        _whatsAppMock = new Mock<IWhatsAppNotificationService>();
        _controller = new PostOpTriageController(_whatsAppMock.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "doc-test-1"),
            new Claim(ClaimTypes.Name, "Dr. Test Surgeon"),
            new Claim(ClaimTypes.Role, "doctor")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public void REQ_POSTOP_01_EvaluateEscalation_TriggersOnSeverePain()
    {
        // Arrange
        var session = new PostOpTriageSession
        {
            PainScore = 8,
            BleedingSeverity = "None",
            SwellingSeverity = "None"
        };

        // Act
        session.EvaluateEscalation();

        // Assert
        Assert.True(session.IsEscalated);
        Assert.Equal("Escalated", session.TriageStatus);
        Assert.Contains("Severe pain level reported", session.EscalationReason);
    }

    [Fact]
    public void REQ_POSTOP_02_EvaluateEscalation_TriggersOnSevereBleeding()
    {
        // Arrange
        var session = new PostOpTriageSession
        {
            PainScore = 4,
            BleedingSeverity = "Severe",
            SwellingSeverity = "None"
        };

        // Act
        session.EvaluateEscalation();

        // Assert
        Assert.True(session.IsEscalated);
        Assert.Equal("Escalated", session.TriageStatus);
        Assert.Contains("severe active bleeding", session.EscalationReason);
    }

    [Fact]
    public void REQ_POSTOP_03_EvaluateEscalation_TriggersOnSevereSwelling()
    {
        // Arrange
        var session = new PostOpTriageSession
        {
            PainScore = 3,
            BleedingSeverity = "None",
            SwellingSeverity = "Severe"
        };

        // Act
        session.EvaluateEscalation();

        // Assert
        Assert.True(session.IsEscalated);
        Assert.Equal("Escalated", session.TriageStatus);
        Assert.Contains("Significant acute facial/intraoral swelling", session.EscalationReason);
    }

    [Fact]
    public void REQ_POSTOP_04_EvaluateEscalation_MildSymptomsClassifiesAsRecovering()
    {
        // Arrange
        var session = new PostOpTriageSession
        {
            PainScore = 3,
            BleedingSeverity = "Slight",
            SwellingSeverity = "Moderate"
        };

        // Act
        session.EvaluateEscalation();

        // Assert
        Assert.False(session.IsEscalated);
        Assert.Equal("Recovering", session.TriageStatus);
        Assert.Null(session.EscalationReason);
    }

    [Fact]
    public async Task REQ_POSTOP_05_TriggerTriage_ImmediateDispatchSendsWhatsAppNotification()
    {
        // Arrange
        var request = new PostOpTriageController.TriggerTriageRequest(
            PatientId: "pat-999",
            PatientName: "Alice Miller",
            PatientPhone: "+15551234567",
            ProcedureName: "Surgical Crown Lengthening #14",
            IntervalHours: 24,
            ImmediateDispatch: true
        );

        // Act
        var result = await _controller.TriggerTriageSession(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var session = Assert.IsType<PostOpTriageSession>(okResult.Value);

        Assert.Equal("PendingResponse", session.TriageStatus);
        Assert.NotNull(session.DispatchedAtUtc);
        _whatsAppMock.Verify(w => w.SendNotificationAsync(request.PatientPhone, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void REQ_POSTOP_06_ResolveTriage_ClearsEscalationAndLogsDoctorIntervention()
    {
        // Arrange
        var sessionId = "postop-seed-101"; // Known escalated seed
        var request = new PostOpTriageController.ResolveTriageRequest(
            DoctorClinicalAction: "Telephoned patient. Prescribed Amoxicillin + Tramadol. Gauze pressure instructed."
        );

        // Act
        var result = _controller.ResolveTriage(sessionId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var session = Assert.IsType<PostOpTriageSession>(okResult.Value);

        Assert.False(session.IsEscalated);
        Assert.Equal("Resolved", session.TriageStatus);
        Assert.Equal("doc-test-1", session.ResolvedByDoctorId);
        Assert.Equal("Dr. Test Surgeon", session.ResolvedByDoctorName);
        Assert.Equal(request.DoctorClinicalAction, session.DoctorClinicalAction);
        Assert.NotNull(session.ResolvedAtUtc);
    }
}
