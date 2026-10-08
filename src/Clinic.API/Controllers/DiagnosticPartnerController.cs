using System.Text.Json;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/public/diagnostics")]
public class DiagnosticPartnerController : ControllerBase
{
    private readonly ClinicDbContext _context;
    private readonly IDiagnosticRequisitionRepository _orderRepo;
    private readonly INotificationService _notificationService;
    private readonly IWebHostEnvironment _env;

    public DiagnosticPartnerController(
        ClinicDbContext context,
        IDiagnosticRequisitionRepository orderRepo,
        INotificationService notificationService,
        IWebHostEnvironment env)
    {
        _context = context;
        _orderRepo = orderRepo;
        _notificationService = notificationService;
        _env = env;
    }

    // ── Staff Endpoint: Create Diagnostic Requisition (Doctor / Assistant) ──
    [HttpPost("orders")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateDiagnosticRequisitionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ClinicId) || 
            string.IsNullOrWhiteSpace(dto.DoctorId) || 
            string.IsNullOrWhiteSpace(dto.PatientId))
        {
            return BadRequest(new { message = "ClinicId, DoctorId, and PatientId are required." });
        }

        var clinic = await _context.Clinics.FindAsync(dto.ClinicId);
        if (clinic == null) return NotFound(new { message = "Clinic not found" });

        var doctor = await _context.Doctors.FindAsync(dto.DoctorId);
        if (doctor == null) return NotFound(new { message = "Doctor not found" });

        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null) return NotFound(new { message = "Patient not found" });

        // Generate cryptographic short token: "ORD-" + 6 uppercase hex chars
        var token = "ORD-" + Guid.NewGuid().ToString("N")[..6].ToUpper();

        var order = new DiagnosticRequisitionOrder
        {
            Id = Guid.NewGuid().ToString(),
            RequisitionToken = token,
            ClinicId = dto.ClinicId,
            DoctorId = dto.DoctorId,
            PatientId = dto.PatientId,
            ToothNumber = dto.ToothNumber,
            ServiceType = string.IsNullOrWhiteSpace(dto.ServiceType) ? "DentalLab" : dto.ServiceType,
            Indications = dto.Indications,
            PartnerName = dto.PartnerName,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        await _orderRepo.AddAsync(order);

        var dropzoneUrl = $"https://clinic-app-ten-topaz.vercel.app/partner-dropzone?order={token}";

        return Ok(new
        {
            message = "Diagnostic requisition order created successfully.",
            data = new
            {
                orderId = order.Id,
                token = order.RequisitionToken,
                dropzoneUrl,
                serviceType = order.ServiceType,
                indications = order.Indications,
                status = order.Status
            }
        });
    }

    // ── Staff Endpoint: List Requisitions for Clinic or Patient ──
    [HttpGet("orders/clinic/{clinicId}")]
    [Authorize(Roles = "admin,doctor,assistant")]
    public async Task<IActionResult> GetClinicOrders(string clinicId, [FromQuery] string? patientId)
    {
        var query = _context.DiagnosticRequisitions
            .Include(d => d.Doctor)
            .Include(d => d.Patient)
            .Where(d => d.ClinicId == clinicId && !d.IsDeleted);

        if (!string.IsNullOrEmpty(patientId))
        {
            query = query.Where(d => d.PatientId == patientId);
        }

        var orders = await query.OrderByDescending(d => d.CreatedAt).AsNoTracking().ToListAsync();

        var dtos = orders.Select(d => new DiagnosticRequisitionDto
        {
            Id = d.Id,
            RequisitionToken = d.RequisitionToken,
            ClinicId = d.ClinicId,
            ClinicName = d.Clinic?.Name ?? string.Empty,
            DoctorId = d.DoctorId,
            DoctorName = $"{d.Doctor?.FirstName} {d.Doctor?.LastName}".Trim(),
            PatientId = d.PatientId,
            PatientName = $"{d.Patient?.FirstName} {d.Patient?.LastName}".Trim(),
            PatientPhone = d.Patient?.PhoneNumber,
            ToothNumber = d.ToothNumber,
            ServiceType = d.ServiceType,
            Indications = d.Indications,
            Status = d.Status,
            PartnerName = d.PartnerName,
            PartnerNotes = d.PartnerNotes,
            TechnicianName = d.TechnicianName,
            CreatedAt = d.CreatedAt,
            FulfilledAt = d.FulfilledAt,
            ResultFileUrls = TryParseJsonList(d.ResultFileUrls)
        }).ToList();

        return Ok(new { data = dtos });
    }

    // ── Public Partner Endpoint: Resolve Order Metadata via Token ──
    [HttpGet("orders/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> ResolveOrder(string token)
    {
        var order = await _orderRepo.GetByTokenAsync(token);
        if (order == null)
        {
            return NotFound(new { message = "Diagnostic order not found or invalid token." });
        }

        var dto = new DiagnosticRequisitionDto
        {
            Id = order.Id,
            RequisitionToken = order.RequisitionToken,
            ClinicId = order.ClinicId,
            ClinicName = order.Clinic?.Name ?? "Dental Specialty Center",
            DoctorId = order.DoctorId,
            DoctorName = $"{order.Doctor?.FirstName} {order.Doctor?.LastName}".Trim(),
            PatientId = order.PatientId,
            PatientName = MaskPatientName(order.Patient?.FirstName, order.Patient?.LastName),
            ToothNumber = order.ToothNumber,
            ServiceType = order.ServiceType,
            Indications = order.Indications,
            Status = order.Status,
            PartnerName = order.PartnerName,
            PartnerNotes = order.PartnerNotes,
            TechnicianName = order.TechnicianName,
            CreatedAt = order.CreatedAt,
            FulfilledAt = order.FulfilledAt,
            ResultFileUrls = TryParseJsonList(order.ResultFileUrls)
        };

        return Ok(new { data = dto });
    }

    private static string MaskPatientName(string? first, string? last)
    {
        if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last))
            return "P**** N****";

        var f = (first ?? string.Empty).Trim();
        var l = (last ?? string.Empty).Trim();

        var maskedFirst = f.Length > 1 ? f[0] + new string('*', Math.Min(4, f.Length - 1)) : f;
        var maskedLast = l.Length > 1 ? l[0] + new string('*', Math.Min(4, l.Length - 1)) : l;

        return $"{maskedFirst} {maskedLast}".Trim();
    }

    // ── Public Partner Endpoint: Upload Diagnostic Results & Auto-Link to EMR ──
    [HttpPost("orders/{token}/upload")]
    [AllowAnonymous]
    public async Task<IActionResult> UploadOrderResults(
        string token,
        [FromForm] IFormFileCollection files,
        [FromForm] string? partnerNotes,
        [FromForm] string? technicianName)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new { message = "At least one diagnostic file must be attached." });
        }

        var order = await _orderRepo.GetByTokenAsync(token);
        if (order == null)
        {
            return NotFound(new { message = "Diagnostic order not found or invalid token." });
        }

        // Tri-Factor Automatic Ingestion (BR-LAB-02)
        // Store under wwwroot/uploads/patients/{patientId}/diagnostics/
        var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "patients", order.PatientId, "diagnostics");
        if (!Directory.Exists(uploadsRoot))
        {
            Directory.CreateDirectory(uploadsRoot);
        }

        var savedFileUrls = new List<string>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            var safeFileName = $"{order.RequisitionToken}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(file.FileName)}";
            var targetPath = Path.Combine(uploadsRoot, safeFileName);

            using (var stream = new FileStream(targetPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativeUrl = $"/uploads/patients/{order.PatientId}/diagnostics/{safeFileName}";
            savedFileUrls.Add(relativeUrl);

            // If ServiceType is Radiology, automatically index into RadiologyRecords!
            if (order.ServiceType.Equals("Radiology", StringComparison.OrdinalIgnoreCase))
            {
                var center = await _context.RadiologyCenters.FirstOrDefaultAsync();
                var centerId = center?.Id ?? "RC-DEFAULT";
                var radiologyRecord = new RadiologyRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    DoctorId = order.DoctorId,
                    PatientId = order.PatientId,
                    RadiologyCenterId = centerId,
                    ProcedureName = string.IsNullOrWhiteSpace(order.Indications) ? "Diagnostic Scan" : order.Indications,
                    Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    Notes = $"Diagnostic Partner Dropzone ({order.RequisitionToken}): {relativeUrl}"
                };
                await _context.RadiologyRecords.AddAsync(radiologyRecord);
            }
        }

        // Update Order
        var existingList = TryParseJsonList(order.ResultFileUrls);
        existingList.AddRange(savedFileUrls);

        order.ResultFileUrls = JsonSerializer.Serialize(existingList);
        order.Status = "ResultsReceived";
        order.FulfilledAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(partnerNotes)) order.PartnerNotes = partnerNotes;
        if (!string.IsNullOrWhiteSpace(technicianName)) order.TechnicianName = technicianName;

        await _orderRepo.UpdateAsync(order);
        await _context.SaveChangesAsync();

        // BR-LAB-03: Real-Time Notification to Attending Doctor
        try
        {
            var doctorUser = await _context.Users.FirstOrDefaultAsync(u => u.DoctorId == order.DoctorId);
            if (doctorUser != null)
            {
                await _notificationService.CreateNotificationAsync(
                    doctorUser.Id,
                    "Diagnostic Results Received",
                    $"Results for Patient {order.Patient?.FirstName} {order.Patient?.LastName} (Order #{order.RequisitionToken}) have been received from {technicianName ?? order.PartnerName ?? "Diagnostic Partner"} and attached to their clinical chart.",
                    "DiagnosticUpload"
                );
            }
        }
        catch
        {
            // Non-blocking notification dispatch
        }

        return Ok(new PartnerUploadResultDto
        {
            Success = true,
            Message = "Diagnostic results successfully received and auto-linked to patient EMR.",
            FilesProcessed = savedFileUrls.Count,
            OrderStatus = order.Status,
            FileUrls = savedFileUrls
        });
    }

    private static List<string> TryParseJsonList(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
