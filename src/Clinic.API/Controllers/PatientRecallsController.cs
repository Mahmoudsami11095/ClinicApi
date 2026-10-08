using System;
using System.Linq;
using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/recalls")]
[Authorize(Roles = "admin,doctor,assistant")]
public class PatientRecallsController : ControllerBase
{
    private readonly ClinicDbContext _context;
    private readonly IPatientRecallRepository _recallRepo;
    private readonly IWhatsAppNotificationService _whatsAppService;

    public PatientRecallsController(
        ClinicDbContext context,
        IPatientRecallRepository recallRepo,
        IWhatsAppNotificationService whatsAppService)
    {
        _context = context;
        _recallRepo = recallRepo;
        _whatsAppService = whatsAppService;
    }

    // ── 1. Create / Schedule Recall ──
    [HttpPost]
    public async Task<IActionResult> CreateRecall([FromBody] CreatePatientRecallDto dto)
    {
        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null)
            return NotFound(new { message = "Patient record not found." });

        var doctor = await _context.Doctors.FindAsync(dto.DoctorId);
        if (doctor == null)
            return NotFound(new { message = "Doctor record not found." });

        var clinic = await _context.Clinics.FindAsync(dto.ClinicId);
        if (clinic == null)
            return NotFound(new { message = "Clinic facility not found." });

        var recallNumber = await _recallRepo.GetNextRecallNumberAsync();
        var dueDate = dto.CustomDueDate ?? DateTime.UtcNow.AddMonths(dto.RecallIntervalMonths > 0 ? dto.RecallIntervalMonths : 6);

        var recall = new PatientRecall
        {
            Id = Guid.NewGuid().ToString(),
            RecallNumber = recallNumber,
            ClinicId = dto.ClinicId,
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            SourceAppointmentId = dto.SourceAppointmentId,
            RecallType = dto.RecallType,
            RecallIntervalMonths = dto.RecallIntervalMonths,
            DueDate = dueDate,
            Status = dueDate <= DateTime.UtcNow ? "Due" : "Scheduled",
            NotificationChannel = "WhatsApp",
            ClinicalNotes = dto.ClinicalNotes,
            CreatedAt = DateTime.UtcNow
        };

