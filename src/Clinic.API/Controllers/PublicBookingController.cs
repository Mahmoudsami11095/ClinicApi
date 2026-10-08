using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/public/clinics")]
[AllowAnonymous]
public class PublicBookingController : ControllerBase
{
    private readonly ClinicDbContext _context;
    private readonly IClinicRepository _clinicRepo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly IPatientRepository _patientRepo;
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IWhatsAppNotificationService _whatsAppService;

    public PublicBookingController(
        ClinicDbContext context,
        IClinicRepository clinicRepo,
        IDoctorRepository doctorRepo,
        IPatientRepository patientRepo,
        IAppointmentRepository appointmentRepo,
        IWhatsAppNotificationService whatsAppService)
    {
        _context = context;
        _clinicRepo = clinicRepo;
        _doctorRepo = doctorRepo;
        _patientRepo = patientRepo;
        _appointmentRepo = appointmentRepo;
        _whatsAppService = whatsAppService;
    }

    [HttpGet("{slug}/booking-data")]
    public async Task<IActionResult> GetBookingData(string slug)
    {
        var clinic = await _clinicRepo.GetBySlugAsync(slug) 
                     ?? await _context.Clinics
                         .Include(c => c.DoctorClinics)
                             .ThenInclude(dc => dc.Doctor)
                         .FirstOrDefaultAsync(c => c.Id == slug);

        if (clinic == null)
        {
            return NotFound(new { message = "Clinic not found" });
        }

        if (!clinic.PublicBookingEnabled)
        {
            return BadRequest(new { message = "Public online booking is disabled for this clinic." });
        }

        var doctors = new List<PublicDoctorCardDto>();

        // Include creator doctor if not in DoctorClinics
        if (!string.IsNullOrEmpty(clinic.CreatorDoctorId))
        {
            var creatorDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == clinic.CreatorDoctorId);
            if (creatorDoctor != null)
            {
                var rel = clinic.DoctorClinics.FirstOrDefault(dc => dc.DoctorId == creatorDoctor.Id);
                var days = ParseDays(rel?.AvailabilityDays ?? clinic.AvailabilityDays);
                var hours = rel?.AvailabilityHours ?? clinic.AvailabilityHours;

                doctors.Add(new PublicDoctorCardDto
                {
                    Id = creatorDoctor.Id,
                    FullName = $"{creatorDoctor.FirstName} {creatorDoctor.LastName}".Trim(),
                    Specialization = creatorDoctor.Specialization ?? "General Dentistry",
                    Avatar = creatorDoctor.Avatar,
                    ClinicAvailabilityDays = days,
                    ClinicAvailabilityHours = hours
                });
            }
        }

        // Add associated accepted doctors
        foreach (var dc in clinic.DoctorClinics.Where(dc => dc.Status == "Accepted"))
        {
            if (doctors.Any(d => d.Id == dc.DoctorId)) continue;
            var doc = dc.Doctor ?? await _context.Doctors.FirstOrDefaultAsync(d => d.Id == dc.DoctorId);
            if (doc != null)
            {
                var days = ParseDays(dc.AvailabilityDays ?? clinic.AvailabilityDays);
                var hours = dc.AvailabilityHours ?? clinic.AvailabilityHours;

                doctors.Add(new PublicDoctorCardDto
                {
                    Id = doc.Id,
                    FullName = $"{doc.FirstName} {doc.LastName}".Trim(),
                    Specialization = doc.Specialization ?? "Specialist",
                    Avatar = doc.Avatar,
                    ClinicAvailabilityDays = days,
                    ClinicAvailabilityHours = hours
                });
            }
        }

        var result = new PublicClinicBookingMetadataDto
        {
            Id = clinic.Id,
            Name = clinic.Name,
            Slug = clinic.Slug ?? clinic.Id,
            Address = clinic.Address,
            Phone = clinic.Phone,
            AvailabilityHours = clinic.AvailabilityHours,
            AvailabilityDays = clinic.AvailabilityDays,
            City = clinic.City,
            State = clinic.State,
            Country = clinic.Country,
            PublicBookingEnabled = clinic.PublicBookingEnabled,
            QrPosterAssetUrl = clinic.QrPosterAssetUrl,
            Doctors = doctors
        };

