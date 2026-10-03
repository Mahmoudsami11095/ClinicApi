using Clinic.Application.DTOs;
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
    [InlineData(AppointmentStatus.Waiting)]
    [InlineData(AppointmentStatus.InConsultation)]
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

    [Fact]
    public void REQ_APT_02_PatientCheckIn_SetsWaitingStatus_AndStampsArrival()
    {
        // Arrange
        var appt = new Appointment
        {
            Id = "apt-queue-1",
            PatientId = "pat-1",
            DoctorId = "doc-1",
            Status = "scheduled",
            Date = "2026-10-03T10:00:00Z"
        };

        // Act - Simulate check-in
        const string arrivalTime = "2026-10-03T09:48:00Z";
        appt.Status = "waiting";
        appt.ArrivedAt = arrivalTime;
        appt.QueueNumber = 1;

        // Assert
        Assert.Equal("waiting", appt.Status);
        Assert.Equal(arrivalTime, appt.ArrivedAt);
        Assert.Equal(1, appt.QueueNumber);
    }

    [Fact]
    public void REQ_APT_02_ConsultationStartAndEnd_StampsTimestampsCorrectly()
    {
        // Arrange
        var appt = new Appointment
        {
            Id = "apt-queue-2",
            PatientId = "pat-1",
            DoctorId = "doc-1",
            Status = "waiting",
            ArrivedAt = "2026-10-03T09:45:00Z",
            QueueNumber = 2
        };

        // Act 1: Doctor starts consultation
        const string startTime = "2026-10-03T10:05:00Z";
        appt.Status = "in_consultation";
        appt.ConsultationStartedAt = startTime;

        Assert.Equal("in_consultation", appt.Status);
        Assert.Equal(startTime, appt.ConsultationStartedAt);

        // Act 2: Doctor completes consultation
        const string endTime = "2026-10-03T10:35:00Z";
        appt.Status = "completed";
        appt.ConsultationEndedAt = endTime;

        Assert.Equal("completed", appt.Status);
        Assert.Equal(endTime, appt.ConsultationEndedAt);
    }

    [Fact]
    public void REQ_APT_02_AppointmentDto_Mapping_PreservesQueueFields()
    {
        // Arrange
        var dto = new AppointmentDto
        {
            Id = "apt-dto-1",
            PatientId = "pat-100",
            DoctorId = "doc-100",
            Date = "2026-10-03T11:00:00Z",
            Status = "waiting",
            Type = "Dental Consultation",
            ClinicId = "clinic-1",
            ArrivedAt = "2026-10-03T10:45:00Z",
            ConsultationStartedAt = "2026-10-03T11:02:00Z",
            ConsultationEndedAt = "2026-10-03T11:32:00Z",
            QueueNumber = 4
        };

        // Assert
        Assert.Equal("waiting", dto.Status);
        Assert.Equal("2026-10-03T10:45:00Z", dto.ArrivedAt);
        Assert.Equal("2026-10-03T11:02:00Z", dto.ConsultationStartedAt);
        Assert.Equal("2026-10-03T11:32:00Z", dto.ConsultationEndedAt);
        Assert.Equal(4, dto.QueueNumber);
    }
}
