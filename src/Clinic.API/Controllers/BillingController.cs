using Clinic.Application.Common;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly IBillingRepository _repo;
    private readonly IClinicRepository _clinicRepo;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepo;
    private readonly IAppointmentRepository _appointmentRepo;

    public BillingController(
        IBillingRepository repo, 
        IClinicRepository clinicRepo,
        INotificationService notificationService,
        IUserRepository userRepo,
        IAppointmentRepository appointmentRepo)
    {
        _repo = repo;
        _clinicRepo = clinicRepo;
        _notificationService = notificationService;
        _userRepo = userRepo;
        _appointmentRepo = appointmentRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var records = await _repo.GetAllAsync();
        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var allowedClinicIds = await _clinicRepo.GetAllowedClinicIdsForDoctorAsync(doctorIdClaim);
            records = records.Where(r => allowedClinicIds.Contains(r.ClinicId ?? "")).ToList();
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            records = records.Where(r => r.ClinicId == clinicIdClaim).ToList();
        }

        var dtos = records.Select(MapToDto).ToList();
        return Ok(new { data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BillingRecordDto dto)
    {
        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();
        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var isAllowed = !string.IsNullOrEmpty(dto.ClinicId) && 
                            await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, dto.ClinicId);
            if (!isAllowed)
                return StatusCode(403, new { message = "You can only manage billing for your clinics" });
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (dto.ClinicId != clinicIdClaim)
                return StatusCode(403, new { message = "You can only manage billing for your assigned clinic" });
        }

        var isDoctorOrAdmin = User.IsInRole("admin") || User.IsInRole("doctor") || !string.IsNullOrEmpty(User.GetDoctorId());

        // BR-FIN-01: Courtesy Discount Authorization Matrix
        if (dto.DiscountPercentage > 10 && !isDoctorOrAdmin && string.IsNullOrWhiteSpace(dto.DiscountAuthorizedBy))
        {
            return BadRequest(new { message = "Discounts exceeding 10% require Doctor or Admin authorization PIN verification." });
        }

        var entity = MapToEntity(dto);
        entity.Id = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString() : dto.Id;
        await _repo.AddAsync(entity);
        return Ok(new { message = "Success", data = dto });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] BillingRecordDto dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound(new { message = "Not found" });

        var doctorIdClaim = User.GetDoctorId();
        var clinicIdClaim = User.GetClinicId();
        var isDoctorOrAdmin = User.IsInRole("admin") || User.IsInRole("doctor") || !string.IsNullOrEmpty(doctorIdClaim);

        if (!string.IsNullOrEmpty(doctorIdClaim))
        {
            var isAllowed = !string.IsNullOrEmpty(entity.ClinicId) && 
                            await _clinicRepo.IsDoctorAuthorizedForClinicAsync(doctorIdClaim, entity.ClinicId);
            if (!isAllowed)
                return StatusCode(403, new { message = "You can only manage billing for your clinics" });
        }
        else if (!string.IsNullOrEmpty(clinicIdClaim))
        {
            if (dto.ClinicId != clinicIdClaim || entity.ClinicId != clinicIdClaim)
                return StatusCode(403, new { message = "You can only manage billing for your assigned clinic" });
        }

        // BR-FIN-01: Courtesy Discount Authorization Matrix
        if (dto.DiscountPercentage > 10 && !isDoctorOrAdmin && string.IsNullOrWhiteSpace(dto.DiscountAuthorizedBy))
        {
            return BadRequest(new { message = "Discounts exceeding 10% require Doctor or Admin authorization PIN verification." });
        }

        entity.PatientId = dto.PatientId;
        entity.AppointmentId = dto.AppointmentId;
        entity.Subtotal = dto.Subtotal > 0 ? dto.Subtotal : dto.Amount;
        entity.DiscountPercentage = dto.DiscountPercentage;
        entity.DiscountAmount = dto.DiscountAmount;
        entity.DiscountReason = dto.DiscountReason;
        entity.DiscountAuthorizedBy = dto.DiscountAuthorizedBy;
        entity.Amount = dto.Amount;
        entity.PaidAmount = dto.PaidAmount;
        entity.Status = dto.Status;
        entity.DateIssued = dto.DateIssued;
        entity.PaymentMethod = dto.PaymentMethod;
        entity.Description = dto.Description;
        entity.ClinicId = dto.ClinicId;

        if (dto.Payments != null)
        {
            entity.Payments.Clear();
            entity.Payments.AddRange(dto.Payments.Select(p => new PaymentLog
            {
                Amount = p.Amount, Date = p.Date, PaymentMethod = p.PaymentMethod
            }));
        }

        await _repo.UpdateAsync(entity);

        // Notify Doctor if overdue
        if (entity.Status == "Overdue" && !string.IsNullOrEmpty(entity.AppointmentId))
        {
            var appointment = await _appointmentRepo.GetByIdAsync(entity.AppointmentId);
            if (appointment != null && !string.IsNullOrEmpty(appointment.DoctorId))
            {
                var users = await _userRepo.GetAllAsync();
                var doctorUser = users.FirstOrDefault(u => u.DoctorId == appointment.DoctorId);
                if (doctorUser != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        doctorUser.Id,
                        "Bill Overdue",
                        $"A bill of {entity.Amount:C} for appointment on {appointment.Date} is now overdue.",
                        "Billing"
                    );
                }
            }
        }

        return Ok(new { message = "Success", data = MapToDto(entity) });
    }

    private static BillingRecordDto MapToDto(BillingRecord b) => new()
    {
        Id = b.Id, PatientId = b.PatientId, AppointmentId = b.AppointmentId,
        Subtotal = b.Subtotal,
        DiscountPercentage = b.DiscountPercentage,
        DiscountAmount = b.DiscountAmount,
        DiscountReason = b.DiscountReason,
        DiscountAuthorizedBy = b.DiscountAuthorizedBy,
        Amount = b.Amount, PaidAmount = b.PaidAmount, Status = b.Status,
        DateIssued = b.DateIssued, PaymentMethod = b.PaymentMethod,
        Description = b.Description, ClinicId = b.ClinicId,
        Payments = b.Payments.Select(p => new PaymentLogDto
        {
            Amount = p.Amount, Date = p.Date, PaymentMethod = p.PaymentMethod
        }).ToList()
    };

    private static BillingRecord MapToEntity(BillingRecordDto dto) => new()
    {
        Id = dto.Id, PatientId = dto.PatientId, AppointmentId = dto.AppointmentId,
        Subtotal = dto.Subtotal > 0 ? dto.Subtotal : dto.Amount,
        DiscountPercentage = dto.DiscountPercentage,
        DiscountAmount = dto.DiscountAmount,
        DiscountReason = dto.DiscountReason,
        DiscountAuthorizedBy = dto.DiscountAuthorizedBy,
        Amount = dto.Amount, PaidAmount = dto.PaidAmount, Status = dto.Status,
        DateIssued = dto.DateIssued, PaymentMethod = dto.PaymentMethod,
        Description = dto.Description, ClinicId = dto.ClinicId,
        Payments = (dto.Payments ?? new()).Select(p => new PaymentLog
        {
            Amount = p.Amount, Date = p.Date, PaymentMethod = p.PaymentMethod
        }).ToList()
    };
}
