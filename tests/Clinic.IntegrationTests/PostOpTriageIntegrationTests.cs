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

namespace Clinic.IntegrationTests;

public class PostOpTriageIntegrationTests
{
    private readonly Mock<IWhatsAppNotificationService> _whatsAppMock;
    private readonly PostOpTriageController _controller;

    public PostOpTriageIntegrationTests()
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
        _whatsAppMock.Verify(w => w.SendNotificationAsync(request.PatientPhone!, It.IsAny<string>()), Times.Once);
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
