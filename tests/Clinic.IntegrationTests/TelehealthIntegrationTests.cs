using System;
using System.Collections.Generic;
using System.Security.Claims;
using Clinic.API.Controllers;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Clinic.IntegrationTests;

public class TelehealthIntegrationTests
{
    private readonly TelehealthController _controller;

    public TelehealthIntegrationTests()
    {
        _controller = new TelehealthController();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "doc-tele-101"),
            new(ClaimTypes.Name, "Dr. Tarek Shawky"),
            new(ClaimTypes.Role, "Doctor")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public void REQ_TELE_01_CreateSession_WithInvitedSpecialist_ReturnsSessionAndLogsAuditEntry()
    {
        // Arrange
        var request = new CreateTelehealthSessionRequest(
            ClinicId: "clinic-heliopolis",
            AppointmentId: "apt-tele-001",
            PatientId: "pat-554",
            PatientName: "Nouran Selim",
            InvitedSpecialistId: "spec-ortho-02",
            InvitedSpecialistName: "Dr. Hesham Ezzat",
            InvitedSpecialty: "Orthodontics"
        );

        // Act
        var result = _controller.CreateSession(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var session = Assert.IsType<TelehealthSession>(okResult.Value);

        Assert.NotNull(session.RoomToken);
        Assert.Equal("WaitingForPatient", session.Status);
        Assert.Equal("Dr. Hesham Ezzat", session.InvitedSpecialistName);
        Assert.Equal("Orthodontics", session.InvitedSpecialty);
        Assert.Single(session.AuditTrail);
        Assert.Equal("RoomCreated", session.AuditTrail[0].EventType);
    }

    [Fact]
    public void REQ_TELE_02_StartAndEndSession_CalculatesDuration_AndComputesSha256AuditSeal()
    {
        // Arrange: Create session first
        var createReq = new CreateTelehealthSessionRequest(
            ClinicId: "clinic-maadi",
            AppointmentId: "apt-tele-002",
            PatientId: "pat-660",
            PatientName: "Youssef Gamal"
        );
        var created = Assert.IsType<TelehealthSession>(Assert.IsType<OkObjectResult>(_controller.CreateSession(createReq)).Value);

        // Act 1: Start consultation
        var startResult = _controller.StartSession(created.RoomToken);
        var activeSession = Assert.IsType<TelehealthSession>(Assert.IsType<OkObjectResult>(startResult).Value);
        Assert.Equal("Active", activeSession.Status);
        Assert.NotNull(activeSession.StartedAtUtc);

        // Act 2: End consultation with summary
        var endReq = new EndTelehealthSessionRequest(
            ClinicalSummary: "Discussed Invisalign staging refinement. Tray 14 tracking well.",
            DurationSeconds: 1250
        );
        var endResult = _controller.EndSession(created.RoomToken, endReq);
        var completedSession = Assert.IsType<TelehealthSession>(Assert.IsType<OkObjectResult>(endResult).Value);

        // Assert
        Assert.Equal("Completed", completedSession.Status);
        Assert.Equal(1250, completedSession.DurationSeconds);
        Assert.NotNull(completedSession.Sha256AuditSeal);
        Assert.StartsWith("urn:telehealth:sha256:", completedSession.Sha256AuditSeal);
        Assert.True(completedSession.IsSha256Verified);
    }

    [Fact]
    public void REQ_TELE_03_SyncViewport_UpdatesActiveViewType_AndSavesSyncJson()
    {
        // Arrange
        var createReq = new CreateTelehealthSessionRequest(
            ClinicId: "clinic-zamalek",
            AppointmentId: "apt-tele-003",
            PatientId: "pat-771",
            PatientName: "Kareem Farouk"
        );
        var session = Assert.IsType<TelehealthSession>(Assert.IsType<OkObjectResult>(_controller.CreateSession(createReq)).Value);

        var syncPayload = new ViewportSyncDto(
            RoomToken: session.RoomToken,
            ViewType: "Mesh3D",
            CameraX: 15.2,
            CameraY: -8.4,
            CameraZ: 32.1,
            RotationPitch: 0.15,
            RotationYaw: -0.82,
            ZoomFactor: 1.45,
            ActiveAssetId: "STL-TOOTH-36-CROWN"
        );

        // Act
        var syncResult = _controller.SyncViewport(session.RoomToken, syncPayload);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(syncResult);
        Assert.NotNull(okResult.Value);

        var fetched = Assert.IsType<TelehealthSession>(Assert.IsType<OkObjectResult>(_controller.GetSessionByToken(session.RoomToken)).Value);
        Assert.Equal("Mesh3D", fetched.ActiveViewType);
        Assert.Contains("STL-TOOTH-36-CROWN", fetched.LastViewportSyncJson);
    }

    [Fact]
    public void REQ_TELE_04_IssueTelePrescription_GeneratesSignedRx_WithSha256Digest()
    {
        // Arrange
        var createReq = new CreateTelehealthSessionRequest(
            ClinicId: "clinic-nasr",
            AppointmentId: "apt-tele-004",
            PatientId: "pat-882",
            PatientName: "Mariam Adel"
        );
        var session = Assert.IsType<TelehealthSession>(Assert.IsType<OkObjectResult>(_controller.CreateSession(createReq)).Value);

        var rxReq = new IssueTelePrescriptionRequest(
            PatientId: session.PatientId,
            PatientName: session.PatientName,
            Medications: new List<TelehealthMedicationItemDto>
            {
                new("Augmentin 1g", "1 tablet", "BID", "7 days", "Take with meals"),
                new("Cataflam 50mg", "1 tablet", "TDS", "3 days", "Take after food for pain")
            },
            Notes: "Avoid dairy within 2 hours of antibiotic intake.",
            DispatchChannel: "WhatsApp"
        );

        // Act
        var rxResult = _controller.IssueTelePrescription(session.RoomToken, rxReq);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(rxResult);
        var rx = Assert.IsType<TelehealthPrescriptionRecord>(okResult.Value);

        Assert.StartsWith("RX-TELE-", rx.RxNumber);
        Assert.Equal("Dr. Tarek Shawky", rx.DoctorName);
        Assert.Equal(2, rx.Medications.Count);
        Assert.StartsWith("SIG-DR-", rx.DigitalSignature);
        Assert.StartsWith("urn:rx:sha256:", rx.Sha256Digest);
        Assert.Contains(rx.RxNumber, rx.VerificationUrl);

        var prescriptionsResult = _controller.GetTelePrescriptions(session.RoomToken);
        var prescriptions = Assert.IsType<List<TelehealthPrescriptionRecord>>(Assert.IsType<OkObjectResult>(prescriptionsResult).Value);
        Assert.Single(prescriptions);
    }

    [Fact]
    public void REQ_TELE_05_GetAuditDossier_ReturnsCompleteChronologicalAuditEvents()
    {
        // Arrange
        var createReq = new CreateTelehealthSessionRequest(
            ClinicId: "clinic-tagamoa",
            AppointmentId: "apt-tele-005",
            PatientId: "pat-993",
            PatientName: "Salma Fayed"
        );
        var session = Assert.IsType<TelehealthSession>(Assert.IsType<OkObjectResult>(_controller.CreateSession(createReq)).Value);
        _controller.StartSession(session.RoomToken);
        _controller.EndSession(session.RoomToken, new EndTelehealthSessionRequest("All clear", 600));

        // Act
        var dossierResult = _controller.GetAuditDossier(session.RoomToken);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(dossierResult);
        var dossier = Assert.IsType<TelehealthAuditDossierDto>(okResult.Value);

        Assert.Equal(session.RoomToken, dossier.RoomToken);
        Assert.Equal("Completed", dossier.Status);
        Assert.True(dossier.TotalEventsLogged >= 3);
        Assert.True(dossier.IsSha256Verified);
        Assert.NotNull(dossier.Sha256AuditSeal);
    }

    [Fact]
    public void REQ_TELE_06_VerifyAuditSeal_ValidatesCryptographicSessionIntegrity()
    {
        // Arrange
        var createReq = new CreateTelehealthSessionRequest(
            ClinicId: "clinic-alex",
            AppointmentId: "apt-tele-006",
            PatientId: "pat-104",
            PatientName: "Amr Diab"
        );
        var session = Assert.IsType<TelehealthSession>(Assert.IsType<OkObjectResult>(_controller.CreateSession(createReq)).Value);
        _controller.StartSession(session.RoomToken);
        _controller.EndSession(session.RoomToken, new EndTelehealthSessionRequest("Surgical site inspection clear.", 900));

        // Act
        var verifyResult = _controller.VerifyAuditSeal(session.RoomToken);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(verifyResult);
        var val = Assert.IsType<VerifyAuditSealResponse>(okResult.Value);
        Assert.True(val.IsValid);
        Assert.NotNull(val.CurrentSeal);
        Assert.Equal(val.CurrentSeal, val.CalculatedSeal);
    }
}
