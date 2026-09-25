using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Xunit;

namespace Clinic.UnitTests;

public class AppointmentUnitTests
{
    [Fact]
    public void Appointment_Initialization_ShouldPreserveProperties()
    {
        // Arrange & Act
        var appointment = new Appointment
        {
            Id = "apt-101",
            PatientId = "pat-001",
            DoctorId = "doc-001",
            Date = "2026-09-26T10:00:00Z",
            Type = "Dental Consultation",
            Status = nameof(AppointmentStatus.Scheduled),
            Notes = "Check tooth 16 pain"
        };

        // Assert
        Assert.Equal("apt-101", appointment.Id);
        Assert.Equal("pat-001", appointment.PatientId);
        Assert.Equal("doc-001", appointment.DoctorId);
        Assert.Equal("Scheduled", appointment.Status);
        Assert.NotNull(appointment.BillingRecords);
    }

    [Theory]
    [InlineData(AppointmentStatus.Scheduled)]
    [InlineData(AppointmentStatus.Completed)]
    [InlineData(AppointmentStatus.Cancelled)]
    public void Appointment_StatusTransitions_ShouldSupportAllDefinedEnums(AppointmentStatus status)
    {
        // Arrange
        var appointment = new Appointment
        {
            Id = "apt-102",
            Status = status.ToString()
        };

        // Assert
        Assert.Equal(status.ToString(), appointment.Status);
    }
}
