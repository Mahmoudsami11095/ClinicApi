using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/portal")]
public class PortalController : ControllerBase
{
    private readonly ClinicDbContext _context;
    private readonly IOtpService _otpService;
    private readonly IJwtService _jwtService;
    private readonly IUserRepository _userRepo;

    public PortalController(
        ClinicDbContext context,
        IOtpService otpService,
        IJwtService jwtService,
        IUserRepository userRepo)
    {
        _context = context;
        _otpService = otpService;
        _jwtService = jwtService;
        _userRepo = userRepo;
    }

    public record SendOtpRequest(string PhoneNumber);
    public record VerifyOtpRequest(string PhoneNumber, string Code);
    public record BookAppointmentRequest(string? PatientId, string DoctorId, string? ClinicId, string Date, string? Reason);

    [HttpPost("auth/send-otp")]
    [AllowAnonymous]
    public IActionResult SendOtp([FromBody] SendOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            return BadRequest(new { message = "Phone number is required." });

        var phone = request.PhoneNumber.Trim().Replace(" ", "").Replace("-", "");
        var otp = _otpService.GenerateOtp(phone);

        return Ok(new
        {
            success = true,
            message = "Verification code dispatched via WhatsApp/SMS.",
            expiresInMinutes = 5,
            debugOtp = otp
        });
    }