        await _recallRepo.AddAsync(recall);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = recall.Id }, MapToDto(recall, clinic.Name, $"{patient.FirstName} {patient.LastName}", patient.PhoneNumber, $"Dr. {doctor.FirstName} {doctor.LastName}"));
    }

    // ── 2. Query Recalls ──
    [HttpGet]
    public async Task<IActionResult> GetRecalls([FromQuery] string? clinicId, [FromQuery] string? status, [FromQuery] string? doctorId)
    {
        var query = _context.PatientRecalls
            .Include(r => r.Clinic)
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .Where(r => !r.IsDeleted);

        if (!string.IsNullOrEmpty(clinicId) && clinicId != "all")
            query = query.Where(r => r.ClinicId == clinicId);

        if (!string.IsNullOrEmpty(status) && status != "all")
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrEmpty(doctorId))
            query = query.Where(r => r.DoctorId == doctorId);

        var list = await query.OrderBy(r => r.DueDate).AsNoTracking().ToListAsync();

        return Ok(new { data = list.Select(r => MapToDto(r, r.Clinic?.Name, $"{r.Patient?.FirstName} {r.Patient?.LastName}", r.Patient?.PhoneNumber, $"Dr. {r.Doctor?.FirstName} {r.Doctor?.LastName}")) });
    }

    // ── 3. Recall Summary Metrics (REQ-REC-04) ──
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] string? clinicId)
    {
        var query = _context.PatientRecalls.Where(r => !r.IsDeleted);

        if (!string.IsNullOrEmpty(clinicId) && clinicId != "all")
            query = query.Where(r => r.ClinicId == clinicId);

        var all = await query.AsNoTracking().ToListAsync();
        var now = DateTime.UtcNow;

        var totalDue = all.Count(r => r.DueDate <= now && r.Status != "Completed" && r.Status != "Cancelled");
        var overdue = all.Count(r => r.DueDate < now.AddDays(-7) && r.Status != "Completed" && r.Status != "Cancelled");
        var dispatched = all.Count(r => r.NotificationSentAt.HasValue);
        var booked = all.Count(r => r.Status == "Booked");
        var completed = all.Count(r => r.Status == "Completed");

        var conversionDenominator = booked + completed + dispatched;
        var conversionRate = conversionDenominator > 0
            ? Math.Round(((double)(booked + completed) / conversionDenominator) * 100.0, 1)
            : 0.0;

        return Ok(new
        {
            data = new RecallSummaryDto
            {
                TotalDue = totalDue,
                OverdueCount = overdue,
                DispatchedCount = dispatched,
                BookedCount = booked,
                CompletedCount = completed,
                ConversionRatePercentage = conversionRate
            }
        });
    }

    // ── 4. Get By Id ──
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var recall = await _recallRepo.GetByIdAsync(id);
        if (recall == null)
            return NotFound(new { message = "Recall record not found." });

        return Ok(new { data = MapToDto(recall, recall.Clinic?.Name, $"{recall.Patient?.FirstName} {recall.Patient?.LastName}", recall.Patient?.PhoneNumber, $"Dr. {recall.Doctor?.FirstName} {recall.Doctor?.LastName}") });
    }

    // ── 5. Dispatch Recall Engagement Notification (REQ-REC-02) ──
    [HttpPost("{id}/dispatch")]
    public async Task<IActionResult> DispatchRecall(string id, [FromBody] DispatchRecallDto dto)
    {
        var recall = await _recallRepo.GetByIdAsync(id);
        if (recall == null)
            return NotFound(new { message = "Recall record not found." });

        var patientName = $"{recall.Patient?.FirstName} {recall.Patient?.LastName}".Trim();
        var doctorName = $"Dr. {recall.Doctor?.FirstName} {recall.Doctor?.LastName}".Trim();
        var clinicName = recall.Clinic?.Name ?? "Clinic";

        var message = dto.CustomMessage ??
            $"Hello {patientName}, this is {clinicName}. Dr. {doctorName} recommends your preventative {recall.RecallType} check-up. Book your preferred slot here: https://clinic-app-ten-topaz.vercel.app/book/{recall.ClinicId}?recall={recall.RecallNumber}";

        if (!string.IsNullOrEmpty(recall.Patient?.PhoneNumber))
        {
            try
            {
                await _whatsAppService.SendNotificationAsync(recall.Patient.PhoneNumber, message);
            }
            catch { /* Best effort delivery */ }
        }

        recall.NotificationChannel = dto.Channel;
        recall.NotificationSentAt = DateTime.UtcNow;
        recall.ReminderCount++;
        recall.Status = "NotificationSent";

        await _recallRepo.UpdateAsync(recall);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Recall engagement reminder dispatched successfully.", data = MapToDto(recall, recall.Clinic?.Name, patientName, recall.Patient?.PhoneNumber, doctorName) });
    }

    // ── 6. 1-Click Batch Outreach Campaign (REQ-REC-03) ──
    [HttpPost("batch-dispatch")]
    public async Task<IActionResult> BatchDispatch([FromQuery] string clinicId)
    {
        var now = DateTime.UtcNow;
        var dueRecalls = await _context.PatientRecalls
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .Include(r => r.Clinic)
            .Where(r => r.ClinicId == clinicId && !r.IsDeleted &&
                        r.DueDate <= now &&
                        (r.Status == "Due" || r.Status == "Scheduled") &&
                        (!r.NotificationSentAt.HasValue || r.NotificationSentAt < now.AddDays(-7)))
            .Take(50)
            .ToListAsync();

        int dispatchedCount = 0;
        foreach (var r in dueRecalls)
        {
            var pName = $"{r.Patient?.FirstName} {r.Patient?.LastName}".Trim();
            var dName = $"Dr. {r.Doctor?.FirstName} {r.Doctor?.LastName}".Trim();
            var cName = r.Clinic?.Name ?? "Clinic";

            var msg = $"Dear {pName}, your {r.RecallType} preventative check-up at {cName} with Dr. {dName} is due. Please reserve your slot: https://clinic-app-ten-topaz.vercel.app/book/{r.ClinicId}?recall={r.RecallNumber}";

            if (!string.IsNullOrEmpty(r.Patient?.PhoneNumber))
            {
                try { await _whatsAppService.SendNotificationAsync(r.Patient.PhoneNumber, msg); }
                catch { }
            }

            r.NotificationSentAt = now;
            r.ReminderCount++;
            r.Status = "NotificationSent";
            dispatchedCount++;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = $"Batch recall outreach successfully queued for {dispatchedCount} patients.", count = dispatchedCount });
    }

    // ── 7. Snooze Recall (BR-REC-04) ──
    [HttpPut("{id}/snooze")]
    public async Task<IActionResult> SnoozeRecall(string id, [FromBody] SnoozeRecallDto dto)
    {
        var recall = await _recallRepo.GetByIdAsync(id);
        if (recall == null)
            return NotFound(new { message = "Recall record not found." });

        var weeks = dto.SnoozeWeeks > 0 ? dto.SnoozeWeeks : 4;
        recall.SnoozeUntilDate = DateTime.UtcNow.AddDays(weeks * 7);
        recall.DueDate = recall.SnoozeUntilDate.Value;
        recall.Status = "Snoozed";

        await _recallRepo.UpdateAsync(recall);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Recall snoozed for {weeks} weeks.", data = MapToDto(recall, recall.Clinic?.Name, $"{recall.Patient?.FirstName} {recall.Patient?.LastName}", recall.Patient?.PhoneNumber, $"Dr. {recall.Doctor?.FirstName} {recall.Doctor?.LastName}") });
    }

    // ── 8. Complete Recall (BR-REC-02) ──
    [HttpPut("{id}/complete")]
    public async Task<IActionResult> CompleteRecall(string id, [FromQuery] string? appointmentId)
    {
        var recall = await _recallRepo.GetByIdAsync(id);
        if (recall == null)
            return NotFound(new { message = "Recall record not found." });

        recall.BookedAppointmentId = appointmentId ?? recall.BookedAppointmentId;
        recall.CompletedAt = DateTime.UtcNow;
        recall.Status = "Completed";

        await _recallRepo.UpdateAsync(recall);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Recall successfully completed.", data = MapToDto(recall, recall.Clinic?.Name, $"{recall.Patient?.FirstName} {recall.Patient?.LastName}", recall.Patient?.PhoneNumber, $"Dr. {recall.Doctor?.FirstName} {recall.Doctor?.LastName}") });
    }

    private static PatientRecallResponseDto MapToDto(
        PatientRecall r,
        string? clinicName,
        string? patientName,
        string? patientPhone,
        string? doctorName)
    {
        return new PatientRecallResponseDto
        {
            Id = r.Id,
            RecallNumber = r.RecallNumber,
            ClinicId = r.ClinicId,
            ClinicName = clinicName ?? r.ClinicId,
            PatientId = r.PatientId,
            PatientName = patientName ?? r.PatientId,
            PatientPhone = patientPhone ?? string.Empty,
            DoctorId = r.DoctorId,
            DoctorName = doctorName ?? r.DoctorId,
            SourceAppointmentId = r.SourceAppointmentId,
            BookedAppointmentId = r.BookedAppointmentId,
            RecallType = r.RecallType,
            RecallIntervalMonths = r.RecallIntervalMonths,
            DueDate = r.DueDate,
            Status = r.Status,
            NotificationChannel = r.NotificationChannel,
            NotificationSentAt = r.NotificationSentAt,
            ReminderCount = r.ReminderCount,
            SnoozeUntilDate = r.SnoozeUntilDate,
            ClinicalNotes = r.ClinicalNotes,
            CreatedAt = r.CreatedAt,
            CompletedAt = r.CompletedAt
        };
    }
}
