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
        int? EstimatedDays
    );

    public record UpdateLabStatusRequest(
        string Status,
        string? TrackingNumber,
        string? TechnicianNotes,
        DateTime? EstimatedDeliveryDate
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

        return Ok(new
        {
            order.Id,
            order.OrderToken,
            order.ClinicId,
            order.DoctorName,
            PatientName = maskedPatient,
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
            order.CreatedAtUtc
        });
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
            return Ok(new
            {
                canSchedule = true,
                reason = "No active lab work-order attached to this tooth. Standard scheduling applies."
            });
        }

        var canSchedule = order.CanScheduleFittingAppointment(out var reason);
        return Ok(new
        {
            canSchedule,
            orderStatus = order.Status,
            orderToken = order.OrderToken,
            reason
        });
    }

    private static string MaskName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "P**** N****";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var maskedParts = parts.Select(p => p.Length > 1 ? p[0] + new string('*', Math.Min(4, p.Length - 1)) : p);
        return string.Join(" ", maskedParts);
    }
}
