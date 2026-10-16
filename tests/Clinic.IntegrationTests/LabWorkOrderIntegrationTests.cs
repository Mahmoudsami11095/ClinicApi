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

    [Fact]
    public void REQ_CAL_01_GetMeasurements_ReturnsSeededMeasurements()
    {
        // Act
        var result = _controller.GetMeasurements("lab-seed-01");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var measurements = Assert.IsType<List<LabWorkOrderController.ModelMeasurementDto>>(okResult.Value);

        Assert.NotEmpty(measurements);
        Assert.Contains(measurements, m => m.Label == "Occlusal Reduction" && m.Unit == "mm");
    }

    [Fact]
    public void REQ_CAL_02_SaveMeasurement_ValidDistance_PersistsAndReturnsDto()
    {
        // Arrange
        var req = new LabWorkOrderController.SaveModelMeasurementRequest(
            Label: "Margin Thickness",
            Value: 1.45,
            Unit: "mm",
            Mode: "distance"
        );

        // Act
        var result = _controller.SaveMeasurement("lab-seed-02", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.ModelMeasurementDto>(okResult.Value);

        Assert.Equal("lab-seed-02", dto.OrderId);

        Assert.Equal("Margin Thickness", dto.Label);
        Assert.Equal(1.45, dto.Value);
        Assert.Equal("mm", dto.Unit);
        Assert.Equal("distance", dto.Mode);
    }

    [Fact]
    public void REQ_HEAT_01_GetClearanceHeatmap_ReturnsSeededHeatmap()
    {
        // Act
        var result = _controller.GetClearanceHeatmap("lab-seed-01");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.ClearanceHeatmapDto>(okResult.Value);

        Assert.Equal("lab-seed-01", dto.OrderId);
        Assert.Equal(0.0, dto.UndercutSurfacePercent);
        Assert.True(dto.IsCompliant);
        Assert.True(dto.MinClearanceMm >= 1.0);
    }

    [Fact]
    public void REQ_HEAT_02_SaveClearanceHeatmap_WithUndercuts_ComputesCompliance()
    {
        // Arrange
        var req = new LabWorkOrderController.SaveClearanceHeatmapRequest(
            UndercutSurfacePercent: 4.8,
            MinClearanceMm: 0.85,
            MaxClearanceMm: 2.10,
            InsertionTiltMesialDistalDeg: 5.0,
            InsertionTiltBuccalLingualDeg: 0.0,
            IsCompliant: false,
            StatusMessage: "Undercut detected (4.8%). Path of insertion adjustment required."
        );

        // Act
        var result = _controller.SaveClearanceHeatmap("lab-seed-02", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.ClearanceHeatmapDto>(okResult.Value);

        Assert.Equal("lab-seed-02", dto.OrderId);
        Assert.Equal(4.8, dto.UndercutSurfacePercent);
        Assert.False(dto.IsCompliant);
        Assert.Equal(5.0, dto.InsertionTiltMesialDistalDeg);
        Assert.Contains("Undercut detected", dto.StatusMessage);
    }

    [Fact]
    public void REQ_HEAT_03_SaveClearanceHeatmap_OptimalClearance_ReturnsCompliant()
    {
        // Arrange
        var req = new LabWorkOrderController.SaveClearanceHeatmapRequest(
            UndercutSurfacePercent: 0.0,
            MinClearanceMm: 1.65,
            MaxClearanceMm: 2.25,
            InsertionTiltMesialDistalDeg: 0.0,
            InsertionTiltBuccalLingualDeg: 0.0,
            IsCompliant: null,
            StatusMessage: null
        );

        // Act
        var result = _controller.SaveClearanceHeatmap("lab-seed-03", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.ClearanceHeatmapDto>(okResult.Value);

        Assert.Equal("lab-seed-03", dto.OrderId);
        Assert.Equal(0.0, dto.UndercutSurfacePercent);
        Assert.True(dto.IsCompliant);
        Assert.Contains("within clinical tolerance", dto.StatusMessage);
    }

    [Fact]
    public void REQ_ART_01_GetArticulatorSettings_ReturnsSeedSettings()
    {
        // Act
        var result = _controller.GetArticulatorSettings("lab-seed-01");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.ArticulatorSettingsDto>(okResult.Value);

        Assert.Equal("lab-seed-01", dto.OrderId);
        Assert.Equal(35.0, dto.CondylarInclinationDeg);
        Assert.Equal(15.0, dto.BennettAngleDeg);
        Assert.Equal("Centric", dto.ExcursionType);
        Assert.False(dto.HasInterferenceWarning);
    }

    [Fact]
    public void REQ_ART_02_SaveArticulatorSettings_WithPrematureInterference_FlagsWarning()
    {
        // Arrange
        var req = new LabWorkOrderController.SaveArticulatorSettingsRequest(
            CondylarInclinationDeg: 40.0,
            BennettAngleDeg: 20.0,
            IncisalPinOpeningMm: 0.5,
            ExcursionType: "RightLateral",
            MaxInterferenceMm: 0.35,
            ActiveContactCount: 4,
            HasInterferenceWarning: null,
            StatusMessage: null
        );

        // Act
        var result = _controller.SaveArticulatorSettings("lab-seed-02", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.ArticulatorSettingsDto>(okResult.Value);

        Assert.Equal("lab-seed-02", dto.OrderId);
        Assert.Equal("RightLateral", dto.ExcursionType);
        Assert.Equal(0.35, dto.MaxInterferenceMm);
        Assert.True(dto.HasInterferenceWarning);
        Assert.Contains("High-spot premature interference detected", dto.StatusMessage);
    }

    [Fact]
    public void REQ_ART_03_SaveArticulatorSettings_OptimalKinematics_ReturnsValidated()
    {
        // Arrange
        var req = new LabWorkOrderController.SaveArticulatorSettingsRequest(
            CondylarInclinationDeg: 30.0,
            BennettAngleDeg: 10.0,
            IncisalPinOpeningMm: 0.0,
            ExcursionType: "Protrusive",
            MaxInterferenceMm: 0.02,
            ActiveContactCount: 8,
            HasInterferenceWarning: false,
            StatusMessage: "Protrusive guidance clear."
        );

        // Act
        var result = _controller.SaveArticulatorSettings("lab-seed-03", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.ArticulatorSettingsDto>(okResult.Value);

        Assert.Equal("lab-seed-03", dto.OrderId);
        Assert.Equal("Protrusive", dto.ExcursionType);
        Assert.False(dto.HasInterferenceWarning);
        Assert.Equal("Protrusive guidance clear.", dto.StatusMessage);
    }

    // --- Release v9.4.0 AI Gingival Finish Line & Margin Auto-Tracing Integration Tests ---

    [Fact]
    public void REQ_MAR_01_GetMarginLine_WhenOrderExists_ReturnsMarginWithNodes()
    {
        // Act
        var result = _controller.GetMarginLine("lab-seed-01");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.MarginLineDto>(okResult.Value);

        Assert.Equal("lab-seed-01", dto.OrderId);
        Assert.Equal("Chamfer", dto.ProfileType);
        Assert.True(dto.IsClosed);
        Assert.NotEmpty(dto.Points);
        Assert.True(dto.Points.Count >= 16);
    }

    [Fact]
    public void BR_MAR_02_SaveMarginLine_WhenChamferWidthUnderHalfMm_FlagsWarning()
    {
        // Arrange: 0.35 mm width is below 0.50 mm minimum for Zirconia chamfer
        var points = new List<LabWorkOrderController.MarginPointDto>();
        for (int i = 0; i < 20; i++)
        {
            points.Add(new LabWorkOrderController.MarginPointDto(i * 0.5, 0.1, i * 0.3, i));
        }

        var req = new LabWorkOrderController.SaveMarginLineRequest(
            ProfileType: "Chamfer",
            PerimeterMm: 31.5,
            AverageWidthMm: 0.35,
            SmoothingTension: 0.5,
            IsClosed: true,
            IsApproved: false,
            HasMarginWarning: null,
            StatusMessage: null,
            Points: points
        );

        // Act
        var result = _controller.SaveMarginLine("lab-seed-02", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.MarginLineDto>(okResult.Value);

        Assert.True(dto.HasMarginWarning);
        Assert.Contains("below clinical minimum", dto.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void REQ_MAR_04_SaveMarginLine_WhenCompliantAndApproved_PersistsSuccessfully()
    {
        // Arrange: Compliant 0.65 mm chamfer with 24 closed points
        var points = new List<LabWorkOrderController.MarginPointDto>();
        for (int i = 0; i < 24; i++)
        {
            points.Add(new LabWorkOrderController.MarginPointDto(Math.Cos(i) * 5, 0.2, Math.Sin(i) * 5, i));
        }

        var req = new LabWorkOrderController.SaveMarginLineRequest(
            ProfileType: "Chamfer",
            PerimeterMm: 33.80,
            AverageWidthMm: 0.65,
            SmoothingTension: 0.60,
            IsClosed: true,
            IsApproved: true,
            HasMarginWarning: false,
            StatusMessage: "Clinically certified finish line ready for 5-axis toolpath compilation.",
            Points: points
        );

        // Act
        var result = _controller.SaveMarginLine("lab-seed-03", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.MarginLineDto>(okResult.Value);

        Assert.Equal("lab-seed-03", dto.OrderId);
        Assert.True(dto.IsApproved);
        Assert.False(dto.HasMarginWarning);
        Assert.Equal(33.80, dto.PerimeterMm);
        Assert.Equal(24, dto.Points.Count);
    }

    [Fact]
    public void REQ_CAM_01_GetCamToolpath_ReturnsSeededZirconiaToolpath()
    {
        // Act
        var result = _controller.GetCamToolpath("lab-seed-01");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.CamToolpathResultDto>(okResult.Value);

        Assert.Equal("lab-seed-01", dto.OrderId);
        Assert.Equal("Zirconia", dto.BlankMaterial);
        Assert.Equal(1.2450, dto.ShrinkageFactor);
        Assert.True(dto.TotalGCodeLines > 20);
        Assert.NotEmpty(dto.Layers);
        Assert.Contains(dto.Layers, l => l.PassType == "Roughing");
        Assert.Contains(dto.Layers, l => l.PassType == "MarginDetail");
    }

    [Fact]
    public void BR_CAM_01_GenerateGCode_EnforcesMarginSafetyBuffer()
    {
        // Arrange
        var req = new LabWorkOrderController.GenerateCamGCodeRequest(
            BlankMaterial: "Zirconia",
            BlankThicknessMm: 14.0,
            ShrinkageFactor: 1.2450,
            SpindleRpm: 24000,
            FeedRateMmMin: 1200,
            StepoverMm: 0.05,
            StepdownMm: 0.50,
            OutputDialect: "ISO6983"
        );

        // Act
        var result = _controller.GenerateGCode("lab-seed-02", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.CamToolpathResultDto>(okResult.Value);

        Assert.True(dto.MarginProtectionEnforced);
        Assert.Contains("BR-CAM-01 MARGIN BUFFER 0.15MM", dto.GCodeContent);
        Assert.Contains("M10 (VACUUM DUST EXTRACTION ON)", dto.GCodeContent);
    }

    [Fact]
    public void BR_CAM_02_BR_CAM_03_GenerateGCode_PMMA_EnforcesCoolantAndFeedLimit()
    {
        // Arrange: Excessive feed rate of 3500 mm/min
        var req = new LabWorkOrderController.GenerateCamGCodeRequest(
            BlankMaterial: "PMMA",
            BlankThicknessMm: 16.0,
            ShrinkageFactor: 1.0000,
            SpindleRpm: 20000,
            FeedRateMmMin: 3500, // Exceeds machine safety limit
            StepoverMm: 0.08,
            StepdownMm: 0.50,
            OutputDialect: "ISO6983"
        );

        // Act
        var result = _controller.GenerateGCode("lab-seed-02", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.CamToolpathResultDto>(okResult.Value);

        Assert.Equal("PMMA", dto.BlankMaterial);
        Assert.True(dto.FeedRateMmMin <= 1400.0, "Feed rate must be clamped to PMMA machine limit (BR-CAM-02).");
        Assert.Contains("M08 (COOLANT FLOOD ON)", dto.GCodeContent);
        Assert.Contains("M09 (COOLANT FLOOD OFF)", dto.GCodeContent);
    }

    [Fact]
    public void BR_CAM_04_GenerateGCode_ValidatesIso6983StandardHeaderAndFooter()
    {
        // Arrange
        var req = new LabWorkOrderController.GenerateCamGCodeRequest(
            BlankMaterial: "Zirconia",
            BlankThicknessMm: 14.0,
            ShrinkageFactor: 1.2450,
            SpindleRpm: 24000,
            FeedRateMmMin: 1200,
            StepoverMm: 0.05,
            StepdownMm: 0.50,
            OutputDialect: "ISO6983"
        );

        // Act
        var result = _controller.GenerateGCode("lab-seed-01", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.CamToolpathResultDto>(okResult.Value);

        Assert.StartsWith("%", dto.GCodeContent.TrimStart());
        Assert.Contains("G90 G21 G17 G40 G49 G80 G94", dto.GCodeContent);
        Assert.Contains("G54", dto.GCodeContent);
        Assert.Contains("M06 T01", dto.GCodeContent);
        Assert.Contains("M30 (PROGRAM END AND REWIND)", dto.GCodeContent);
        Assert.True(dto.EstimatedMillingMinutes > 0);
    }

    [Fact]
    public void GetSurgicalGuide_ReturnsDefaultNobelActiveConfiguration()
    {
        // Act: use pristine order lab-seed-02 to verify default initialization
        var result = _controller.GetSurgicalGuide("lab-seed-02");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.ImplantSurgicalGuideDto>(okResult.Value);

        Assert.Equal("NobelActive", dto.ImplantSystem);
        Assert.Equal(5.0, dto.SleeveDiameterMm);
        Assert.Equal(4.2, dto.SleeveInnerDiameterMm);
        Assert.Equal(9.0, dto.DrillOffsetMm);
        Assert.Equal(2.5, dto.StentWallThicknessMm);
        Assert.True(dto.SafetyClearanceMm >= 2.0);
        Assert.False(dto.HasSafetyWarning);
        Assert.True(dto.HasInspectionWindow);
    }

    [Fact]
    public void BR_SG_01_SaveSurgicalGuide_EnforcesSafetyClearanceWarning()
    {
        // Arrange: Clearance drops to 1.2 mm (< 2.0 mm limit)
        var req = new LabWorkOrderController.SaveImplantSurgicalGuideRequest(
            ImplantSystem: "NobelActive",
            SleeveDiameterMm: 5.0,
            SleeveInnerDiameterMm: 4.2,
            SleeveHeightMm: 5.0,
            DrillOffsetMm: 9.0,
            BuccoLingualAngleDeg: 5.0,
            MesioDistalAngleDeg: 2.0,
            StentWallThicknessMm: 2.5,
            SafetyClearanceMm: 1.2,
            HasInspectionWindow: true
        );

        // Act
        var result = _controller.SaveSurgicalGuide("lab-seed-01", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.ImplantSurgicalGuideDto>(okResult.Value);

        Assert.True(dto.HasSafetyWarning, "BR-SG-01: Must flag warning when safety clearance is below 2.0 mm.");
        Assert.Contains("BR-SG-01 WARNING", dto.StatusMessage);
        Assert.Equal(1.2, dto.SafetyClearanceMm);
    }

    [Fact]
    public void BR_SG_02_SaveSurgicalGuide_EnforcesMinimumStentWallThickness()
    {
        // Arrange: User requests 1.0 mm (violates 2.5 mm minimum)
        var req = new LabWorkOrderController.SaveImplantSurgicalGuideRequest(
            ImplantSystem: "NobelActive",
            SleeveDiameterMm: 5.0,
            SleeveInnerDiameterMm: 4.2,
            SleeveHeightMm: 5.0,
            DrillOffsetMm: 9.0,
            BuccoLingualAngleDeg: 0.0,
            MesioDistalAngleDeg: 0.0,
            StentWallThicknessMm: 1.0,
            SafetyClearanceMm: 3.5,
            HasInspectionWindow: true
        );

        // Act
        var result = _controller.SaveSurgicalGuide("lab-seed-01", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.ImplantSurgicalGuideDto>(okResult.Value);

        Assert.Equal(2.5, dto.StentWallThicknessMm); // Clamped to minimum 2.5 mm
    }

    [Fact]
    public void BR_SG_04_SaveSurgicalGuide_AppliesStraumannAndZimmerDimensions()
    {
        // Act: Straumann
        var reqStraumann = new LabWorkOrderController.SaveImplantSurgicalGuideRequest(
            ImplantSystem: "StraumannBLX",
            SleeveDiameterMm: null,
            SleeveInnerDiameterMm: null,
            SleeveHeightMm: null,
            DrillOffsetMm: null,
            BuccoLingualAngleDeg: 3.0,
            MesioDistalAngleDeg: 1.0,
            StentWallThicknessMm: 2.8,
            SafetyClearanceMm: 3.8,
            HasInspectionWindow: true
        );
        var res1 = Assert.IsType<OkObjectResult>(_controller.SaveSurgicalGuide("lab-seed-01", reqStraumann));
        var dto1 = Assert.IsType<LabWorkOrderController.ImplantSurgicalGuideDto>(res1.Value);

        Assert.Equal("StraumannBLX", dto1.ImplantSystem);
        Assert.Equal(4.8, dto1.SleeveDiameterMm);
        Assert.Equal(4.0, dto1.SleeveInnerDiameterMm);
        Assert.Equal(10.0, dto1.DrillOffsetMm);

        // Act: Zimmer
        var reqZimmer = new LabWorkOrderController.SaveImplantSurgicalGuideRequest(
            ImplantSystem: "ZimmerTSV",
            SleeveDiameterMm: null,
            SleeveInnerDiameterMm: null,
            SleeveHeightMm: null,
            DrillOffsetMm: null,
            BuccoLingualAngleDeg: 0.0,
            MesioDistalAngleDeg: 0.0,
            StentWallThicknessMm: 2.5,
            SafetyClearanceMm: 4.0,
            HasInspectionWindow: true
        );
        var res2 = Assert.IsType<OkObjectResult>(_controller.SaveSurgicalGuide("lab-seed-01", reqZimmer));
        var dto2 = Assert.IsType<LabWorkOrderController.ImplantSurgicalGuideDto>(res2.Value);

        Assert.Equal("ZimmerTSV", dto2.ImplantSystem);
        Assert.Equal(5.2, dto2.SleeveDiameterMm);
        Assert.Equal(4.4, dto2.SleeveInnerDiameterMm);
        Assert.Equal(9.0, dto2.DrillOffsetMm);
    }

    [Fact]
    public void GetCrownMorphology_ReturnsDefaultCrownPlan()
    {
        // Act: use unmutated order
        var result = _controller.GetCrownMorphology("lab-seed-02");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.CrownMorphologyPlanDto>(okResult.Value);

        Assert.Equal("lab-seed-02", dto.OrderId);
        Assert.Equal("Incisor", dto.ToothType);
        Assert.Equal(40, dto.CementSpacerMicrons);
        Assert.Equal(0.5, dto.MarginCollarMm);
        Assert.Equal(1.8, dto.OcclusalReductionMm);
        Assert.False(dto.HasOcclusalWarning);
        Assert.True(dto.IsRestorationApproved);
    }

    [Fact]
    public void BR_CROWN_01_SaveCrownMorphology_EnforcesCementSpacerAndCollar()
    {
        // Arrange
        var req = new LabWorkOrderController.SaveCrownMorphologyRequest(
            ToothType: "Molar",
            CementSpacerMicrons: 45,
            MarginCollarMm: 0.6,
            OcclusalReductionMm: 1.8,
            ProximalContactStrength: "Tight",
            CuspHeightScale: 1.1,
            IsRestorationApproved: true
        );

        // Act
        var result = _controller.SaveCrownMorphology("lab-seed-01", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.CrownMorphologyPlanDto>(okResult.Value);

        Assert.Equal(45, dto.CementSpacerMicrons);
        Assert.Equal(0.6, dto.MarginCollarMm);
        Assert.Equal("Tight", dto.ProximalContactStrength);
        Assert.False(dto.HasOcclusalWarning);
        Assert.True(dto.IsRestorationApproved);
    }

    [Fact]
    public void BR_CROWN_03_SaveCrownMorphology_EnforcesOcclusalReductionSafetyWarning()
    {
        // Arrange: Inadequate occlusal reduction (< 1.5mm for Zirconia)
        var req = new LabWorkOrderController.SaveCrownMorphologyRequest(
            ToothType: "Molar",
            CementSpacerMicrons: 40,
            MarginCollarMm: 0.5,
            OcclusalReductionMm: 1.2, // Below 1.5mm threshold
            ProximalContactStrength: "Normal",
            CuspHeightScale: 1.0,
            IsRestorationApproved: null
        );

        // Act
        var result = _controller.SaveCrownMorphology("lab-seed-01", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.CrownMorphologyPlanDto>(okResult.Value);

        Assert.True(dto.HasOcclusalWarning);
        Assert.False(dto.IsRestorationApproved);
        Assert.Contains("BR-CROWN-03 WARNING", dto.StatusMessage);
    }

    // --- Release v11.0.0 Dual-Modality CBCT-DICOM & Intraoral STL Fusion Tests ---

    [Fact]
    public void REQ_FUSION_01_GetFusionPlan_Returns_Default_Fusion_Plan()
    {
        // Act
        var result = _controller.GetFusionPlan("lab-seed-03");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.DualModalityFusionPlanDto>(okResult.Value);

        Assert.Equal("lab-seed-03", dto.OrderId);
        Assert.True(dto.RegistrationLandmarks.Count >= 3);
        Assert.True(dto.RmsErrorMm <= 0.35); // Clinical compliance threshold
        Assert.False(dto.HasNerveSafetyWarning);
        Assert.True(dto.IsFusionApproved);
    }

    [Fact]
    public void BR_FUSION_01_SaveFusionPlan_Computes_Rms_CoRegistration()
    {
        // Arrange
        var landmarks = new List<LabWorkOrderController.FusionPointPairDto>
        {
            new(10.0, 5.0, 15.0, 10.1, 5.1, 15.0, "Cusp 1"),
            new(-10.0, 5.0, 15.0, -9.9, 5.0, 15.1, "Cusp 2"),
            new(0.0, -8.0, 12.0, 0.1, -8.1, 12.0, "Cingulum")
        };

        var req = new LabWorkOrderController.SaveDualModalityFusionRequest(
            DicomSeriesUid: "1.2.840.10008.5.1.4.1.1.2.999",
            RegistrationLandmarks: landmarks,
            BoneOpacityPercent: 60.0,
            SoftTissueOpacityPercent: 40.0,
            SubsurfaceBoneClearanceMm: 3.5,
            NerveDistanceMm: 3.8,
            IsFusionApproved: true
        );

        // Act
        var result = _controller.SaveFusionPlan("lab-seed-02", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.DualModalityFusionPlanDto>(okResult.Value);

        Assert.Equal("lab-seed-02", dto.OrderId);
        Assert.Equal(3, dto.RegistrationLandmarks.Count);
        Assert.True(dto.RmsErrorMm <= 0.35);
        Assert.False(dto.HasNerveSafetyWarning);
        Assert.True(dto.IsFusionApproved);
        Assert.Contains("Dual-modality fusion approved", dto.StatusMessage);
    }

    [Fact]
    public void BR_FUSION_03_SaveFusionPlan_Enforces_Mandibular_Nerve_Safety_Warning()
    {
        // Arrange: Subsurface nerve distance < 2.0mm violates clinical safety buffer
        var req = new LabWorkOrderController.SaveDualModalityFusionRequest(
            DicomSeriesUid: "1.2.840.10008.5.1.4.1.1.2.999",
            RegistrationLandmarks: null,
            BoneOpacityPercent: 50.0,
            SoftTissueOpacityPercent: 50.0,
            SubsurfaceBoneClearanceMm: 2.5,
            NerveDistanceMm: 1.4, // Dangerous proximity (< 2.0mm)
            IsFusionApproved: null
        );

        // Act
        var result = _controller.SaveFusionPlan("lab-seed-01", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<LabWorkOrderController.DualModalityFusionPlanDto>(okResult.Value);

        Assert.True(dto.HasNerveSafetyWarning);
        Assert.False(dto.IsFusionApproved);
        Assert.Contains("BR-FUSION-03 WARNING", dto.StatusMessage);
    }

    [Fact]
    public void REQ_ALIGN_01_GetAlignerPlan_Returns_Default_Staging_Plan()
    {
        // Act
        var result = _controller.GetAlignerPlan("lab-seed-03");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var plan = Assert.IsType<LabWorkOrderController.AlignerPlanDto>(okResult.Value);

        Assert.Equal("lab-seed-03", plan.OrderId);
        Assert.Equal(16, plan.TotalStages);
        Assert.True(plan.IsVelocityCompliant);
        Assert.True(plan.IsTreatmentApproved);
        Assert.NotEmpty(plan.Stages);
    }

    [Fact]
    public void BR_ALIGN_01_SaveAlignerPlan_Compliant_Velocity_Approves_Plan()
    {
        // Arrange
        var req = new LabWorkOrderController.SaveAlignerPlanRequest(
            TotalStages: 18,
            CurrentStage: 1,
            MaxTranslationDeltaMm: 0.22,
            MaxRotationDeltaDeg: 1.8,
            TotalIprMm: 1.5,
            FoilThicknessMm: 0.75,
            IsTreatmentApproved: true
        );

        // Act
        var result = _controller.SaveAlignerPlan("lab-seed-02", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var plan = Assert.IsType<LabWorkOrderController.AlignerPlanDto>(okResult.Value);

        Assert.Equal("lab-seed-02", plan.OrderId);
        Assert.Equal(18, plan.TotalStages);
        Assert.True(plan.IsVelocityCompliant);
        Assert.True(plan.IsTreatmentApproved);
        Assert.Contains("approved", plan.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BR_ALIGN_01_SaveAlignerPlan_Velocity_Violation_Subdivides_Stages()
    {
        // Arrange: 0.35 mm translation exceeds 0.25 mm physiological limit
        var req = new LabWorkOrderController.SaveAlignerPlanRequest(
            TotalStages: 10,
            CurrentStage: 1,
            MaxTranslationDeltaMm: 0.35, // Velocity violation (> 0.25 mm)
            MaxRotationDeltaDeg: 2.5,   // Velocity violation (> 2.0 deg)
            TotalIprMm: 1.0,
            FoilThicknessMm: 0.75,
            IsTreatmentApproved: null
        );

        // Act
        var result = _controller.SaveAlignerPlan("lab-seed-01", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var plan = Assert.IsType<LabWorkOrderController.AlignerPlanDto>(okResult.Value);

        Assert.False(plan.IsVelocityCompliant);
        Assert.False(plan.IsTreatmentApproved);
        Assert.True(plan.TotalStages > 10); // Automatically subdivided
        Assert.Contains("BR-ALIGN-01 WARNING", plan.StatusMessage);
    }

    [Fact]
    public void REQ_GBR_01_GetGbrMeshPlan_Returns_Default_Mesh_Plan()
    {
        // Act
        var result = _controller.GetGbrMeshPlan("lab-seed-03");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var plan = Assert.IsType<LabWorkOrderController.GbrTitaniumMeshPlanDto>(okResult.Value);

        Assert.Equal("lab-seed-03", plan.OrderId);
        Assert.Equal(450.0, plan.DefectVolumeMm3);
        Assert.Equal(517.5, plan.RecommendedGraftVolumeMm3);
        Assert.Equal(0.15, plan.MeshThicknessMm);
        Assert.Equal(50.0, plan.PorosityPercent);
        Assert.Equal(4, plan.FixationScrewsCount);
        Assert.False(plan.HasSafetyCorridorViolation);
        Assert.True(plan.IsFabricationApproved);
        Assert.NotEmpty(plan.FixationScrews);
    }

    [Fact]
    public void BR_GBR_01_SaveGbrMeshPlan_Compliant_Screws_Approves_Plan()
    {
        // Arrange: 600 mm3 defect requires 690 mm3 graft (+15%)
        var req = new LabWorkOrderController.SaveGbrMeshPlanRequest(
            DefectVolumeMm3: 600.0,
            MeshThicknessMm: 0.18,
            PorosityPercent: 48.0,
            PoreDiameterMm: 1.1,
            TentPoleCrestalClearanceMm: 2.8,
            IsFabricationApproved: true,
            FixationScrews: null
        );

        // Act
        var result = _controller.SaveGbrMeshPlan("lab-seed-02", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var plan = Assert.IsType<LabWorkOrderController.GbrTitaniumMeshPlanDto>(okResult.Value);

        Assert.Equal("lab-seed-02", plan.OrderId);
        Assert.Equal(600.0, plan.DefectVolumeMm3);
        Assert.Equal(690.0, plan.RecommendedGraftVolumeMm3); // +15% overfill
        Assert.Equal(0.18, plan.MeshThicknessMm);
        Assert.False(plan.HasSafetyCorridorViolation);
        Assert.True(plan.IsFabricationApproved);
        Assert.Contains("approved", plan.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BR_GBR_03_SaveGbrMeshPlan_Corridor_Violation_Flags_Warning()
    {
        // Arrange: Screw-2 has 0.8 mm nerve clearance (< 2.0 mm safety corridor)
        var unsafeScrews = new List<LabWorkOrderController.GbrFixationScrewDto>
        {
            new("screw-1", -6.5, -4.2, 2.1, 1.5, 4.0, 2.4, 3.2, true, false),
            new("screw-2", -2.1, -5.0, 1.8, 1.5, 4.0, 2.1, 0.8, true, true) // Safety corridor breach
        };

        var req = new LabWorkOrderController.SaveGbrMeshPlanRequest(
            DefectVolumeMm3: 500.0,
            MeshThicknessMm: 0.15,
            PorosityPercent: 50.0,
            PoreDiameterMm: 1.0,
            TentPoleCrestalClearanceMm: 2.5,
            IsFabricationApproved: null,
            FixationScrews: unsafeScrews
        );

        // Act
        var result = _controller.SaveGbrMeshPlan("lab-seed-01", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var plan = Assert.IsType<LabWorkOrderController.GbrTitaniumMeshPlanDto>(okResult.Value);

        Assert.True(plan.HasSafetyCorridorViolation);
        Assert.False(plan.IsFabricationApproved);
        Assert.Contains("BR-GBR-03 WARNING", plan.StatusMessage);
    }

    [Fact]
    public void BR_RPD_01_GetRpdPlan_Returns_Default_Plan()
    {
        // Act
        var result = _controller.GetRpdPlan("lab-seed-01");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var plan = Assert.IsType<LabWorkOrderController.RpdSurveyorPlanDto>(okResult.Value);

        Assert.Equal("lab-seed-01", plan.OrderId);
        Assert.Equal("Mandibular", plan.Arch);
        Assert.Equal("ClassII", plan.KennedyClassification);
        Assert.Equal(2.5, plan.TiltBuccolingualDeg);
        Assert.Equal(-1.5, plan.TiltMesiodistalDeg);
        Assert.Equal("LingualBar", plan.MajorConnectorType);
        Assert.Equal(8.5, plan.FloorOfMouthDepthMm);
        Assert.False(plan.HasTissueImpingementWarning);
        Assert.True(plan.IsFabricationApproved);
        Assert.NotEmpty(plan.Clasps);
        Assert.Contains(plan.Clasps, c => c.ClaspType == "Akers" && c.UndercutDepthMm == 0.25);
    }

    [Fact]
    public void BR_RPD_02_SaveRpdPlan_Compliant_Returns_Approved()
    {
        // Arrange: Maxillary Class III with Palatal Strap
        var req = new LabWorkOrderController.SaveRpdPlanRequest(
            Arch: "Maxillary",
            KennedyClassification: "ClassIII",
            TiltBuccolingualDeg: 1.0,
            TiltMesiodistalDeg: 0.5,
            MajorConnectorType: "PalatalStrap",
            MajorConnectorThicknessMm: 1.2,
            FloorOfMouthDepthMm: 9.0,
            IsFabricationApproved: true,
            Clasps: null
        );

        // Act
        var result = _controller.SaveRpdPlan("lab-seed-02", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var plan = Assert.IsType<LabWorkOrderController.RpdSurveyorPlanDto>(okResult.Value);

        Assert.Equal("lab-seed-02", plan.OrderId);
        Assert.Equal("Maxillary", plan.Arch);
        Assert.Equal("ClassIII", plan.KennedyClassification);
        Assert.Equal("PalatalStrap", plan.MajorConnectorType);
        Assert.False(plan.HasTissueImpingementWarning);
        Assert.True(plan.IsFabricationApproved);
        Assert.Contains("approved", plan.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BR_RPD_02_SaveRpdPlan_FloorOfMouth_LessThan_7mm_Flags_Impingement()
    {
        // Arrange: Mandibular LingualBar with only 5.2 mm depth (< 7.0 mm threshold)
        var req = new LabWorkOrderController.SaveRpdPlanRequest(
            Arch: "Mandibular",
            KennedyClassification: "ClassI",
            TiltBuccolingualDeg: 2.0,
            TiltMesiodistalDeg: -1.0,
            MajorConnectorType: "LingualBar",
            MajorConnectorThicknessMm: 1.5,
            FloorOfMouthDepthMm: 5.2, // Contraindicated for LingualBar
            IsFabricationApproved: null,
            Clasps: null
        );

        // Act
        var result = _controller.SaveRpdPlan("lab-seed-01", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var plan = Assert.IsType<LabWorkOrderController.RpdSurveyorPlanDto>(okResult.Value);

        Assert.True(plan.HasTissueImpingementWarning);
        Assert.False(plan.IsFabricationApproved);
        Assert.Contains("BR-RPD-02 WARNING", plan.StatusMessage);
        Assert.Contains("LingualPlate", plan.StatusMessage);
    }

    [Fact]
    public void BR_DSD_01_GetDsdSmilePlan_Returns_Default_Plan()
    {
        // Act
        var result = _controller.GetDsdSmilePlan("lab-seed-03");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var plan = Assert.IsType<LabWorkOrderController.DsdSmilePlanDto>(okResult.Value);

        Assert.Equal("lab-seed-03", plan.OrderId);
        Assert.Equal(8.5, plan.CentralIncisorWidthMm);
        Assert.Equal(10.5, plan.CentralIncisorLengthMm);
        Assert.Equal(80.0, plan.TargetWidthToLengthRatio);
        Assert.True(plan.IsAestheticHarmonized);
        Assert.Equal(6, plan.Proportions.Count);
        Assert.Contains(plan.Proportions, p => p.ToothFdi == "11" && p.WidthMm == 8.5);
        Assert.Contains(plan.Proportions, p => p.ToothFdi == "12" && p.IsGoldenProportionCompliant);
    }

    [Fact]
    public void BR_DSD_02_SaveDsdSmilePlan_Compliant_Returns_Approved()
    {
        // Arrange: Ideal 80% central incisor ratio (8.8 mm / 11.0 mm)
        var req = new LabWorkOrderController.SaveDsdSmilePlanRequest(
            FacialMidlineOffsetMm: 0.2,
            InterpupillaryAngleDeg: -0.5,
            SmileArcCurvatureRadiusMm: 44.0,
            CentralIncisorWidthMm: 8.8,
            CentralIncisorLengthMm: 11.0,
            TargetWidthToLengthRatio: 80.0,
            IsAestheticHarmonized: true,
            Proportions: null
        );

        // Act
        var result = _controller.SaveDsdSmilePlan("lab-seed-02", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var plan = Assert.IsType<LabWorkOrderController.DsdSmilePlanDto>(okResult.Value);

        Assert.Equal("lab-seed-02", plan.OrderId);
        Assert.Equal(8.8, plan.CentralIncisorWidthMm);
        Assert.Equal(11.0, plan.CentralIncisorLengthMm);
        Assert.True(plan.IsAestheticHarmonized);
        Assert.Contains("approved", plan.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BR_DSD_02_SaveDsdSmilePlan_Deviation_Flags_Warning()
    {
        // Arrange: Microdontic lateral incisor #22 with -25% golden ratio deviation
        var proportionsWithDefect = new List<LabWorkOrderController.DsdToothProportionDto>
        {
            new("13", 3.25, 11.0, 29.5, 0.0, true),
            new("12", 5.25, 9.5, 55.3, 0.0, true),
            new("11", 8.5, 10.5, 81.0, 0.0, true),
            new("21", 8.5, 10.5, 81.0, 0.0, true),
            new("22", 3.9, 9.5, 41.1, -25.7, false), // > 15% deviation
            new("23", 3.25, 11.0, 29.5, 0.0, true)
        };

        var req = new LabWorkOrderController.SaveDsdSmilePlanRequest(
            FacialMidlineOffsetMm: 0.0,
            InterpupillaryAngleDeg: 0.0,
            SmileArcCurvatureRadiusMm: 42.0,
            CentralIncisorWidthMm: 8.5,
            CentralIncisorLengthMm: 10.5,
            TargetWidthToLengthRatio: 80.0,
            IsAestheticHarmonized: null,
            Proportions: proportionsWithDefect
        );

        // Act
        var result = _controller.SaveDsdSmilePlan("lab-seed-01", req);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var plan = Assert.IsType<LabWorkOrderController.DsdSmilePlanDto>(okResult.Value);

        Assert.False(plan.IsAestheticHarmonized);
        Assert.Contains("BR-DSD-02 WARNING", plan.StatusMessage);
        Assert.Contains("aesthetic veneer mock-up", plan.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }
}


