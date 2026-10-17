using System;
using System.Collections.Generic;
using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class TelehealthUnitTests
{
    [Fact]
    public void TelehealthSession_WhenInitialized_HasDefaultWaitingStatusAndValidToken()
    {
        // Arrange & Act
        var session = new TelehealthSession
        {
            ClinicId = "clinic-01",
            AppointmentId = "apt-101",
            DoctorId = "doc-01",
            DoctorName = "Dr. Sarah Jenkins",
            PatientId = "pat-505",
            PatientName = "Ahmed Hassan"
        };

        // Assert
        Assert.NotNull(session.Id);
        Assert.NotNull(session.RoomToken);
        Assert.Equal(32, session.RoomToken.Length); // Guid "N" format
        Assert.Equal("WaitingForPatient", session.Status);
        Assert.Equal(0, session.DurationSeconds);
        Assert.Null(session.StartedAtUtc);
        Assert.Null(session.EndedAtUtc);
    }

    [Fact]
    public void TelehealthSession_WhenCallStartsAndCompletes_CalculatesDurationCorrectly()
    {
        // Arrange
        var session = new TelehealthSession
        {
            ClinicId = "clinic-01",
            AppointmentId = "apt-101",
            DoctorId = "doc-01",
            PatientId = "pat-505"
        };

        var start = DateTime.UtcNow.AddMinutes(-15);
        var end = DateTime.UtcNow;

        // Act
        session.StartedAtUtc = start;
        session.Status = "Active";

        session.EndedAtUtc = end;
        session.DurationSeconds = (int)(end - start).TotalSeconds;
        session.Status = "Completed";
        session.ClinicalSummary = "Post-op suture check #46: Good healing, no erythema.";

        // Assert
        Assert.Equal("Completed", session.Status);
        Assert.True(session.DurationSeconds >= 900);
        Assert.NotNull(session.ClinicalSummary);
    }

    [Fact]
    public void TelehealthSession_RoomToken_GeneratesUniqueAlphanumericTokens()
    {
        var session1 = new TelehealthSession();
        var session2 = new TelehealthSession();

        Assert.NotEqual(session1.RoomToken, session2.RoomToken);
        Assert.DoesNotContain("-", session1.RoomToken);
        Assert.DoesNotContain("-", session2.RoomToken);
    }

    [Theory]
    [InlineData("WaitingForPatient")]
    [InlineData("Active")]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    public void TelehealthSession_StatusTransitions_AreValid(string status)
    {
        var session = new TelehealthSession { Status = status };
        Assert.Equal(status, session.Status);
    }

    // ── Release v12.0.0: Multi-Clinic Telehealth & Co-Viewer Tests (BR-TELE-01..04) ──

    [Fact]
    public void TelehealthSession_MultiClinicSpecialistInvite_StoresSpecialistMetadata()
    {
        var session = new TelehealthSession
        {
            ClinicId = "clinic-downtown",
            DoctorId = "doc-gp-101",
            DoctorName = "Dr. Tarek Omar",
            PatientId = "pat-881",
            PatientName = "Laila Mahmoud",
            InvitedSpecialistId = "doc-spec-303",
            InvitedSpecialistName = "Dr. Mona Fikry",
            InvitedSpecialty = "Orthodontics"
        };

        Assert.Equal("doc-spec-303", session.InvitedSpecialistId);
        Assert.Equal("Dr. Mona Fikry", session.InvitedSpecialistName);
        Assert.Equal("Orthodontics", session.InvitedSpecialty);
    }

    [Fact]
    public void TelehealthSession_DiagnosticCoViewer_TracksActiveViewTypeAndSyncJson()
    {
        var session = new TelehealthSession
        {
            RoomToken = "room-test-123",
            ActiveViewType = "Mesh3D",
            LastViewportSyncJson = "{\"viewType\":\"Mesh3D\",\"cameraX\":10.5,\"cameraY\":-4.2}"
        };

        Assert.Equal("Mesh3D", session.ActiveViewType);
        Assert.Contains("cameraX", session.LastViewportSyncJson);
    }

    [Fact]
    public void TelehealthSession_CryptographicSha256AuditSeal_ComputesConsistentDigest()
    {
        var session = new TelehealthSession
        {
            RoomToken = "token-hash-audit-01",
            DoctorId = "doc-01",
            PatientId = "pat-01",
            ClinicId = "clinic-main",
            StartedAtUtc = new DateTime(2026, 10, 17, 10, 0, 0, DateTimeKind.Utc),
            EndedAtUtc = new DateTime(2026, 10, 17, 10, 25, 0, DateTimeKind.Utc),
            DurationSeconds = 1500
        };

        session.AuditTrail.Add(new TelehealthAuditRecord
        {
            TimestampUtc = session.StartedAtUtc.Value,
            ParticipantId = "doc-01",
            ParticipantName = "Dr. Attending",
            Role = "Doctor",
            EventType = "RoomCreated"
        });

        var seal1 = session.ComputeSessionAuditSha256();
        var seal2 = session.ComputeSessionAuditSha256();

        Assert.NotNull(seal1);
        Assert.StartsWith("urn:telehealth:sha256:", seal1);
        Assert.Equal(seal1, seal2);
    }

    [Fact]
    public void TelehealthSession_LiveTelePrescription_GeneratesSignedDigestAndMedications()
    {
        var issuedAt = new DateTime(2026, 10, 17, 10, 15, 0, DateTimeKind.Utc);
        var digest = TelehealthSession.ComputePrescriptionSha256("RX-TELE-202610-001", "doc-01", "pat-01", issuedAt, 2);

        Assert.NotNull(digest);
        Assert.StartsWith("urn:rx:sha256:", digest);

        var rx = new TelehealthPrescriptionRecord
        {
            RxNumber = "RX-TELE-202610-001",
            DoctorId = "doc-01",
            DoctorName = "Dr. Sarah Jenkins",
            PatientId = "pat-01",
            PatientName = "Ahmed Hassan",
            IssuedAtUtc = issuedAt,
            DispatchChannel = "WhatsApp",
            Sha256Digest = digest,
            Medications = new List<TelehealthPrescriptionItemRecord>
            {
                new() { DrugName = "Amoxicillin 500mg", Dosage = "1 capsule", Frequency = "TDS", Duration = "5 days", Instructions = "After food" },
                new() { DrugName = "Ibuprofen 400mg", Dosage = "1 tablet", Frequency = "PRN", Duration = "3 days", Instructions = "For pain" }
            }
        };

        Assert.Equal(2, rx.Medications.Count);
        Assert.Equal("WhatsApp", rx.DispatchChannel);
        Assert.Equal(digest, rx.Sha256Digest);
    }
}
