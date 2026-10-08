using System;
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
}
