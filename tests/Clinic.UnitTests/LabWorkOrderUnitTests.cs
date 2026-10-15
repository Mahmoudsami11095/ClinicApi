using System;
using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class LabWorkOrderUnitTests
{
    [Fact]
    public void BR_LAB_01_CanScheduleFittingAppointment_FalseWhenStatusNotReceivedByClinic()
    {
        // Arrange
        var order = new LabWorkOrder
        {
            ToothNumber = 36,
            Status = "Milling"
        };

        // Act
        var canSchedule = order.CanScheduleFittingAppointment(out var reason);

        // Assert
        Assert.False(canSchedule);
        Assert.Contains("BR-LAB-01 Violation", reason);
        Assert.Contains("Milling", reason);
    }

    [Fact]
    public void BR_LAB_01_CanScheduleFittingAppointment_TrueWhenStatusReceivedByClinic()
    {
        // Arrange
        var order = new LabWorkOrder
        {
            ToothNumber = 46,
            Status = "ReceivedByClinic"
        };

        // Act
        var canSchedule = order.CanScheduleFittingAppointment(out var reason);

        // Assert
        Assert.True(canSchedule);
        Assert.Contains("physically received and inspected by clinic", reason);
    }

    [Fact]
    public void REQ_LAB_01_LabWorkOrder_InitializesWithUniqueTokenAndDispatchedStatus()
    {
        // Arrange & Act
        var order = new LabWorkOrder
        {
            ToothNumber = 21,
            RestorationType = "Zirconia Crown",
            Material = "Multi-layer 5Y-PSZ Zirconia",
            Shade = "A1"
        };

        // Assert
        Assert.NotNull(order.Id);
        Assert.StartsWith("LAB-", order.OrderToken);
        Assert.Equal("Dispatched", order.Status);
        Assert.Equal(21, order.ToothNumber);
    }
}
