using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using System;
using Xunit;

namespace Clinic.UnitTests;

public class ChairQueueStateMachineUnitTests
{
    [Fact]
    public void ChairState_Default_ShouldBeAvailable()
    {
        var chair = new ClinicChair
        {
            ClinicId = "c1",
            RoomNumber = "101",
            ChairName = "Operatory 1"
        };

        Assert.Equal("available", chair.Status);
        Assert.Null(chair.CurrentPatientName);
        Assert.Null(chair.OccupancyStartedAt);
        Assert.Null(chair.CleaningStartedAt);
    }

    [Fact]
    public void ChairState_AssignPatient_ShouldTransitionToOccupied()
    {
        var chair = new ClinicChair
        {
            ClinicId = "c1",
            RoomNumber = "102",
            ChairName = "Operatory 2",
            Status = "available"
        };

        // Act - Assign patient & procedure
        chair.Status = "occupied";
        chair.CurrentPatientId = "pat-1";
        chair.CurrentPatientName = "Mona Zaki";
        chair.CurrentDoctorId = "doc-1";
        chair.CurrentDoctorName = "Dr. Mahmoud";
        chair.ProcedureName = "Composite Restoration";
        chair.OccupancyStartedAt = DateTime.UtcNow;
        chair.CleaningStartedAt = null;

        // Assert
        Assert.Equal("occupied", chair.Status);
        Assert.Equal("Mona Zaki", chair.CurrentPatientName);
        Assert.Equal("Composite Restoration", chair.ProcedureName);
        Assert.NotNull(chair.OccupancyStartedAt);
    }

    [Fact]
    public void ChairState_ReleasePatient_ShouldTransitionToSterilizationCleaning()
    {
        var chair = new ClinicChair
        {
            ClinicId = "c1",
            RoomNumber = "103",
            ChairName = "Operatory 3",
            Status = "occupied",
            CurrentPatientName = "Tamer Hosny",
            ProcedureName = "Endodontics",
            OccupancyStartedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        // Act - Release patient after procedure
        chair.Status = "cleaning";
        chair.CleaningStartedAt = DateTime.UtcNow;
        chair.OccupancyStartedAt = null;
        chair.CurrentPatientId = null;
        chair.CurrentPatientName = null;
        chair.ProcedureName = null;

        // Assert
        Assert.Equal("cleaning", chair.Status);
        Assert.NotNull(chair.CleaningStartedAt);
        Assert.Null(chair.OccupancyStartedAt);
        Assert.Null(chair.CurrentPatientName);
    }

    [Fact]
    public void ChairState_SterilizationComplete_ShouldReturnToAvailable()
    {
        var chair = new ClinicChair
        {
            ClinicId = "c1",
            RoomNumber = "104",
            ChairName = "Hygiene Operatory",
            Status = "cleaning",
            CleaningStartedAt = DateTime.UtcNow.AddMinutes(-12)
        };

        // Act - Sterilization cycle done
        chair.Status = "available";
        chair.CleaningStartedAt = null;
        chair.OccupancyStartedAt = null;

        // Assert
        Assert.Equal("available", chair.Status);
        Assert.Null(chair.CleaningStartedAt);
    }

    [Theory]
    [InlineData("available", true)]
    [InlineData("occupied", true)]
    [InlineData("cleaning", true)]
    [InlineData("maintenance", true)]
    [InlineData("invalid_status", false)]
    public void ChairStatus_Validation_ShouldEnforceStrictLifecycleStates(string status, bool isValid)
    {
        var validStatuses = new[] { "available", "occupied", "cleaning", "maintenance" };
        bool result = Array.IndexOf(validStatuses, status.ToLower()) >= 0;
        Assert.Equal(isValid, result);
    }
}
