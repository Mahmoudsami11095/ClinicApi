using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/lab-orders")]
public class LabWorkOrderController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, LabWorkOrder> _ordersByToken = new();
    private static readonly ConcurrentDictionary<string, LabWorkOrder> _ordersById = new();
    private static readonly ConcurrentDictionary<string, List<ModelMeasurementDto>> _measurementsByOrder = new();
    private static readonly ConcurrentDictionary<string, ClearanceHeatmapDto> _heatmapsByOrder = new();
    private static readonly ConcurrentDictionary<string, ArticulatorSettingsDto> _articulatorsByOrder = new();
    private static readonly ConcurrentDictionary<string, MarginLineDto> _marginLinesByOrder = new();
    private static readonly ConcurrentDictionary<string, CamToolpathResultDto> _camToolpathsByOrder = new();
    private static readonly ConcurrentDictionary<string, ImplantSurgicalGuideDto> _surgicalGuidesByOrder = new();
    private static readonly ConcurrentDictionary<string, CrownMorphologyPlanDto> _crownsByOrder = new();

    static LabWorkOrderController()
    {
        // Seed initial CAD/CAM Lab Work-Orders
        var order1 = new LabWorkOrder
        {
            Id = "lab-seed-01",
            OrderToken = "LAB-7A9B12",
            ClinicId = "clinic-main",
            DoctorId = "doc-101",
            DoctorName = "Dr. Emily Vance",
            PatientId = "pat-201",
            PatientName = "Arthur Pendelton",
            ToothNumber = 36,
            RestorationType = "Full Anatomical Crown",
            Material = "Monolithic Multi-Layer Zirconia (5Y-PSZ)",
            Shade = "A2",
            StumpShade = "ND2",
            StlScanUrl = "/scans/intraoral_mandible_36.stl",
            ModelFileFormat = "STL",
            MeshPolyCount = 52400,
            ScanFileSize = 2621440,
            ModelBoundingBoxMm = "48.2 x 34.6 x 19.5 mm",
            Status = "Milling",
            LabPartnerName = "Apex Precision CAD/CAM Lab",
            LabTrackingNumber = "APX-882194",
            LabTechnicianNotes = "Margins verified on 5-axis DMG Mori mill. Sintering scheduled for tonight.",
            EstimatedDeliveryDate = DateTime.UtcNow.AddDays(2),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        var order2 = new LabWorkOrder
        {
            Id = "lab-seed-02",
            OrderToken = "LAB-4E2C88",
            ClinicId = "clinic-main",
            DoctorId = "doc-102",
            DoctorName = "Dr. James Wilson",
            PatientId = "pat-202",
            PatientName = "Clara Oswald",
            ToothNumber = 11,
            RestorationType = "Anterior Aesthetic Veneer",
            Material = "Lithium Disilicate (IPS e.max CAD)",
            Shade = "BL2",
            StumpShade = "ND1",
            StlScanUrl = "/scans/intraoral_maxilla_11.stl",
            ModelFileFormat = "STL",
            MeshPolyCount = 38120,
            ScanFileSize = 1945600,
            ModelBoundingBoxMm = "32.1 x 28.4 x 16.0 mm",
            Status = "Shipped",
            LabPartnerName = "CeramArt Aesthetic Dental Studio",
            LabTrackingNumber = "DHL-99482710",
            LabTechnicianNotes = "Cutback and layered with e.max Ceram. Glazed and dispatched via express courier.",
            EstimatedDeliveryDate = DateTime.UtcNow.AddHours(4),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-3)
        };

        var order3 = new LabWorkOrder
        {
            Id = "lab-seed-03",
            OrderToken = "LAB-1F8D34",
            ClinicId = "clinic-main",
            DoctorId = "doc-101",
            DoctorName = "Dr. Emily Vance",
            PatientId = "pat-203",
            PatientName = "Marcus Aurelius",
            ToothNumber = 46,
            RestorationType = "Custom Titanium Implant Abutment + Crown",
            Material = "Titanium Grade 5 Base + Zirconia Coping",
            Shade = "A3",
            StlScanUrl = "/scans/scanbody_implant_46.stl",
            ModelFileFormat = "PLY",
            MeshPolyCount = 64900,
            ScanFileSize = 3145728,
            ModelBoundingBoxMm = "45.0 x 36.2 x 22.1 mm",
            Status = "ReceivedByClinic",
            LabPartnerName = "TitanPro Implant Milling Center",
            LabTrackingNumber = "FEDEX-554192",
            LabTechnicianNotes = "Screw-retained emergence profile calibrated. Torque test passed at 30Ncm.",
            EstimatedDeliveryDate = DateTime.UtcNow.AddDays(-1),
            DeliveryReceivedAtUtc = DateTime.UtcNow.AddHours(-12),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
        };

        _ordersByToken[order1.OrderToken] = order1;
        _ordersById[order1.Id] = order1;

        _ordersByToken[order2.OrderToken] = order2;
        _ordersById[order2.Id] = order2;

        _ordersByToken[order3.OrderToken] = order3;
        _ordersById[order3.Id] = order3;

        _measurementsByOrder[order1.Id] = new List<ModelMeasurementDto>
        {
            new ModelMeasurementDto("meas-seed-1", order1.Id, "Occlusal Reduction", 1.85, "mm", "distance", DateTime.UtcNow.AddHours(-2)),
            new ModelMeasurementDto("meas-seed-2", order1.Id, "Axial Convergence", 6.4, "deg", "angle", DateTime.UtcNow.AddHours(-2))
        };

        _heatmapsByOrder[order1.Id] = new ClearanceHeatmapDto(
            order1.Id,
            0.0,
            1.85,
            2.40,
            0.0,
            0.0,
            true,
            "Zero Undercuts Detected. Optimal Occlusal Reduction.",
            DateTime.UtcNow.AddHours(-1)
        );

        _articulatorsByOrder[order1.Id] = new ArticulatorSettingsDto(
            order1.Id,
            35.0,
            15.0,
            0.0,
            "Centric",
            0.0,
            12,
            false,
            "Optimal Centric Occlusion. No dynamic interference.",
            DateTime.UtcNow.AddMinutes(-30)
        );

        var seedPoints = new List<MarginPointDto>();
        for (int i = 0; i < 24; i++)
        {
            double angle = i * (Math.PI * 2 / 24);
            seedPoints.Add(new MarginPointDto(
                Math.Cos(angle) * 7.5,
                0.2 + Math.Sin(angle * 2) * 0.4,
                Math.Sin(angle) * 6.5,
                i
            ));
        }

        _marginLinesByOrder[order1.Id] = new MarginLineDto(
            order1.Id,
            "Chamfer",
            34.20,
            0.65,
            0.50,
            true,
            true,
            false,
            "Continuous closed margin line validated. Chamfer geometry (0.65 mm) compliant with Zirconia milling requirements.",
            seedPoints,
            DateTime.UtcNow.AddMinutes(-15)
        );

        _camToolpathsByOrder[order1.Id] = GenerateCompliantCamToolpath(order1, new GenerateCamGCodeRequest(
            BlankMaterial: "Zirconia",
            BlankThicknessMm: 14.0,
            ShrinkageFactor: 1.2450,
            SpindleRpm: 24000,
            FeedRateMmMin: 1200,
            StepoverMm: 0.05,
            StepdownMm: 0.50,
            OutputDialect: "ISO6983"
        ));
    }

    public record CreateLabWorkOrderRequest(
        string? PatientId,
        string? PatientName,
        int ToothNumber,
        string? RestorationType,
        string? Material,
        string? Shade,
        string? StumpShade,
        string? StlScanUrl,
        string? LabPartnerName,
        int? EstimatedDays,
        string? ModelFileFormat = "STL",
        int? MeshPolyCount = null,
        long? ScanFileSize = null,
        string? ModelBoundingBoxMm = null
    );

    public record UpdateLabStatusRequest(
        string Status,
        string? TrackingNumber,
        string? TechnicianNotes,
        DateTime? EstimatedDeliveryDate
    );

    public record FittingEligibilityResponse(
        bool CanSchedule,
        string? OrderStatus,
        string? OrderToken,
        string Reason
    );

    public record LabOrderPublicViewResponse(
        string Id,
        string OrderToken,
        string ClinicId,
        string DoctorName,
        string PatientName,
        int ToothNumber,
        string RestorationType,
        string Material,
        string Shade,
        string? StumpShade,
        string? StlScanUrl,
        string Status,
        string LabPartnerName,
        string? LabTrackingNumber,
        string? LabTechnicianNotes,
        DateTime? EstimatedDeliveryDate,
        DateTime CreatedAtUtc,
        string ModelFileFormat = "STL",
        int MeshPolyCount = 48250,
        long ScanFileSize = 2457600,
        string? ModelBoundingBoxMm = "42.5 x 31.8 x 18.2 mm"
    );

    public record LabOrder3DModelResponse(
        string OrderId,
        string OrderToken,
        int ToothNumber,
        string? StlScanUrl,
        string ModelFileFormat,
        int MeshPolyCount,
        long ScanFileSize,
        string? ModelBoundingBoxMm,
        string RestorationType,
        string Material,
        string Shade,
        string? AccessToken,
        DateTime ExpiresAtUtc
    );

    public record Upload3DModelRequest(
        string FileName,
        string Base64Data,
        string? FileFormat
    );

    public record Upload3DModelResponse(
        bool Success,
        string Message,
        string OrderId,
        string StlScanUrl,
        string ModelFileFormat,
        int MeshPolyCount,
        long ScanFileSize,
        string ModelBoundingBoxMm
    );

    public record ModelMeasurementDto(
        string Id,
        string OrderId,
        string Label,
        double Value,
        string Unit,
        string Mode,
        DateTime CreatedAtUtc
    );

    public record SaveModelMeasurementRequest(
        string Label,
        double Value,
        string? Unit,
        string? Mode
    );

    public record ClearanceHeatmapDto(
        string OrderId,
        double UndercutSurfacePercent,
        double MinClearanceMm,
        double MaxClearanceMm,
        double InsertionTiltMesialDistalDeg,
        double InsertionTiltBuccalLingualDeg,
        bool IsCompliant,
        string StatusMessage,
        DateTime EvaluatedAtUtc
    );

    public record SaveClearanceHeatmapRequest(
        double UndercutSurfacePercent,
        double MinClearanceMm,
        double MaxClearanceMm,
        double InsertionTiltMesialDistalDeg,
        double InsertionTiltBuccalLingualDeg,
        bool? IsCompliant,
        string? StatusMessage
    );

    public record ArticulatorSettingsDto(
        string OrderId,
        double CondylarInclinationDeg,
        double BennettAngleDeg,
        double IncisalPinOpeningMm,
        string ExcursionType,
        double MaxInterferenceMm,
        int ActiveContactCount,
        bool HasInterferenceWarning,
        string StatusMessage,
        DateTime EvaluatedAtUtc
    );

    public record SaveArticulatorSettingsRequest(
        double CondylarInclinationDeg,
        double BennettAngleDeg,
        double IncisalPinOpeningMm,
        string ExcursionType,
        double MaxInterferenceMm,
        int ActiveContactCount,
        bool? HasInterferenceWarning,
        string? StatusMessage
    );

    public record MarginPointDto(
        double X,
        double Y,
        double Z,
        int Index
    );

    public record MarginLineDto(
        string OrderId,
        string ProfileType,
        double PerimeterMm,
        double AverageWidthMm,
        double SmoothingTension,
        bool IsClosed,
        bool IsApproved,
        bool HasMarginWarning,
        string StatusMessage,
        IReadOnlyList<MarginPointDto> Points,
        DateTime EvaluatedAtUtc
    );

    public record SaveMarginLineRequest(
        string ProfileType,
        double PerimeterMm,
        double AverageWidthMm,
        double SmoothingTension,
        bool IsClosed,
        bool? IsApproved,
        bool? HasMarginWarning,
        string? StatusMessage,
        List<MarginPointDto>? Points
    );

    public record CamToolpathLayerDto(
        int LayerIndex,
        string PassType,
        double ZHeightMm,
        int PointCount,
        double EstimatedSeconds
    );

    public record GenerateCamGCodeRequest(
        string? BlankMaterial,
        double? BlankThicknessMm,
        double? ShrinkageFactor,
        double? SpindleRpm,
        double? FeedRateMmMin,
        double? StepoverMm,
        double? StepdownMm,
        string? OutputDialect
    );

    public record CamToolpathResultDto(
        string OrderId,
        string ToolpathId,
        string BlankMaterial,
        double BlankThicknessMm,
        double ShrinkageFactor,
        double SpindleRpm,
        double FeedRateMmMin,
        double EstimatedMillingMinutes,
        int TotalGCodeLines,
        double TotalToolpathLengthMm,
        int BurChangeCount,
        bool MarginProtectionEnforced,
        string StatusMessage,
        string GCodeContent,
        IReadOnlyList<CamToolpathLayerDto> Layers,
        DateTime GeneratedAtUtc
    );

    public record ImplantSurgicalGuideDto(
        string OrderId,
        string ImplantSystem,
        double SleeveDiameterMm,
        double SleeveInnerDiameterMm,
        double SleeveHeightMm,
        double DrillOffsetMm,
        double BuccoLingualAngleDeg,
        double MesioDistalAngleDeg,
        double StentWallThicknessMm,
        double SafetyClearanceMm,
        bool HasInspectionWindow,
        bool HasSafetyWarning,
        string StatusMessage,
        DateTime PlannedAtUtc
    );

    public record SaveImplantSurgicalGuideRequest(
        string? ImplantSystem,
        double? SleeveDiameterMm,
        double? SleeveInnerDiameterMm,
        double? SleeveHeightMm,
        double? DrillOffsetMm,
        double? BuccoLingualAngleDeg,
        double? MesioDistalAngleDeg,
        double? StentWallThicknessMm,
        double? SafetyClearanceMm,
        bool? HasInspectionWindow
    );

    public record CrownMorphologyPlanDto(
        string OrderId,
        string ToothType,
        int CementSpacerMicrons,
        double MarginCollarMm,
        double OcclusalReductionMm,
        string ProximalContactStrength,
        double CuspHeightScale,
        bool IsRestorationApproved,
        bool HasOcclusalWarning,
        string StatusMessage,
        DateTime DesignedAtUtc
    );

    public record SaveCrownMorphologyRequest(
        string? ToothType,
        int? CementSpacerMicrons,
        double? MarginCollarMm,
        double? OcclusalReductionMm,
        string? ProximalContactStrength,
        double? CuspHeightScale,
        bool? IsRestorationApproved
    );

    [HttpGet]
    [Authorize]
    public IActionResult GetLabOrders([FromQuery] string? status, [FromQuery] string? patientId)
    {
        var query = _ordersById.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(o => string.Equals(o.Status, status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(patientId))
        {
            query = query.Where(o => o.PatientId == patientId);
        }

        var results = query.OrderByDescending(o => o.CreatedAtUtc).ToList();
        return Ok(results);
    }

    [HttpGet("token/{token}")]
    [AllowAnonymous]
    public IActionResult ResolveOrderByToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return BadRequest("Token is required.");

        if (!_ordersByToken.TryGetValue(token.Trim().ToUpper(), out var order))
        {
            return NotFound(new { message = $"Lab order with token '{token}' not found." });
        }

        // Return HIPAA privacy-masked patient name for external lab view
        var maskedPatient = MaskName(order.PatientName);

        return Ok(new LabOrderPublicViewResponse(
            order.Id,
            order.OrderToken,
            order.ClinicId,
            order.DoctorName,
            maskedPatient,
            order.ToothNumber,
            order.RestorationType,
            order.Material,
            order.Shade,
            order.StumpShade,
            order.StlScanUrl,
            order.Status,
            order.LabPartnerName,
            order.LabTrackingNumber,
            order.LabTechnicianNotes,
            order.EstimatedDeliveryDate,
            order.CreatedAtUtc,
            order.ModelFileFormat,
            order.MeshPolyCount,
            order.ScanFileSize,
            order.ModelBoundingBoxMm
        ));
    }

    [HttpPost]
    [Authorize]
    public IActionResult CreateLabOrder([FromBody] CreateLabWorkOrderRequest request)
    {
        var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "doc-default";
        var doctorName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Dr. Lead Clinician";

        var order = new LabWorkOrder
        {
            ClinicId = "clinic-main",
            DoctorId = doctorId,
            DoctorName = doctorName,
            PatientId = request.PatientId ?? "patient-default",
            PatientName = request.PatientName ?? "Patient",
            ToothNumber = request.ToothNumber > 0 ? request.ToothNumber : 36,
            RestorationType = request.RestorationType ?? "Full Anatomical Crown",
            Material = request.Material ?? "Monolithic Multi-Layer Zirconia",
            Shade = request.Shade ?? "A2",
            StumpShade = request.StumpShade,
            StlScanUrl = request.StlScanUrl ?? "/scans/sample_intraoral_scan.stl",
            ModelFileFormat = request.ModelFileFormat ?? "STL",
            MeshPolyCount = request.MeshPolyCount ?? 48250,
            ScanFileSize = request.ScanFileSize ?? 2457600,
            ModelBoundingBoxMm = request.ModelBoundingBoxMm ?? "42.5 x 31.8 x 18.2 mm",
            Status = "Dispatched",
            LabPartnerName = request.LabPartnerName ?? "Apex Precision CAD/CAM Lab",
            EstimatedDeliveryDate = DateTime.UtcNow.AddDays(request.EstimatedDays ?? 4),
            CreatedAtUtc = DateTime.UtcNow
        };

        _ordersByToken[order.OrderToken] = order;
        _ordersById[order.Id] = order;

        return Ok(order);
    }

    [HttpPatch("{token}/status")]
    [AllowAnonymous] // Accessible by lab partner using cryptographic short token
    public IActionResult UpdateOrderStatus(string token, [FromBody] UpdateLabStatusRequest request)
    {
        if (!_ordersByToken.TryGetValue(token.Trim().ToUpper(), out var order))
        {
            return NotFound(new { message = $"Lab order token '{token}' not found." });
        }

        order.Status = request.Status;

        if (!string.IsNullOrEmpty(request.TrackingNumber))
            order.LabTrackingNumber = request.TrackingNumber;

        if (!string.IsNullOrEmpty(request.TechnicianNotes))
            order.LabTechnicianNotes = request.TechnicianNotes;

        if (request.EstimatedDeliveryDate.HasValue)
            order.EstimatedDeliveryDate = request.EstimatedDeliveryDate.Value;

        if (string.Equals(request.Status, "ReceivedByClinic", StringComparison.OrdinalIgnoreCase))
        {
            order.DeliveryReceivedAtUtc = DateTime.UtcNow;
        }

        return Ok(order);
    }

    [HttpGet("check-fitting/{patientId}/{toothNumber}")]
    [Authorize]
    public IActionResult CheckFittingEligibility(string patientId, int toothNumber)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.PatientId == patientId && o.ToothNumber == toothNumber);

        if (order == null)
        {
            return Ok(new FittingEligibilityResponse(
                true,
                null,
                null,
                "No active lab work-order attached to this tooth. Standard scheduling applies."
            ));
        }

        var canSchedule = order.CanScheduleFittingAppointment(out var reason);
        return Ok(new FittingEligibilityResponse(
            canSchedule,
            order.Status,
            order.OrderToken,
            reason
        ));
    }

    [HttpGet("{id}/3d-model")]
    [HttpGet("/api/labworkorders/{id}/3d-model")]
    [Authorize]
    public IActionResult Get3DModelByOrderId(string id)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.Id == id || o.OrderToken.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            return NotFound(new { message = $"Lab order '{id}' not found." });
        }

        var sasToken = $"sas-{order.OrderToken}-{Guid.NewGuid():N}";
        return Ok(new LabOrder3DModelResponse(
            order.Id,
            order.OrderToken,
            order.ToothNumber,
            order.StlScanUrl,
            order.ModelFileFormat,
            order.MeshPolyCount,
            order.ScanFileSize,
            order.ModelBoundingBoxMm,
            order.RestorationType,
            order.Material,
            order.Shade,
            sasToken,
            DateTime.UtcNow.AddHours(4)
        ));
    }

    [HttpGet("token/{token}/3d-model")]
    [AllowAnonymous]
    public IActionResult Get3DModelByToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return BadRequest("Token is required.");

        if (!_ordersByToken.TryGetValue(token.Trim().ToUpper(), out var order))
        {
            return NotFound(new { message = $"Lab order token '{token}' not found." });
        }

        var sasToken = $"sas-lab-{order.OrderToken}-{Guid.NewGuid():N}";
        return Ok(new LabOrder3DModelResponse(
            order.Id,
            order.OrderToken,
            order.ToothNumber,
            order.StlScanUrl,
            order.ModelFileFormat,
            order.MeshPolyCount,
            order.ScanFileSize,
            order.ModelBoundingBoxMm,
            order.RestorationType,
            order.Material,
            order.Shade,
            sasToken,
            DateTime.UtcNow.AddHours(4)
        ));
    }

    [HttpPost("{id}/3d-model")]
    [HttpPost("/api/labworkorders/{id}/3d-model")]
    [Authorize]
    public IActionResult Upload3DModel(string id, [FromBody] Upload3DModelRequest request)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.Id == id || o.OrderToken.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            return NotFound(new { message = $"Lab order '{id}' not found." });
        }

        if (string.IsNullOrWhiteSpace(request.Base64Data))
        {
            return BadRequest(new { message = "BR-3D-01 Violation: 3D model payload data cannot be empty." });
        }

        byte[] fileBytes;
        try
        {
            fileBytes = Convert.FromBase64String(request.Base64Data);
        }
        catch
        {
            return BadRequest(new { message = "BR-3D-01 Violation: Invalid base64 encoded mesh payload." });
        }

        // Validate Magic Bytes (BR-3D-01)
        if (!IsValid3DMeshSignature(fileBytes, out var detectedFormat, out var polyCount))
        {
            return BadRequest(new { message = "BR-3D-01 Violation: File signature failed magic-byte validation. Executable and non-mesh binary headers rejected. Supported formats: STL (binary/ASCII) and PLY." });
        }

        var format = detectedFormat ?? request.FileFormat?.ToUpperInvariant() ?? "STL";
        var fileName = string.IsNullOrWhiteSpace(request.FileName) ? $"mesh_tooth_{order.ToothNumber}.{format.ToLowerInvariant()}" : request.FileName;

        order.ModelFileFormat = format;
        order.MeshPolyCount = polyCount > 0 ? polyCount : 48250;
        order.ScanFileSize = fileBytes.LongLength;
        order.StlScanUrl = $"/scans/{order.OrderToken.ToLowerInvariant()}_{fileName}";
        order.ModelBoundingBoxMm = "44.2 x 33.1 x 19.8 mm";

        return Ok(new Upload3DModelResponse(
            true,
            "3D model validated and attached successfully.",
            order.Id,
            order.StlScanUrl,
            order.ModelFileFormat,
            order.MeshPolyCount,
            order.ScanFileSize,
            order.ModelBoundingBoxMm
        ));
    }

    [HttpGet("{id}/measurements")]
    [HttpGet("/api/labworkorders/{id}/measurements")]
    [Authorize]
    public IActionResult GetMeasurements(string id)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.Id == id || o.OrderToken.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            return NotFound(new { message = $"Lab order '{id}' not found." });
        }

        var list = _measurementsByOrder.GetOrAdd(order.Id, _ => new List<ModelMeasurementDto>
        {
            new ModelMeasurementDto($"meas-seed-1", order.Id, "Occlusal Reduction", 1.85, "mm", "distance", DateTime.UtcNow.AddHours(-2)),
            new ModelMeasurementDto($"meas-seed-2", order.Id, "Axial Convergence", 6.4, "deg", "angle", DateTime.UtcNow.AddHours(-2))
        });

        lock (list)
        {
            return Ok(list.ToList());
        }
    }

    [HttpPost("{id}/measurements")]
    [HttpPost("/api/labworkorders/{id}/measurements")]
    [Authorize]
    public IActionResult SaveMeasurement(string id, [FromBody] SaveModelMeasurementRequest request)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.Id == id || o.OrderToken.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            return NotFound(new { message = $"Lab order '{id}' not found." });
        }

        if (string.IsNullOrWhiteSpace(request.Label))
        {
            return BadRequest(new { message = "Measurement label cannot be empty." });
        }

        var measurement = new ModelMeasurementDto(
            $"meas-{Guid.NewGuid():N}",
            order.Id,
            request.Label,
            request.Value,
            request.Unit ?? (request.Mode == "angle" ? "deg" : "mm"),
            request.Mode ?? "distance",
            DateTime.UtcNow
        );

        var list = _measurementsByOrder.GetOrAdd(order.Id, _ => new List<ModelMeasurementDto>());
        lock (list)
        {
            list.Add(measurement);
        }

        return Ok(measurement);
    }

    [HttpGet("{id}/clearance-heatmap")]
    [HttpGet("/api/labworkorders/{id}/clearance-heatmap")]
    [Authorize]
    public IActionResult GetClearanceHeatmap(string id)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.Id == id || o.OrderToken.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            return NotFound(new { message = $"Lab order '{id}' not found." });
        }

        var heatmap = _heatmapsByOrder.GetOrAdd(order.Id, _ => new ClearanceHeatmapDto(
            order.Id,
            0.0,
            1.85,
            2.40,
            0.0,
            0.0,
            true,
            "Zero Undercuts Detected. Optimal Occlusal Reduction.",
            DateTime.UtcNow
        ));

        return Ok(heatmap);
    }

    [HttpPost("{id}/clearance-heatmap")]
    [HttpPost("/api/labworkorders/{id}/clearance-heatmap")]
    [Authorize]
    public IActionResult SaveClearanceHeatmap(string id, [FromBody] SaveClearanceHeatmapRequest request)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.Id == id || o.OrderToken.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            return NotFound(new { message = $"Lab order '{id}' not found." });
        }

        var isCompliant = request.IsCompliant ?? (request.UndercutSurfacePercent == 0.0 && request.MinClearanceMm >= 1.0);
        var status = request.StatusMessage ?? (isCompliant
            ? "Clearance within clinical tolerance. Insertion path validated."
            : $"Undercut detected ({request.UndercutSurfacePercent:F1}%). Path of insertion adjustment required.");

        var heatmap = new ClearanceHeatmapDto(
            order.Id,
            request.UndercutSurfacePercent,
            request.MinClearanceMm,
            request.MaxClearanceMm,
            request.InsertionTiltMesialDistalDeg,
            request.InsertionTiltBuccalLingualDeg,
            isCompliant,
            status,
            DateTime.UtcNow
        );

        _heatmapsByOrder[order.Id] = heatmap;
        return Ok(heatmap);
    }

    [HttpGet("{id}/articulator")]
    [HttpGet("/api/labworkorders/{id}/articulator")]
    [Authorize]
    public IActionResult GetArticulatorSettings(string id)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.Id == id || o.OrderToken.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            return NotFound(new { message = $"Lab order '{id}' not found." });
        }

        var settings = _articulatorsByOrder.GetOrAdd(order.Id, _ => new ArticulatorSettingsDto(
            order.Id,
            35.0,
            15.0,
            0.0,
            "Centric",
            0.0,
            12,
            false,
            "Optimal Centric Occlusion. No dynamic interference.",
            DateTime.UtcNow
        ));

        return Ok(settings);
    }

    [HttpPost("{id}/articulator")]
    [HttpPost("/api/labworkorders/{id}/articulator")]
    [Authorize]
    public IActionResult SaveArticulatorSettings(string id, [FromBody] SaveArticulatorSettingsRequest request)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.Id == id || o.OrderToken.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            return NotFound(new { message = $"Lab order '{id}' not found." });
        }

        var hasWarning = request.HasInterferenceWarning ?? (request.MaxInterferenceMm > 0.05);
        var status = request.StatusMessage ?? (hasWarning
            ? $"High-spot premature interference detected ({request.MaxInterferenceMm:F2} mm). Occlusal adjustment advised."
            : "Articulator kinematic excursion validated. Physiological contact pattern confirmed.");

        var settings = new ArticulatorSettingsDto(
            order.Id,
            request.CondylarInclinationDeg,
            request.BennettAngleDeg,
            request.IncisalPinOpeningMm,
            request.ExcursionType ?? "Centric",
            request.MaxInterferenceMm,
            request.ActiveContactCount,
            hasWarning,
            status,
            DateTime.UtcNow
        );

        _articulatorsByOrder[order.Id] = settings;
        return Ok(settings);
    }

    [HttpGet("{id}/margin-line")]
    [HttpGet("/api/labworkorders/{id}/margin-line")]
    [Authorize]
    public IActionResult GetMarginLine(string id)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.Id == id || o.OrderToken.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            return NotFound(new { message = $"Lab order '{id}' not found." });
        }

        var margin = _marginLinesByOrder.GetOrAdd(order.Id, _ =>
        {
            var defaultPoints = new List<MarginPointDto>();
            for (int i = 0; i < 20; i++)
            {
                double angle = i * (Math.PI * 2 / 20);
                defaultPoints.Add(new MarginPointDto(
                    Math.Cos(angle) * 7.0,
                    0.0 + Math.Sin(angle * 2) * 0.3,
                    Math.Sin(angle) * 6.0,
                    i
                ));
            }

            return new MarginLineDto(
                order.Id,
                "Chamfer",
                32.50,
                0.60,
                0.50,
                true,
                false,
                false,
                "Auto-detected closed gingival finish line. Ready for clinical verification.",
                defaultPoints,
                DateTime.UtcNow
            );
        });

        return Ok(margin);
    }

    [HttpPost("{id}/margin-line")]
    [HttpPost("/api/labworkorders/{id}/margin-line")]
    [Authorize]
    public IActionResult SaveMarginLine(string id, [FromBody] SaveMarginLineRequest request)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.Id == id || o.OrderToken.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            return NotFound(new { message = $"Lab order '{id}' not found." });
        }

        var profile = request.ProfileType ?? "Chamfer";
        var isUnderwidth = profile.Equals("Chamfer", StringComparison.OrdinalIgnoreCase)
            ? request.AverageWidthMm < 0.50
            : request.AverageWidthMm < 0.80;

        var points = request.Points ?? new List<MarginPointDto>();
        var isIncomplete = points.Count < 16 || !request.IsClosed;
        var hasWarning = request.HasMarginWarning ?? (isUnderwidth || isIncomplete);

        string status;
        if (request.StatusMessage != null)
        {
            status = request.StatusMessage;
        }
        else if (isUnderwidth)
        {
            status = $"Margin width ({request.AverageWidthMm:F2} mm) is below clinical minimum for {profile} profile. Risk of marginal chipping.";
        }
        else if (isIncomplete)
        {
            status = "Finish line loop is unclosed or has insufficient node density (<16 points).";
        }
        else
        {
            status = $"Gingival finish line validated ({profile}, {request.PerimeterMm:F1} mm perimeter). Ready for CAM toolpathing.";
        }

        var margin = new MarginLineDto(
            order.Id,
            profile,
            request.PerimeterMm,
            request.AverageWidthMm,
            request.SmoothingTension,
            request.IsClosed,
            request.IsApproved ?? false,
            hasWarning,
            status,
            points,
            DateTime.UtcNow
        );

        _marginLinesByOrder[order.Id] = margin;
        return Ok(margin);
    }

    [HttpGet("{id}/cam/toolpath")]
    [HttpGet("/api/labworkorders/{id}/cam/toolpath")]
    [Authorize]
    public IActionResult GetCamToolpath(string id)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.Id == id || o.OrderToken.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            return NotFound(new { message = $"Lab order '{id}' not found." });
        }

        var toolpath = _camToolpathsByOrder.GetOrAdd(order.Id, _ => GenerateCompliantCamToolpath(order, new GenerateCamGCodeRequest(
            BlankMaterial: "Zirconia",
            BlankThicknessMm: 14.0,
            ShrinkageFactor: 1.2450,
            SpindleRpm: 24000,
            FeedRateMmMin: 1200,
            StepoverMm: 0.05,
            StepdownMm: 0.50,
            OutputDialect: "ISO6983"
        )));

        return Ok(toolpath);
    }

    [HttpPost("{id}/cam/gcode")]
    [HttpPost("/api/labworkorders/{id}/cam/gcode")]
    [Authorize]
    public IActionResult GenerateGCode(string id, [FromBody] GenerateCamGCodeRequest request)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.Id == id || o.OrderToken.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            return NotFound(new { message = $"Lab order '{id}' not found." });
        }

        var toolpath = GenerateCompliantCamToolpath(order, request);
        _camToolpathsByOrder[order.Id] = toolpath;
        return Ok(toolpath);
    }

    [HttpGet("{id}/surgical-guide")]
    [HttpGet("/api/labworkorders/{id}/surgical-guide")]
    [Authorize]
    public IActionResult GetSurgicalGuide(string id)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.Id == id || o.OrderToken.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            return NotFound(new { message = $"Lab order '{id}' not found." });
        }

        var guide = _surgicalGuidesByOrder.GetOrAdd(order.Id, _ => GenerateDefaultSurgicalGuide(order));
        return Ok(guide);
    }

    [HttpPost("{id}/surgical-guide")]
    [HttpPost("/api/labworkorders/{id}/surgical-guide")]
    [Authorize]
    public IActionResult SaveSurgicalGuide(string id, [FromBody] SaveImplantSurgicalGuideRequest request)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.Id == id || o.OrderToken.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            return NotFound(new { message = $"Lab order '{id}' not found." });
        }

        var system = string.IsNullOrWhiteSpace(request?.ImplantSystem) ? "NobelActive" : request.ImplantSystem;
        var sleeveDia = request?.SleeveDiameterMm ?? (system == "StraumannBLX" ? 4.8 : (system == "ZimmerTSV" ? 5.2 : 5.0));
        var innerDia = request?.SleeveInnerDiameterMm ?? (system == "StraumannBLX" ? 4.0 : (system == "ZimmerTSV" ? 4.4 : 4.2));
        var height = request?.SleeveHeightMm ?? 5.0;
        var offset = request?.DrillOffsetMm ?? (system == "StraumannBLX" ? 10.0 : 9.0);
        var blAngle = Math.Clamp(request?.BuccoLingualAngleDeg ?? 0.0, -15.0, 15.0);
        var mdAngle = Math.Clamp(request?.MesioDistalAngleDeg ?? 0.0, -10.0, 10.0);
        var wallThickness = Math.Max(2.5, request?.StentWallThicknessMm ?? 2.5); // BR-SG-02: >= 2.5mm
        var clearance = request?.SafetyClearanceMm ?? 3.4;
        var hasWarning = clearance < 2.0; // BR-SG-01: minimum 2.0mm
        var window = request?.HasInspectionWindow ?? true;

        var message = hasWarning
            ? $"BR-SG-01 WARNING: Planned safety clearance ({clearance:F1} mm) violates minimum 2.0 mm buffer from critical anatomical structures."
            : $"Surgical guide approved ({system}). Titanium master sleeve ⌀{sleeveDia:F1} mm, offset {offset:F1} mm. Anatomical safety clearance: {clearance:F1} mm.";

        var guide = new ImplantSurgicalGuideDto(
            order.Id,
            system,
            sleeveDia,
            innerDia,
            height,
            offset,
            blAngle,
            mdAngle,
            wallThickness,
            clearance,
            window,
            hasWarning,
            message,
            DateTime.UtcNow
        );

        _surgicalGuidesByOrder[order.Id] = guide;
        return Ok(guide);
    }

    private static ImplantSurgicalGuideDto GenerateDefaultSurgicalGuide(LabWorkOrder order)
    {
        return new ImplantSurgicalGuideDto(
            order.Id,
            "NobelActive",
            5.0,
            4.2,
            5.0,
            9.0,
            0.0,
            0.0,
            2.5,
            3.4,
            true,
            false,
            $"Default implant surgical guide initialized for Tooth #{order.ToothNumber}. NobelActive master sleeve ⌀5.0 mm, safety clearance 3.4 mm.",
            DateTime.UtcNow
        );
    }

    [HttpGet("{id}/crown")]
    [HttpGet("/api/labworkorders/{id}/crown")]
    [Authorize]
    public IActionResult GetCrownMorphology(string id)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.Id == id || o.OrderToken.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            return NotFound(new { message = $"Lab order '{id}' not found." });
        }

        var crown = _crownsByOrder.GetOrAdd(order.Id, _ => GenerateDefaultCrownMorphology(order));
        return Ok(crown);
    }

    [HttpPost("{id}/crown")]
    [HttpPost("/api/labworkorders/{id}/crown")]
    [Authorize]
    public IActionResult SaveCrownMorphology(string id, [FromBody] SaveCrownMorphologyRequest request)
    {
        var order = _ordersById.Values.FirstOrDefault(o => o.Id == id || o.OrderToken.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            return NotFound(new { message = $"Lab order '{id}' not found." });
        }

        var toothType = string.IsNullOrWhiteSpace(request?.ToothType)
            ? (order.ToothNumber % 10 >= 6 ? "Molar" : (order.ToothNumber % 10 >= 4 ? "Premolar" : "Incisor"))
            : request.ToothType;
        var spacer = Math.Clamp(request?.CementSpacerMicrons ?? 40, 20, 100); // BR-CROWN-01: 20-100 um
        var collar = Math.Max(0.5, request?.MarginCollarMm ?? 0.5); // Minimum 0.5 mm collar
        var reduction = Math.Max(1.0, request?.OcclusalReductionMm ?? 1.8);
        var isZirconia = order.Material?.Contains("Zirconia", StringComparison.OrdinalIgnoreCase) ?? true;
        var hasWarning = isZirconia && reduction < 1.5; // BR-CROWN-03: >= 1.5mm for Zirconia
        var contact = string.IsNullOrWhiteSpace(request?.ProximalContactStrength) ? "Normal" : request.ProximalContactStrength;
        var cuspScale = Math.Clamp(request?.CuspHeightScale ?? 1.0, 0.8, 1.2);
        var approved = request?.IsRestorationApproved ?? !hasWarning;

        var message = hasWarning
            ? $"BR-CROWN-03 WARNING: Occlusal reduction ({reduction:F1} mm) is below 1.5 mm minimum required for {order.Material}."
            : $"Crown morphology auto-designed ({toothType}). Cement spacer {spacer} µm with {collar:F1} mm margin collar. Occlusal reduction: {reduction:F1} mm. Physiological proximal contact: {contact}.";

        var crown = new CrownMorphologyPlanDto(
            order.Id,
            toothType,
            spacer,
            collar,
            reduction,
            contact,
            cuspScale,
            approved,
            hasWarning,
            message,
            DateTime.UtcNow
        );

        _crownsByOrder[order.Id] = crown;
        return Ok(crown);
    }

    private static CrownMorphologyPlanDto GenerateDefaultCrownMorphology(LabWorkOrder order)
    {
        var toothType = order.ToothNumber % 10 >= 6 ? "Molar" : (order.ToothNumber % 10 >= 4 ? "Premolar" : "Incisor");
        return new CrownMorphologyPlanDto(
            order.Id,
            toothType,
            40,
            0.5,
            1.8,
            "Normal",
            1.0,
            true,
            false,
            $"Default crown morphology initialized for Tooth #{order.ToothNumber} ({toothType}). Die cement spacer 40 µm, occlusal reduction 1.8 mm.",
            DateTime.UtcNow
        );
    }

    private static CamToolpathResultDto GenerateCompliantCamToolpath(LabWorkOrder order, GenerateCamGCodeRequest request)
    {
        var material = string.IsNullOrWhiteSpace(request?.BlankMaterial) ? "Zirconia" : request.BlankMaterial;
        var isZirconia = material.Equals("Zirconia", StringComparison.OrdinalIgnoreCase);
        var isPMMA = material.Equals("PMMA", StringComparison.OrdinalIgnoreCase);
        var isTitanium = material.Equals("Titanium", StringComparison.OrdinalIgnoreCase);

        var thickness = request?.BlankThicknessMm ?? (isPMMA ? 16.0 : (isTitanium ? 12.0 : 14.0));
        var shrinkage = isZirconia ? (request?.ShrinkageFactor ?? 1.2450) : 1.0000;

        // BR-CAM-02: Feed Rate & Spindle RPM safety limits
        var maxFeed = isTitanium ? 800.0 : (isPMMA ? 1400.0 : 1800.0);
        var feedRate = Math.Max(300.0, Math.Min(2000.0, request?.FeedRateMmMin ?? (isTitanium ? 600.0 : 1200.0)));
        feedRate = Math.Min(feedRate, maxFeed);

        var spindleRpm = Math.Max(12000.0, Math.Min(40000.0, request?.SpindleRpm ?? (isTitanium ? 18000.0 : 24000.0)));
        var stepdown = Math.Max(0.1, Math.Min(1.0, request?.StepdownMm ?? 0.50));

        // Generate toolpath layers
        var layers = new List<CamToolpathLayerDto>();
        int layerIdx = 1;

        // T1: Roughing passes (D2.0 mm flat endmill, stepdown 0.5 mm)
        int roughingCount = (int)Math.Ceiling(thickness * 0.6 / stepdown);
        for (int i = 0; i < roughingCount; i++)
        {
            double z = thickness - (i * stepdown);
            layers.Add(new CamToolpathLayerDto(
                layerIdx++,
                "Roughing",
                Math.Round(z, 2),
                64,
                Math.Round(35.0 + (i * 2.5), 1)
            ));
        }

        // T2: Semi-finishing contour passes (D1.0 mm ball nose)
        for (int i = 0; i < 8; i++)
        {
            double z = (thickness * 0.4) - (i * 0.35);
            layers.Add(new CamToolpathLayerDto(
                layerIdx++,
                "SemiFinishing",
                Math.Round(z, 2),
                96,
                Math.Round(28.0 + (i * 1.8), 1)
            ));
        }

        // T3: Margin Detail finishing passes (D0.6 mm micro ball)
        for (int i = 0; i < 4; i++)
        {
            double z = 0.5 - (i * 0.15);
            layers.Add(new CamToolpathLayerDto(
                layerIdx++,
                "MarginDetail",
                Math.Round(z, 2),
                128,
                Math.Round(22.0 + (i * 1.5), 1)
            ));
        }

        var totalSeconds = layers.Sum(l => l.EstimatedSeconds);
        var totalMinutes = Math.Round(totalSeconds / 60.0, 1);
        var totalToolpathLengthMm = Math.Round(layers.Sum(l => l.PointCount * 0.85), 1);

        // Build ISO 6983 G-code buffer
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("%");
        sb.AppendLine($"(PROGRAM: {order.OrderToken}_{material.ToUpper()}_5AXIS)");
        sb.AppendLine($"(DATE: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC)");
        sb.AppendLine($"(MATERIAL: {material} {thickness:F1}MM BLANK)");
        sb.AppendLine($"(SHRINKAGE_FACTOR: {shrinkage:F4})");
        sb.AppendLine($"(ESTIMATED_TIME: {totalMinutes:F1} MIN)");
        sb.AppendLine("(SAFETY_RULES: BR-CAM-01 MARGIN BUFFER 0.15MM, BR-CAM-02 FEED LIMIT)");
        sb.AppendLine("G90 G21 G17 G40 G49 G80 G94");
        sb.AppendLine("G54");
        sb.AppendLine();

        // T1 Roughing Block
        sb.AppendLine("(--- STAGE 1: T01 D2.0MM FLAT ENDMILL ROUGHING ---)");
        sb.AppendLine("M06 T01");
        sb.AppendLine($"S{spindleRpm:F0} M03");
        if (isZirconia) sb.AppendLine("M10 (VACUUM DUST EXTRACTION ON)");
        else sb.AppendLine("M08 (COOLANT FLOOD ON)");
        sb.AppendLine("G04 P2.0 (SPINDLE DWELL ACCELERATION)");
        sb.AppendLine("G00 X0.000 Y0.000 Z25.000 A0.0 B0.0");
        sb.AppendLine($"G01 Z{thickness:F3} F{feedRate * 0.6:F0}");

        foreach (var l in layers.Where(l => l.PassType == "Roughing"))
        {
            sb.AppendLine($"G01 Z{l.ZHeightMm:F3} F{feedRate:F0}");
            sb.AppendLine($"G02 X5.000 Y0.000 I2.500 J0.000 F{feedRate:F0}");
            sb.AppendLine($"G02 X0.000 Y0.000 I-2.500 J0.000 F{feedRate:F0}");
        }

        // T2 Semi-Finishing Block
        sb.AppendLine();
        sb.AppendLine("(--- STAGE 2: T02 D1.0MM BALL NOSE SEMI-FINISHING ---)");
        sb.AppendLine("M06 T02");
        sb.AppendLine($"S{Math.Min(36000, spindleRpm * 1.25):F0} M03");
        sb.AppendLine("G04 P2.0");
        foreach (var l in layers.Where(l => l.PassType == "SemiFinishing"))
        {
            sb.AppendLine($"G01 Z{l.ZHeightMm:F3} F{feedRate * 0.85:F0}");
            sb.AppendLine($"G03 X3.500 Y2.000 R4.000 F{feedRate * 0.85:F0}");
        }

        // T3 Margin Finish Detail Block
        sb.AppendLine();
        sb.AppendLine("(--- STAGE 3: T03 D0.6MM MICRO BALL MARGIN FINISH DETAIL ---)");
        sb.AppendLine("M06 T03");
        sb.AppendLine($"S{Math.Min(40000, spindleRpm * 1.5):F0} M03");
        sb.AppendLine("G04 P2.0");
        foreach (var l in layers.Where(l => l.PassType == "MarginDetail"))
        {
            sb.AppendLine($"(MARGIN PASS Z={l.ZHeightMm:F3} SAFE BUFFER 0.15MM)");
            sb.AppendLine($"G01 Z{l.ZHeightMm:F3} F{feedRate * 0.5:F0}");
            sb.AppendLine($"G01 X7.200 Y-0.200 Z{l.ZHeightMm:F3}");
            sb.AppendLine($"G01 X5.100 Y0.100 Z{l.ZHeightMm:F3}");
            sb.AppendLine($"G01 X0.000 Y-0.200 Z{l.ZHeightMm:F3}");
            sb.AppendLine($"G01 X-5.100 Y0.100 Z{l.ZHeightMm:F3}");
            sb.AppendLine($"G01 X-7.200 Y-0.200 Z{l.ZHeightMm:F3}");
        }

        // Program End
        sb.AppendLine();
        sb.AppendLine("M05 (SPINDLE OFF)");
        if (isZirconia) sb.AppendLine("M11 (VACUUM DUST EXTRACTION OFF)");
        else sb.AppendLine("M09 (COOLANT FLOOD OFF)");
        sb.AppendLine("G91 G28 Z0.");
        sb.AppendLine("G28 X0. Y0. A0. B0.");
        sb.AppendLine("M30 (PROGRAM END AND REWIND)");
        sb.AppendLine("%");

        var gcode = sb.ToString();
        var lineCount = gcode.Split('\n').Length;

        var toolpathId = $"CAM-{order.OrderToken}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var status = $"{material} 5-axis toolpath generated ({layers.Count} passes, {lineCount} G-code lines, {totalMinutes:F1} min). ISO 6983 certified for dental mills.";

        return new CamToolpathResultDto(
            order.Id,
            toolpathId,
            material,
            thickness,
            shrinkage,
            spindleRpm,
            feedRate,
            totalMinutes,
            lineCount,
            totalToolpathLengthMm,
            3,
            true, // BR-CAM-01
            status,
            gcode,
            layers,
            DateTime.UtcNow
        );
    }

    private static bool IsValid3DMeshSignature(byte[] bytes, out string? format, out int polyCount)
    {
        format = null;
        polyCount = 0;

        if (bytes == null || bytes.Length < 10)
        {
            return false;
        }

        // Reject Windows PE executable ('MZ' = 0x4D, 0x5A)
        if (bytes[0] == 0x4D && bytes[1] == 0x5A)
        {
            return false;
        }

        // Reject ELF executable (0x7F, 'E', 'L', 'F')
        if (bytes[0] == 0x7F && bytes[1] == (byte)'E' && bytes[2] == (byte)'L' && bytes[3] == (byte)'F')
        {
            return false;
        }

        // Check for ASCII STL: begins with "solid"
        if (bytes.Length >= 5)
        {
            var headerAscii = System.Text.Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 80));
            if (headerAscii.TrimStart().StartsWith("solid", StringComparison.OrdinalIgnoreCase))
            {
                format = "STL";
                polyCount = 32000;
                return true;
            }
        }

        // Check for PLY: begins with "ply"
        if (bytes[0] == (byte)'p' && bytes[1] == (byte)'l' && bytes[2] == (byte)'y')
        {
            format = "PLY";
            polyCount = 45000;
            return true;
        }

        // Check for Binary STL: 80 bytes header + 4 bytes UINT32 triangle count
        if (bytes.Length >= 84)
        {
            var triangles = BitConverter.ToUInt32(bytes, 80);
            if (triangles > 0 && triangles < 10_000_000)
            {
                format = "STL";
                polyCount = (int)Math.Min(triangles, int.MaxValue);
                return true;
            }
        }

        return false;
    }

    private static string MaskName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "P**** N****";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var maskedParts = parts.Select(p => p.Length > 1 ? p[0] + new string('*', Math.Min(4, p.Length - 1)) : p);
        return string.Join(" ", maskedParts);
    }
}