        return Ok(new { data = result });
    }

    [HttpGet("{slug}/doctors/{doctorId}/available-slots")]
    public async Task<IActionResult> GetDoctorSlots(string slug, string doctorId, [FromQuery] string date)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            return BadRequest(new { message = "Date query parameter is required (YYYY-MM-DD)" });
        }

        var clinic = await _clinicRepo.GetBySlugAsync(slug) 
                     ?? await _context.Clinics.FirstOrDefaultAsync(c => c.Id == slug);

        if (clinic == null) return NotFound(new { message = "Clinic not found" });

        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
        if (doctor == null) return NotFound(new { message = "Doctor not found" });

        // Parse date to day of week
        if (!DateTime.TryParse(date, out var targetDate))
        {
            return BadRequest(new { message = "Invalid date format. Expected YYYY-MM-DD." });
        }

        var dayName = targetDate.DayOfWeek.ToString();

        // Check availability relationship
        var rel = await _context.DoctorClinics
            .FirstOrDefaultAsync(dc => dc.ClinicId == clinic.Id && dc.DoctorId == doctorId);

        var days = ParseDays(rel?.AvailabilityDays ?? clinic.AvailabilityDays);
        if (!days.Contains(dayName, StringComparer.OrdinalIgnoreCase) && days.Count > 0)
        {
            return Ok(new { data = new List<PublicTimeSlotDto>() }); // Not working on this day
        }

        var hours = rel?.AvailabilityHours ?? clinic.AvailabilityHours ?? "09:00-17:00";
        var generatedSlots = GenerateTimeSlots(hours);

        // Cross-clinic double-booking check (BR-SAAS-02 / BR-QR-02)
        // Find all confirmed appointments for this doctor on targetDate across ANY clinic
        var existingAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.Date.StartsWith(date) && a.Status != "cancelled")
            .Select(a => a.Date)
            .ToListAsync();

        var slots = generatedSlots.Select(time => new PublicTimeSlotDto
        {
            Time = time,
            Available = !existingAppointments.Any(appDate => appDate.Contains(time))
        }).ToList();

        return Ok(new { data = slots });
    }

    [HttpPost("{slug}/book-appointment")]
    public async Task<IActionResult> BookAppointment(string slug, [FromBody] PublicAppointmentBookingRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.DoctorId) || 
            string.IsNullOrWhiteSpace(request.Date) || 
            string.IsNullOrWhiteSpace(request.Time) ||
            string.IsNullOrWhiteSpace(request.PatientName) ||
            string.IsNullOrWhiteSpace(request.PatientPhone))
        {
            return BadRequest(new { message = "All fields (doctorId, date, time, patientName, patientPhone) are required." });
        }

        var clinic = await _clinicRepo.GetBySlugAsync(slug) 
                     ?? await _context.Clinics.FirstOrDefaultAsync(c => c.Id == slug);

        if (clinic == null) return NotFound(new { message = "Clinic not found" });
        if (!clinic.PublicBookingEnabled) return BadRequest(new { message = "Public online booking is disabled for this clinic." });

        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == request.DoctorId);
        if (doctor == null) return NotFound(new { message = "Doctor not found" });

        var apptDateTime = $"{request.Date} {request.Time}";

        // Cross-clinic collision check
        var isBooked = await _context.Appointments.AnyAsync(a => 
            a.DoctorId == request.DoctorId && 
            a.Date.StartsWith(request.Date) &&
            a.Date.Contains(request.Time) && 
            a.Status != "cancelled");

        if (isBooked)
        {
            return Conflict(new { message = "The requested time slot is no longer available. Please select another slot." });
        }

        // Clean phone number
        var cleanPhone = request.PatientPhone.Trim().Replace(" ", "");

        // Find or create patient
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => (p.PhoneNumber == cleanPhone || p.Email == cleanPhone) && !p.IsDeleted);

        if (patient == null)
        {
            var nameParts = request.PatientName.Trim().Split(' ', 2);
            patient = new Patient
            {
                Id = Guid.NewGuid().ToString(),
                FirstName = nameParts[0],
                LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
                PhoneNumber = cleanPhone,
                CountryCode = "+20",
                ClinicId = clinic.Id,
                RegistrationDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
            };
            await _context.Patients.AddAsync(patient);
            await _context.SaveChangesAsync();
        }

        // Create appointment
        var appointment = new Appointment
        {
            Id = Guid.NewGuid().ToString(),
            ClinicId = clinic.Id,
            DoctorId = doctor.Id,
            PatientId = patient.Id,
            Date = apptDateTime,
            Status = "scheduled",
            Type = "In-Person",
            Notes = $"Online QR Booking: {request.Reason ?? "General Consultation"}"
        };

        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync();

        // Fire-and-forget WhatsApp confirmation
        try
        {
            _ = Task.Run(async () =>
            {
                await _whatsAppService.SendAppointmentConfirmationAsync(
                    cleanPhone,
                    $"{patient.FirstName} {patient.LastName}".Trim(),
                    clinic.Name,
                    "In-Person Consultation",
                    request.Date,
                    request.Time
                );
            });
        }
        catch
        {
            // WhatsApp failure should not abort booking
        }

        return Ok(new
        {
            message = "Appointment booked successfully!",
            data = new
            {
                appointmentId = appointment.Id,
                clinicName = clinic.Name,
                doctorName = $"{doctor.FirstName} {doctor.LastName}".Trim(),
                patientName = $"{patient.FirstName} {patient.LastName}".Trim(),
                date = appointment.Date,
                time = request.Time,
                status = appointment.Status
            }
        });
    }

    private static List<string> ParseDays(string? daysStr)
    {
        if (string.IsNullOrWhiteSpace(daysStr))
            return new List<string> { "Saturday", "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday" };

        return daysStr.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(d => d.Trim())
                      .ToList();
    }

    private static List<string> GenerateTimeSlots(string hoursStr)
    {
        var slots = new List<string>();
        // Expected format: "14:00-21:00" or "09:00 - 17:00"
        var parts = hoursStr.Split('-');
        if (parts.Length != 2 || 
            !TimeSpan.TryParse(parts[0].Trim(), out var start) || 
            !TimeSpan.TryParse(parts[1].Trim(), out var end))
        {
            start = new TimeSpan(9, 0, 0);
            end = new TimeSpan(17, 0, 0);
        }

        var current = start;
        while (current < end)
        {
            slots.Add(current.ToString(@"hh\:mm"));
            current = current.Add(TimeSpan.FromMinutes(30));
        }

        return slots;
    }
}
