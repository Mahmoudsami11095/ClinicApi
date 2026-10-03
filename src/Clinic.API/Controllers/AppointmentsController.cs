using Clinic.Application.Common;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentRepository _repo;
    private readonly IClinicRepository _clinicRepo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepo;
    private readonly IPatientRepository _patientRepo;
    private readonly IWhatsAppNotificationService _whatsAppNotificationService;

    public AppointmentsController(
        IAppointmentRepository repo, 
        IClinicRepository clinicRepo, 
        IDoctorRepository doctorRepo,
        INotificationService notificationService,
        IUserRepository userRepo,
        IPatientRepository patientRepo,
        IWhatsAppNotificationService whatsAppNotificationService)
    {
        _repo = repo;
        _clinicRepo = clinicRepo;
        _doctorRepo = doctorRepo;
        _notificationService = notificationService;
        _userRepo = userRepo;
        _patientRepo = patientRepo;
        _whatsAppNotificationService = whatsAppNotificationService;
    }

    private async Task<string?> ValidateDoctorAvailability(string doctorId, string clinicId, string appointmentDateStr)
    {
        if (string.IsNullOrEmpty(doctorId) || string.IsNullOrEmpty(clinicId) || string.IsNullOrEmpty(appointmentDateStr))
            return null;

        var d = await _doctorRepo.GetByIdAsync(doctorId);
        if (d == null)
            return "Doctor not found";

        if (!DateTime.TryParse(appointmentDateStr, out var apptDate))
        {
            return "Invalid appointment date format";
        }

        var dayOfWeek = apptDate.DayOfWeek.ToString();
        var timeOfDay = apptDate.TimeOfDay;

        var dc = d.DoctorClinics.FirstOrDefault(x => x.ClinicId == clinicId);
        
        string? hoursStr = null;
        string? daysStr = null;

        if (dc != null && !string.IsNullOrEmpty(dc.AvailabilityHours))
        {
            hoursStr = dc.AvailabilityHours;
            daysStr = dc.AvailabilityDays;
        }
        else
        {
            var cl = await _clinicRepo.GetByIdAsync(clinicId);
            if (cl != null)
            {
                hoursStr = cl.AvailabilityHours;
                daysStr = cl.AvailabilityDays;
            }
        }

        if (string.IsNullOrEmpty(hoursStr))
        {
            return null;
        }

        if (!string.IsNullOrEmpty(daysStr))
        {
            try
            {
                var days = System.Text.Json.JsonSerializer.Deserialize<List<string>>(daysStr);
                if (days != null && days.Any() && !days.Contains(dayOfWeek, StringComparer.OrdinalIgnoreCase))
                {
                    return $"Doctor is not available on {dayOfWeek} at this clinic.";
                }
            }
            catch
            {
            }
        }

        var parts = hoursStr.Split('-');
        if (parts.Length == 2)
        {
            if (TimeSpan.TryParse(parts[0], out var startTime) && TimeSpan.TryParse(parts[1], out var endTime))
            {
                if (timeOfDay < startTime || timeOfDay > endTime)
                {
                    return $"Appointment time {timeOfDay:hh\\:mm} is outside the doctor's availability hours ({hoursStr}) for this clinic.";
                }
            }
        }

        return null;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var appointments = await _repo.GetAllAsync();
        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();
        var roleClaim = User.GetUserRole();

        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var allowedClinicIds = await _clinicRepo.GetAllowedClinicIdsForDoctorAsync(doctorIdClaim);
            appointments = appointments.Where(a => allowedClinicIds.Contains(a.ClinicId ?? "")).ToList();
        }
        else if (roleClaim == "assistant")
        {
            var cIds = User.FindAll("clinicIds").Select(c => c.Value).ToList();
            if (cIds.Any())
            {
                appointments = appointments.Where(a => cIds.Contains(a.ClinicId ?? "")).ToList();
            }
            else if (!string.IsNullOrEmpty(clinicIdClaim))
            {
                appointments = appointments.Where(a => a.ClinicId == clinicIdClaim).ToList();
            }
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            appointments = appointments.Where(a => a.ClinicId == clinicIdClaim).ToList();
        }

        var dtos = appointments.Select(MapToDto).ToList();
        return Ok(new { data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AppointmentDto dto)
    {
        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var isAllowed = !string.IsNullOrEmpty(dto.ClinicId) && 
                            await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, dto.ClinicId);
            if (!isAllowed)
                return StatusCode(403, new { message = "You can only manage appointments for your clinics" });
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (dto.ClinicId != clinicIdClaim)
                return StatusCode(403, new { message = "You can only manage appointments for your assigned clinic" });
        }

        var validationError = await ValidateDoctorAvailability(dto.DoctorId, dto.ClinicId ?? "", dto.Date);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        var entity = new Appointment
        {
            Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id,
            PatientId = dto.PatientId, DoctorId = dto.DoctorId,
            Date = dto.Date, Status = dto.Status, Type = dto.Type,
            Notes = dto.Notes, ClinicId = dto.ClinicId,
            RoomNumber = dto.RoomNumber
        };
        await _repo.AddAsync(entity);

        // Notify Doctor
        var users = await _userRepo.GetAllAsync();
        var doctorUser = users.FirstOrDefault(u => u.DoctorId == dto.DoctorId);
        if (doctorUser != null)
        {
            await _notificationService.CreateNotificationAsync(
                doctorUser.Id,
                "New Appointment",
                $"You have a new appointment scheduled for {dto.Date}.",
                "Appointment"
            );
        }

        // Notify Patient via WhatsApp
        var patient = await _patientRepo.GetByIdAsync(dto.PatientId);
        var clinic = await _clinicRepo.GetByIdAsync(dto.ClinicId ?? "");
        if (patient != null && clinic != null && !string.IsNullOrWhiteSpace(patient.ContactNumber))
        {
            var dateOnly = dto.Date;
            var timeOnly = "";
            if (DateTime.TryParse(dto.Date, out var dt))
            {
                dateOnly = dt.ToString("MMMM dd, yyyy");
                timeOnly = dt.ToString("h:mm tt");
            }

            await _whatsAppNotificationService.SendAppointmentConfirmationAsync(
                patient.ContactNumber,
                patient.FirstName,
                clinic.Name ?? "Clinic",
                dto.Type ?? "Appointment",
                dateOnly,
                timeOnly
            );
        }

        return Ok(new { message = "Success", data = dto });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] AppointmentDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Not found" });

        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var isAllowed = !string.IsNullOrEmpty(entity.ClinicId) && 
                            await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, entity.ClinicId);
            if (!isAllowed)
                return StatusCode(403, new { message = "You can only manage appointments for your clinics" });
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (dto.ClinicId != clinicIdClaim || entity.ClinicId != clinicIdClaim)
                return StatusCode(403, new { message = "You can only manage appointments for your assigned clinic" });
        }

        var validationError = await ValidateDoctorAvailability(dto.DoctorId, dto.ClinicId ?? "", dto.Date);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        entity.PatientId = dto.PatientId;
        entity.DoctorId = dto.DoctorId;
        entity.Date = dto.Date;
        entity.Status = dto.Status;
        entity.Type = dto.Type;
        entity.Notes = dto.Notes;
        entity.ClinicId = dto.ClinicId;
        entity.RoomNumber = dto.RoomNumber;
        await _repo.UpdateAsync(entity);

        return Ok(new { message = "Success", data = dto });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Not found" });

        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var isAllowed = !string.IsNullOrEmpty(entity.ClinicId) && 
                            await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, entity.ClinicId);
            if (!isAllowed)
                return StatusCode(403, new { message = "You can only manage appointments for your clinics" });
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (entity.ClinicId != clinicIdClaim)
                return StatusCode(403, new { message = "You can only manage appointments for your assigned clinic" });
        }

        await _repo.DeleteAsync(id);

        // Notify Doctor
        var users = await _userRepo.GetAllAsync();
        var doctorUser = users.FirstOrDefault(u => u.DoctorId == entity.DoctorId);
        if (doctorUser != null)
        {
            await _notificationService.CreateNotificationAsync(
                doctorUser.Id,
                "Appointment Cancelled",
                $"Your appointment on {entity.Date} has been cancelled.",
                "Appointment"
            );
        }

        // Notify Patient via WhatsApp
        var patient = await _patientRepo.GetByIdAsync(entity.PatientId);
        var clinic = await _clinicRepo.GetByIdAsync(entity.ClinicId ?? "");
        if (patient != null && clinic != null && !string.IsNullOrWhiteSpace(patient.ContactNumber))
        {
            var dateOnly = entity.Date;
            var timeOnly = "";
            if (DateTime.TryParse(entity.Date, out var dt))
            {
                dateOnly = dt.ToString("MMMM dd, yyyy");
                timeOnly = dt.ToString("h:mm tt");
            }

            await _whatsAppNotificationService.SendAppointmentCancellationAsync(
                patient.ContactNumber,
                patient.FirstName,
                clinic.Name ?? "Clinic",
                dateOnly,
                timeOnly
            );
        }

        return Ok(new { message = "Deleted" });
    }

    /// <summary>
    /// REQ-APT-02 / UAT-APT-02: Patient check-in by receptionist.
    /// Advances status to "waiting", stamps ArrivedAt, calculates sequential daily QueueNumber,
    /// and triggers real-time notification to the doctor.
    /// </summary>
    [HttpPost("{id}/check-in")]
    public async Task<IActionResult> CheckIn(string id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Appointment not found" });

        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var isAllowed = !string.IsNullOrEmpty(entity.ClinicId) && 
                            await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, entity.ClinicId);
            if (!isAllowed) return StatusCode(403, new { message = "You can only manage appointments for your clinics" });
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (entity.ClinicId != clinicIdClaim) return StatusCode(403, new { message = "You can only manage appointments for your assigned clinic" });
        }

        // Calculate today's next queue ticket number
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var allAppts = await _repo.GetAllAsync();
        var existingQueue = allAppts
            .Where(a => a.ClinicId == entity.ClinicId && 
                        a.DoctorId == entity.DoctorId && 
                        !string.IsNullOrEmpty(a.ArrivedAt) && 
                        a.ArrivedAt.StartsWith(today))
            .ToList();

        var nextQueueNum = existingQueue.Any() 
            ? existingQueue.Max(a => a.QueueNumber ?? 0) + 1 
            : 1;

        entity.Status = "waiting";
        entity.ArrivedAt = DateTime.UtcNow.ToString("o");
        entity.QueueNumber = nextQueueNum;

        await _repo.UpdateAsync(entity);

        // Fetch patient name for the real-time notification
        var patient = await _patientRepo.GetByIdAsync(entity.PatientId);
        var patientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "Patient";

        // Dispatch in-app notification to the doctor
        var users = await _userRepo.GetAllAsync();
        var doctorUser = users.FirstOrDefault(u => u.DoctorId == entity.DoctorId);
        if (doctorUser != null)
        {
            await _notificationService.CreateNotificationAsync(
                doctorUser.Id,
                "Patient Arrived & Waiting",
                $"{patientName} has arrived and is in the waiting room (Queue #{entity.QueueNumber}).",
                "Queue"
            );
        }

        return Ok(new { 
            message = "Patient checked in and placed in waiting queue successfully.", 
            data = MapToDto(entity) 
        });
    }

    /// <summary>
    /// REQ-APT-02 & REQ-CLI-03: Doctor calls patient into exam room.
    /// Advances status to "in_consultation", stamps ConsultationStartedAt, and optionally assigns room/chair.
    /// </summary>
    [HttpPost("{id}/start-consultation")]
    public async Task<IActionResult> StartConsultation(string id, [FromQuery] string? roomNumber = null)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Appointment not found" });

        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var isAllowed = !string.IsNullOrEmpty(entity.ClinicId) && 
                            await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, entity.ClinicId);
            if (!isAllowed) return StatusCode(403, new { message = "You can only manage appointments for your clinics" });
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (entity.ClinicId != clinicIdClaim) return StatusCode(403, new { message = "You can only manage appointments for your assigned clinic" });
        }

        entity.Status = "in_consultation";
        entity.ConsultationStartedAt = DateTime.UtcNow.ToString("o");
        if (!string.IsNullOrEmpty(roomNumber))
        {
            entity.RoomNumber = roomNumber;
        }

        await _repo.UpdateAsync(entity);

        return Ok(new { 
            message = "Patient consultation started.", 
            data = MapToDto(entity) 
        });
    }

    /// <summary>
    /// REQ-APT-02: Doctor completes consultation.
    /// Advances status to "completed" and stamps ConsultationEndedAt.
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteConsultation(string id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Appointment not found" });

        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var isAllowed = !string.IsNullOrEmpty(entity.ClinicId) && 
                            await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, entity.ClinicId);
            if (!isAllowed) return StatusCode(403, new { message = "You can only manage appointments for your clinics" });
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (entity.ClinicId != clinicIdClaim) return StatusCode(403, new { message = "You can only manage appointments for your assigned clinic" });
        }

        entity.Status = "completed";
        entity.ConsultationEndedAt = DateTime.UtcNow.ToString("o");

        await _repo.UpdateAsync(entity);

        return Ok(new { 
            message = "Consultation completed successfully.", 
            data = MapToDto(entity) 
        });
    }

    /// <summary>
    /// REQ-APT-02: Retrieves live waiting room queue for today.
    /// Orders active patients: in_consultation first, then waiting by QueueNumber, then scheduled.
    /// </summary>
    [HttpGet("live-queue")]
    public async Task<IActionResult> GetLiveQueue([FromQuery] string? clinicId, [FromQuery] string? doctorId)
    {
        var appointments = await _repo.GetAllAsync();
        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();

        var targetClinicId = !string.IsNullOrEmpty(clinicId) ? clinicId : clinicIdClaim;
        var targetDoctorId = !string.IsNullOrEmpty(doctorId) ? doctorId : doctorIdClaim;

        if (!string.IsNullOrEmpty(targetClinicId))
        {
            appointments = appointments.Where(a => a.ClinicId == targetClinicId).ToList();
        }
        if (!string.IsNullOrEmpty(targetDoctorId))
        {
            appointments = appointments.Where(a => a.DoctorId == targetDoctorId).ToList();
        }

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var activeQueue = appointments
            .Where(a => a.Date.StartsWith(today) || a.Status == "waiting" || a.Status == "in_consultation")
            .OrderBy(a => a.Status == "in_consultation" ? 0 : a.Status == "waiting" ? 1 : 2)
            .ThenBy(a => a.QueueNumber ?? int.MaxValue)
            .ThenBy(a => a.Date)
            .Select(MapToDto)
            .ToList();

        return Ok(new { data = activeQueue });
    }

    /// <summary>
    /// REQ-NOTIF-02: Sends manual or automated WhatsApp/SMS reminder for an appointment.
    /// Updates LastReminderSentAt and increments ReminderCount.
    /// </summary>
    [HttpPost("{id}/send-reminder")]
    public async Task<IActionResult> SendReminder(string id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Appointment not found" });

        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var isAllowed = !string.IsNullOrEmpty(entity.ClinicId) && 
                            await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, entity.ClinicId);
            if (!isAllowed) return StatusCode(403, new { message = "You can only manage appointments for your clinics" });
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (entity.ClinicId != clinicIdClaim) return StatusCode(403, new { message = "You can only manage appointments for your assigned clinic" });
        }

        var patient = await _patientRepo.GetByIdAsync(entity.PatientId);
        var clinic = !string.IsNullOrEmpty(entity.ClinicId) ? await _clinicRepo.GetByIdAsync(entity.ClinicId) : null;
        var doctor = await _doctorRepo.GetByIdAsync(entity.DoctorId);

        var dateOnly = entity.Date;
        var timeOnly = "";
        if (DateTime.TryParse(entity.Date, out var dt))
        {
            dateOnly = dt.ToString("yyyy-MM-dd");
            timeOnly = dt.ToString("hh:mm tt");
        }

        if (patient != null && !string.IsNullOrWhiteSpace(patient.ContactNumber))
        {
            await _whatsAppNotificationService.SendAppointmentReminderAsync(
                patient.ContactNumber,
                patient.FirstName,
                clinic?.Name ?? "Clinic",
                doctor != null ? $"{doctor.FirstName} {doctor.LastName}" : "Your Doctor",
                entity.Type ?? "Consultation",
                dateOnly,
                timeOnly
            );
        }

        entity.LastReminderSentAt = DateTime.UtcNow.ToString("o");
        entity.ReminderCount = (entity.ReminderCount ?? 0) + 1;
        await _repo.UpdateAsync(entity);

        // Record in-app confirmation notification
        var users = await _userRepo.GetAllAsync();
        var doctorUser = users.FirstOrDefault(u => u.DoctorId == entity.DoctorId);
        if (doctorUser != null)
        {
            await _notificationService.CreateNotificationAsync(
                doctorUser.Id,
                "Appointment Reminder Sent",
                $"Reminder sent to {patient?.FirstName ?? "patient"} for visit on {dateOnly} {timeOnly}.",
                "Reminder"
            );
        }

        return Ok(new { 
            message = "Appointment reminder dispatched successfully via WhatsApp/SMS.", 
            data = MapToDto(entity) 
        });
    }

    /// <summary>
    /// REQ-NOTIF-02: Batch-sends reminders to all patients with upcoming appointments in the next 24 hours.
    /// </summary>
    [HttpPost("send-batch-reminders")]
    public async Task<IActionResult> SendBatchReminders([FromQuery] string? clinicId)
    {
        var targetClinicId = !string.IsNullOrEmpty(clinicId) ? clinicId : User.GetClinicId();
        var appointments = await _repo.GetAllAsync();

        if (!string.IsNullOrEmpty(targetClinicId))
        {
            appointments = appointments.Where(a => a.ClinicId == targetClinicId).ToList();
        }

        var now = DateTime.UtcNow;
        var next24h = now.AddHours(24);
        var sentCount = 0;

        foreach (var appt in appointments.Where(a => a.Status == "scheduled"))
        {
            if (DateTime.TryParse(appt.Date, out var apptDate) && apptDate >= now && apptDate <= next24h)
            {
                // Avoid spamming if reminder was sent within the last 12 hours
                if (!string.IsNullOrEmpty(appt.LastReminderSentAt) && 
                    DateTime.TryParse(appt.LastReminderSentAt, out var lastSent) && 
                    (now - lastSent).TotalHours < 12)
                {
                    continue;
                }

                var patient = await _patientRepo.GetByIdAsync(appt.PatientId);
                var clinic = !string.IsNullOrEmpty(appt.ClinicId) ? await _clinicRepo.GetByIdAsync(appt.ClinicId) : null;
                var doctor = await _doctorRepo.GetByIdAsync(appt.DoctorId);

                var dateOnly = apptDate.ToString("yyyy-MM-dd");
                var timeOnly = apptDate.ToString("hh:mm tt");

                if (patient != null && !string.IsNullOrWhiteSpace(patient.ContactNumber))
                {
                    await _whatsAppNotificationService.SendAppointmentReminderAsync(
                        patient.ContactNumber,
                        patient.FirstName,
                        clinic?.Name ?? "Clinic",
                        doctor != null ? $"{doctor.FirstName} {doctor.LastName}" : "Your Doctor",
                        appt.Type ?? "Consultation",
                        dateOnly,
                        timeOnly
                    );
                }

                appt.LastReminderSentAt = DateTime.UtcNow.ToString("o");
                appt.ReminderCount = (appt.ReminderCount ?? 0) + 1;
                await _repo.UpdateAsync(appt);
                sentCount++;
            }
        }

        return Ok(new { 
            message = $"Dispatched {sentCount} appointment reminder(s) for the next 24 hours.", 
            count = sentCount 
        });
    }

    private static AppointmentDto MapToDto(Appointment a) => new()
    {
        Id = a.Id,
        PatientId = a.PatientId,
        DoctorId = a.DoctorId,
        Date = a.Date,
        Status = a.Status,
        Type = a.Type,
        Notes = a.Notes,
        ClinicId = a.ClinicId,
        ArrivedAt = a.ArrivedAt,
        ConsultationStartedAt = a.ConsultationStartedAt,
        ConsultationEndedAt = a.ConsultationEndedAt,
        QueueNumber = a.QueueNumber,
        LastReminderSentAt = a.LastReminderSentAt,
        ReminderCount = a.ReminderCount ?? 0,
        RoomNumber = a.RoomNumber
    };
}
