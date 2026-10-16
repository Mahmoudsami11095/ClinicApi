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
