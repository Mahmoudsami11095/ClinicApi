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

    public BillingController(IBillingRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var records = await _repo.GetAllAsync();
        var dtos = records.Select(MapToDto).ToList();
        return Ok(new { data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BillingRecordDto dto)
    {
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

        entity.PatientId = dto.PatientId;
        entity.AppointmentId = dto.AppointmentId;
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
        return Ok(new { message = "Success", data = MapToDto(entity) });
    }

    private static BillingRecordDto MapToDto(BillingRecord b) => new()
    {
        Id = b.Id, PatientId = b.PatientId, AppointmentId = b.AppointmentId,
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
        Amount = dto.Amount, PaidAmount = dto.PaidAmount, Status = dto.Status,
        DateIssued = dto.DateIssued, PaymentMethod = dto.PaymentMethod,
        Description = dto.Description, ClinicId = dto.ClinicId,
        Payments = (dto.Payments ?? new()).Select(p => new PaymentLog
        {
            Amount = p.Amount, Date = p.Date, PaymentMethod = p.PaymentMethod
        }).ToList()
    };
}
