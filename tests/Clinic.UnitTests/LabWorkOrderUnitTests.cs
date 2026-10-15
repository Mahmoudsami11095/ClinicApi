using System;
using System.Security.Claims;
using Clinic.API.Controllers;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Clinic.UnitTests;

public class LabWorkOrderUnitTests
{
    private readonly LabWorkOrderController _controller;

    public LabWorkOrderUnitTests()
    {
        _controller = new LabWorkOrderController();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "doc-prostho-1"),
            new Claim(ClaimTypes.Name, "Dr. Prostho Specialist"),
            new Claim(ClaimTypes.Role, "doctor")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public void REQ_LAB_01_CreateLabWorkOrder_PopulatesFullProstheticSpecification()
    {
        // Arrange
        var request = new LabWorkOrderController.CreateLabWorkOrderRequest(
            PatientId: "pat-999",
            PatientName: "Elena Fisher",
            ToothNumber: 21,
            RestorationType: "Zirconia Crown",
            Material: "Multi-layer 5Y-PSZ Zirconia",
            Shade: "A1",
            StumpShade: "ND1",
            StlScanUrl: "/scans/tooth_21.stl",
            LabPartnerName: "Prestige Dental Lab",
            EstimatedDays: 5
        );

        // Act
        var result = _controller.CreateLabOrder(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var order = Assert.IsType<LabWorkOrder>(okResult.Value);

        Assert.NotNull(order.Id);
        Assert.StartsWith("LAB-", order.OrderToken);
        Assert.Equal(21, order.ToothNumber);
        Assert.Equal("A1", order.Shade);
        Assert.Equal("Dispatched", order.Status);
        Assert.NotNull(order.EstimatedDeliveryDate);
    }

    [Fact]
    public void REQ_LAB_02_ResolveOrderByToken_MasksPatientNameForHIPAAPrivacy()
    {
        // Act
        var result = _controller.ResolveOrderByToken("LAB-7A9B12");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic data = okResult.Value!;

        Assert.Equal("LAB-7A9B12", data.OrderToken);
        Assert.Equal(36, data.ToothNumber);
        Assert.Contains("*", data.PatientName.ToString());
    }

    [Fact]
    public void REQ_LAB_03_UpdateOrderStatus_AdvancesMillingLifecycle()
    {
        // Arrange
        var token = "LAB-7A9B12";
        var updateRequest = new LabWorkOrderController.UpdateLabStatusRequest(
            Status: "QualityControl",
            TrackingNumber: "APX-882194-QC",
            TechnicianNotes: "Sintering and glazing complete. Margins inspected at 20x magnification.",
            EstimatedDeliveryDate: DateTime.UtcNow.AddDays(1)
        );

        // Act
        var result = _controller.UpdateOrderStatus(token, updateRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var order = Assert.IsType<LabWorkOrder>(okResult.Value);

        Assert.Equal("QualityControl", order.Status);
        Assert.Equal("APX-882194-QC", order.LabTrackingNumber);
        Assert.Contains("Sintering and glazing complete", order.LabTechnicianNotes);
    }

    [Fact]
    public void BR_LAB_01_FittingAppointment_BlockedWhenNotReceivedByClinic()
    {
        // Arrange (Arthur Pendelton, Tooth #36 is currently in 'Milling' or 'QualityControl')
        var patientId = "pat-201";
        var toothNumber = 36;

        // Act
        var result = _controller.CheckFittingEligibility(patientId, toothNumber);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic data = okResult.Value!;

        Assert.False((bool)data.canSchedule);
        Assert.Contains("BR-LAB-01 Violation", (string)data.reason);
    }

    [Fact]
    public void BR_LAB_01_FittingAppointment_AllowedWhenReceivedByClinic()
    {
        // Arrange (Marcus Aurelius, Tooth #46 is 'ReceivedByClinic')
        var patientId = "pat-203";
        var toothNumber = 46;

        // Act
        var result = _controller.CheckFittingEligibility(patientId, toothNumber);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic data = okResult.Value!;

        Assert.True((bool)data.canSchedule);
        Assert.Contains("physically received and inspected by clinic", (string)data.reason);
    }
}