    [HttpPost("auth/verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { message = "Phone number and verification code are required." });

        var phone = request.PhoneNumber.Trim().Replace(" ", "").Replace("-", "");
        var isValid = _otpService.VerifyOtp(phone, request.Code);

        if (!isValid)
            return Unauthorized(new { message = "Invalid or expired verification code." });

        _otpService.RemoveOtp(phone);

        // Find patient by phone number
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.PhoneNumber != null && p.PhoneNumber.EndsWith(phone.Length > 9 ? phone.Substring(phone.Length - 9) : phone));

        // Find or create associated User
        var email = $"patient_{phone}@clinicportal.local";
        var user = await _userRepo.GetByEmailAsync(email);
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                Name = patient != null ? $"{patient.FirstName} {patient.LastName}".Trim() : "Patient",
                Role = UserRole.Patient,
                PatientId = patient?.Id
            };
            await _userRepo.AddAsync(user);
        }

        var token = _jwtService.GenerateToken(user);

        return Ok(new
        {
            token,
            patient = patient != null ? new
            {
                id = patient.Id,
                name = $"{patient.FirstName} {patient.LastName}".Trim(),
                phone = patient.PhoneNumber,
                email = patient.Email,
                gender = patient.Gender,
                clinicId = patient.ClinicId
            } : null
        });
    }

    [HttpGet("doctors/available-slots")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailableSlots([FromQuery] string doctorId, [FromQuery] string date)
    {
        var targetDatePrefix = date.Trim();
        if (targetDatePrefix.Length > 10)
            targetDatePrefix = targetDatePrefix.Substring(0, 10);

        // Fetch existing bookings for this doctor on target date
        var existingAppts = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorId == doctorId && a.Date.StartsWith(targetDatePrefix) && a.Status != "cancelled")
            .Select(a => a.Date)
            .ToListAsync();

        var standardSlots = new List<string>
        {
            "09:00", "09:30", "10:00", "10:30", "11:00", "11:30",
            "12:00", "12:30", "14:00", "14:30", "15:00", "15:30",
            "16:00", "16:30", "17:00", "17:30", "18:00"
        };

        var availableSlots = standardSlots
            .Where(slot => !existingAppts.Any(d => d.Contains(slot)))
            .ToList();

        return Ok(new
        {
            doctorId,
            date = targetDatePrefix,
            slots = availableSlots
        });
    }

    [HttpPost("appointments/book")]
    [Authorize]
    public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentRequest request)
    {
        var targetDate = request.Date.Trim();

        // Check if slot is already occupied
        var isOccupied = await _context.Appointments
            .AnyAsync(a => a.DoctorId == request.DoctorId && a.Date == targetDate && a.Status != "cancelled");

        if (isOccupied)
            return Conflict(new { message = "Selected time slot is no longer available. Please choose another." });

        var patientId = request.PatientId;
        if (string.IsNullOrWhiteSpace(patientId))
        {
            var fallback = await _context.Patients.FirstOrDefaultAsync();
            patientId = fallback?.Id ?? Guid.NewGuid().ToString();
        }

        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == request.DoctorId);
        var clinicId = request.ClinicId;
        if (string.IsNullOrWhiteSpace(clinicId))
        {
            var firstClinic = await _context.Clinics.FirstOrDefaultAsync();
            clinicId = firstClinic?.Id;
        }

        var appt = new Appointment
        {
            Id = Guid.NewGuid().ToString(),
            DoctorId = request.DoctorId,
            PatientId = patientId,
            ClinicId = clinicId,
            Date = targetDate,
            Status = "scheduled",
            Type = "consultation",
            Notes = request.Reason ?? "Self-booked via Patient Portal"
        };

        _context.Appointments.Add(appt);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            appointmentId = appt.Id,
            message = "Appointment booked successfully.",
            details = new
            {
                date = targetDate,
                status = appt.Status
            }
        });
    }

    [HttpGet("queue/status")]
    [Authorize]
    public async Task<IActionResult> GetQueueStatus([FromQuery] string? patientId)
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var targetPatientId = patientId;
        if (string.IsNullOrWhiteSpace(targetPatientId))
        {
            var pat = await _context.Patients.FirstOrDefaultAsync();
            targetPatientId = pat?.Id;
        }

        if (string.IsNullOrWhiteSpace(targetPatientId))
            return Ok(new { inQueue = false, message = "No patient record found." });

        var appt = await _context.Appointments
            .Include(a => a.Doctor)
            .Where(a => a.PatientId == targetPatientId && a.Date.StartsWith(today) && a.Status != "cancelled")
            .FirstOrDefaultAsync();

        if (appt == null || (appt.Status != "checked_in" && appt.Status != "waiting" && appt.Status != "in_consultation"))
        {
            return Ok(new
            {
                inQueue = false,
                message = "You are not currently in the active waiting queue for today."
            });
        }

        var aheadCount = await _context.Appointments
            .Where(a => a.DoctorId == appt.DoctorId &&
                        a.Date.StartsWith(today) &&
                        (a.Status == "checked_in" || a.Status == "waiting") &&
                        a.Id != appt.Id)
            .CountAsync();

        var position = aheadCount + 1;
        var estimatedMinutes = position * 12;

        return Ok(new
        {
            inQueue = true,
            appointmentId = appt.Id,
            position,
            estimatedWaitMinutes = estimatedMinutes,
            doctorName = appt.Doctor != null ? $"Dr. {appt.Doctor.FirstName} {appt.Doctor.LastName}" : "Attending Doctor",
            status = appt.Status,
            date = appt.Date
        });
    }

    [HttpGet("prescriptions")]
    [Authorize]
    public async Task<IActionResult> GetPrescriptions([FromQuery] string? patientId)
    {
        var targetPatientId = patientId;
        if (string.IsNullOrWhiteSpace(targetPatientId))
        {
            var pat = await _context.Patients.FirstOrDefaultAsync();
            targetPatientId = pat?.Id;
        }

        if (string.IsNullOrWhiteSpace(targetPatientId))
            return Ok(new List<object>());

        var prescriptions = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Where(p => p.PatientId == targetPatientId)
            .OrderByDescending(p => p.Date)
            .Take(20)
            .Select(p => new
            {
                id = p.Id,
                date = p.Date,
                doctorName = p.Doctor != null ? $"Dr. {p.Doctor.FirstName} {p.Doctor.LastName}" : "Attending Physician",
                isFinalized = p.IsFinalized,
                status = p.Status,
                medicationsCount = p.Medications.Count,
                medications = p.Medications.Select(m => new
                {
                    name = m.Name,
                    dosage = m.Dosage,
                    frequency = m.Frequency,
                    duration = m.Duration
                })
            })
            .ToListAsync();

        return Ok(prescriptions);
    }
}
