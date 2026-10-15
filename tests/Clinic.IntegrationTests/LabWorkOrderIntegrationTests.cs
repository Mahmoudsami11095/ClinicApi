using System;
using System.Security.Claims;
using Clinic.API.Controllers;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Clinic.IntegrationTests;

public class LabWorkOrderIntegrationTests
{
    private readonly LabWorkOrderController _controller;

    public LabWorkOrderIntegrationTests()
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
        var data = Assert.IsType<LabWorkOrderController.LabOrderPublicViewResponse>(okResult.Value);

        Assert.Equal("LAB-7A9B12", data.OrderToken);
        Assert.Equal(36, data.ToothNumber);
        Assert.Contains("*", data.PatientName);
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
        var data = Assert.IsType<LabWorkOrderController.FittingEligibilityResponse>(okResult.Value);

        Assert.False(data.CanSchedule);
        Assert.Contains("BR-LAB-01 Violation", data.Reason);
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
        var data = Assert.IsType<LabWorkOrderController.FittingEligibilityResponse>(okResult.Value);

        Assert.True(data.CanSchedule);
        Assert.Contains("physically received and inspected by clinic", data.Reason);
    }

    [Fact]
    public void REQ_3D_01_Get3DModelByOrderId_ReturnsModelTelemetryAndStreamingToken()
    {
        // Act
        var result = _controller.Get3DModelByOrderId("lab-seed-01");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LabWorkOrderController.LabOrder3DModelResponse>(okResult.Value);

        Assert.Equal("lab-seed-01", response.OrderId);
        Assert.Equal("LAB-7A9B12", response.OrderToken);
        Assert.Equal(36, response.ToothNumber);
        Assert.Equal("STL", response.ModelFileFormat);
        Assert.True(response.MeshPolyCount > 0);
        Assert.NotNull(response.ModelBoundingBoxMm);
        Assert.NotNull(response.AccessToken);
        Assert.StartsWith("sas-LAB-7A9B12", response.AccessToken);
    }

    [Fact]
    public void BR_3D_02_Get3DModelByToken_PermitsAnonymousLabPartnerAccessWithMaskedContext()
    {
        // Act (external lab partner calling using order token)
        var result = _controller.Get3DModelByToken("LAB-4E2C88");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LabWorkOrderController.LabOrder3DModelResponse>(okResult.Value);

        Assert.Equal("LAB-4E2C88", response.OrderToken);
        Assert.Equal(11, response.ToothNumber);
        Assert.Equal("STL", response.ModelFileFormat);
        Assert.NotNull(response.AccessToken);
        Assert.StartsWith("sas-lab-LAB-4E2C88", response.AccessToken);
    }

    [Fact]
    public void REQ_3D_03_Upload3DModel_ValidAsciiStlMesh_UpdatesOrderAndTelemetry()
    {
        // Arrange: ASCII STL starting with "solid ..."
        var asciiStl = "solid ToothMesh\nfacet normal 0 0 0\nouter loop\nvertex 0 0 0\nvertex 1 0 0\nvertex 0 1 0\nendloop\nendfacet\nendsolid ToothMesh";
        var base64Stl = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(asciiStl));
        var uploadRequest = new LabWorkOrderController.Upload3DModelRequest(
            FileName: "mandible_crown_36.stl",
            Base64Data: base64Stl,
            FileFormat: "STL"
        );

        // Act
        var result = _controller.Upload3DModel("lab-seed-01", uploadRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LabWorkOrderController.Upload3DModelResponse>(okResult.Value);

        Assert.True(response.Success);
        Assert.Equal("STL", response.ModelFileFormat);
        Assert.Contains("mandible_crown_36.stl", response.StlScanUrl);
    }

    [Fact]
    public void REQ_3D_03_Upload3DModel_ValidBinaryStlMesh_ExtractsPolyCount()
    {
        // Arrange: Binary STL with 80-byte header + uint32 triangle count (5000 triangles)
        var header = new byte[84];
        BitConverter.GetBytes((uint)5000).CopyTo(header, 80);
        var base64BinaryStl = Convert.ToBase64String(header);
        var uploadRequest = new LabWorkOrderController.Upload3DModelRequest(
            FileName: "binary_scan_tooth11.stl",
            Base64Data: base64BinaryStl,
            FileFormat: "STL"
        );

        // Act
        var result = _controller.Upload3DModel("lab-seed-02", uploadRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LabWorkOrderController.Upload3DModelResponse>(okResult.Value);

        Assert.True(response.Success);
        Assert.Equal(5000, response.MeshPolyCount);
        Assert.Equal("STL", response.ModelFileFormat);
    }

    [Fact]
    public void BR_3D_01_Upload3DModel_RejectsMaliciousWindowsExecutable()
    {
        // Arrange: Windows PE MZ header (0x4D, 0x5A) masquerading as .stl
        var fakeStl = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00 };
        var base64Fake = Convert.ToBase64String(fakeStl);
        var uploadRequest = new LabWorkOrderController.Upload3DModelRequest(
            FileName: "malware.stl",
            Base64Data: base64Fake,
            FileFormat: "STL"
        );

        // Act
        var result = _controller.Upload3DModel("lab-seed-01", uploadRequest);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public void BR_3D_01_Upload3DModel_RejectsEmptyOrCorruptedPayload()
    {
        // Arrange: empty payload
        var uploadRequest = new LabWorkOrderController.Upload3DModelRequest(
            FileName: "empty.stl",
            Base64Data: "",
            FileFormat: "STL"
        );

        // Act
        var result = _controller.Upload3DModel("lab-seed-01", uploadRequest);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }
}
