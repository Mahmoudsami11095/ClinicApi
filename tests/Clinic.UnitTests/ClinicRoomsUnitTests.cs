using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class ClinicRoomsUnitTests
{
    [Fact]
    public void REQ_CLI_03_ClinicEntity_StoresBranchCodeAndRoomsList()
    {
        // Arrange
        var clinic = new ClinicEntity
        {
            Id = "c-branch-1",
            Name = "Nasr City Dental Branch",
            BranchCode = "NC-02",
            Rooms = "Dental Chair 1,Dental Chair 2,Panoramic Imaging Suite"
        };

        // Assert
        Assert.Equal("NC-02", clinic.BranchCode);
        Assert.Contains("Dental Chair 1", clinic.Rooms);
        Assert.Contains("Panoramic Imaging Suite", clinic.Rooms);
    }

    [Fact]
    public void REQ_CLI_03_Appointment_TracksAssignedExaminationRoom()
    {
        // Arrange
        var appt = new Appointment
        {
            Id = "apt-room-1",
            PatientId = "pat-1",
            DoctorId = "doc-1",
            ClinicId = "c-branch-1",
            RoomNumber = "Dental Chair 2"
        };

        // Assert
        Assert.Equal("Dental Chair 2", appt.RoomNumber);
    }

    [Fact]
    public void REQ_CLI_03_AppointmentDto_PreservesRoomNumber()
    {
        // Arrange
        var dto = new AppointmentDto
        {
            Id = "dto-room-1",
            PatientId = "pat-1",
            DoctorId = "doc-1",
            RoomNumber = "Surgical Suite 3"
        };

        // Assert
        Assert.Equal("Surgical Suite 3", dto.RoomNumber);
    }
}
