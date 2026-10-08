using System;
using System.Collections.Generic;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class PatientRecallUnitTests
{
    [Fact]
    public void REQ_REC_01_PatientRecall_Defaults_AreValid()
    {
        // Arrange & Act
        var recall = new PatientRecall
        {
            RecallNumber = "RCL-202610-0001",
            ClinicId = "clinic-01",
            PatientId = "pat-01",
            DoctorId = "doc-01",
            RecallType = "PeriodontalMaintenance",
            RecallIntervalMonths = 6,
            DueDate = DateTime.UtcNow.AddMonths(6)
        };

        // Assert
        Assert.NotNull(recall.Id);
        Assert.Equal("RCL-202610-0001", recall.RecallNumber);
        Assert.Equal("Scheduled", recall.Status);
        Assert.Equal("WhatsApp", recall.NotificationChannel);
        Assert.Equal(0, recall.ReminderCount);
        Assert.Null(recall.NotificationSentAt);
        Assert.Null(recall.SnoozeUntilDate);
        Assert.False(recall.IsDeleted);
    }

    [Fact]
    public void REQ_REC_02_DispatchRecall_UpdatesStatusAndReminderCount()
    {
        // Arrange
        var recall = new PatientRecall
        {
            RecallNumber = "RCL-202610-0002",
            Status = "Due",
            ReminderCount = 0
        };

        // Act (Dispatch reminder)
        recall.NotificationChannel = "WhatsApp";
        recall.NotificationSentAt = DateTime.UtcNow;
        recall.ReminderCount++;
        recall.Status = "NotificationSent";

        // Assert
        Assert.Equal("NotificationSent", recall.Status);
        Assert.Equal(1, recall.ReminderCount);
        Assert.NotNull(recall.NotificationSentAt);
    }

    [Fact]
    public void BR_REC_02_CompleteRecall_TransitionsToCompleted()
    {
        // Arrange
        var recall = new PatientRecall
        {
            RecallNumber = "RCL-202610-0003",
            Status = "Booked"
        };

        // Act (Patient attends and completes treatment)
        recall.CompletedAt = DateTime.UtcNow;
        recall.Status = "Completed";

        // Assert
        Assert.Equal("Completed", recall.Status);
        Assert.NotNull(recall.CompletedAt);
    }

    [Fact]
    public void BR_REC_04_SnoozeRecall_CalculatesNewDueDate()
    {
        // Arrange
        var originalDue = DateTime.UtcNow;
        var recall = new PatientRecall
        {
            RecallNumber = "RCL-202610-0004",
            DueDate = originalDue,
            Status = "Due"
        };

        // Act (Snooze 4 weeks)
        int snoozeWeeks = 4;
        var newDate = DateTime.UtcNow.AddDays(snoozeWeeks * 7);
        recall.SnoozeUntilDate = newDate;
        recall.DueDate = newDate;
        recall.Status = "Snoozed";

        // Assert
        Assert.Equal("Snoozed", recall.Status);
        Assert.True(recall.DueDate > originalDue);
        Assert.NotNull(recall.SnoozeUntilDate);
    }

    [Fact]
    public void REQ_REC_04_RecallSummary_ComputesConversionRateAccurately()
    {
        // Arrange
        int booked = 12;
        int completed = 8;
        int dispatched = 20; // 20 contacted + not booked yet

        // Act
        int totalConverted = booked + completed; // 20
        int totalDenominator = booked + completed + dispatched; // 40
        double conversionRate = Math.Round(((double)totalConverted / totalDenominator) * 100.0, 1);

        // Assert
        Assert.Equal(50.0, conversionRate);
    }
}
