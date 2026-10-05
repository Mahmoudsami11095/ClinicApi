using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin,doctor,assistant,receptionist")]
public class CommissionController : ControllerBase
{
    private readonly ICommissionService _commissionService;

    public CommissionController(ICommissionService commissionService)
    {
        _commissionService = commissionService;
    }

    /// <summary>
    /// REQ-COMM-01: Real-time Doctor Commission & Profit-Sharing Analytics
    /// </summary>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] string? clinicId,
        [FromQuery] string? doctorId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken ct)
    {
        var analytics = await _commissionService.GetAnalyticsAsync(clinicId, doctorId, startDate, endDate, ct);
        return Ok(analytics);
    }

    /// <summary>
    /// REQ-COMM-02: Get Doctor Commission Plan
    /// </summary>
    [HttpGet("plans/doctor/{doctorId}")]
    public async Task<IActionResult> GetDoctorPlan(string doctorId, [FromQuery] string? clinicId, CancellationToken ct)
    {
        var plan = await _commissionService.GetPlanByDoctorIdAsync(doctorId, clinicId, ct);
        if (plan == null) return NotFound(new { message = "Doctor or plan not found" });
        return Ok(plan);
    }

    /// <summary>
    /// REQ-COMM-02: Upsert Doctor Commission Plan
    /// </summary>
    [HttpPost("plans")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpsertPlan([FromBody] CreateOrUpdateCommissionPlanDto dto, CancellationToken ct)
    {
        if (dto.DefaultCommissionRate < 0 || dto.DefaultCommissionRate > 100)
        {
            return BadRequest(new { message = "Default commission rate must be between 0% and 100%" });
        }

        var plan = await _commissionService.UpsertPlanAsync(dto, ct);
        return Ok(plan);
    }

    /// <summary>
    /// REQ-COMM-03: Get Payout History Ledger
    /// </summary>
    [HttpGet("payouts")]
    public async Task<IActionResult> GetPayouts([FromQuery] string? clinicId, [FromQuery] string? doctorId, CancellationToken ct)
    {
        var payouts = await _commissionService.GetPayoutsAsync(clinicId, doctorId, ct);
        return Ok(payouts);
    }

    /// <summary>
    /// REQ-COMM-03: Generate Payout Record
    /// </summary>
    [HttpPost("payouts")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreatePayout([FromBody] CreateCommissionPayoutDto dto, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(dto.DoctorId))
        {
            return BadRequest(new { message = "DoctorId is required" });
        }

        var payout = await _commissionService.CreatePayoutAsync(dto, ct);
        return CreatedAtAction(nameof(GetPayouts), new { doctorId = payout.DoctorId }, payout);
    }

    /// <summary>
    /// REQ-COMM-03: Settle Commission Payout
    /// </summary>
    [HttpPut("payouts/{id}/settle")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> SettlePayout(string id, [FromBody] SettleCommissionPayoutDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.PaymentReference))
        {
            return BadRequest(new { message = "Payment reference is required to settle payout" });
        }

        var settled = await _commissionService.SettlePayoutAsync(id, dto, ct);
        if (settled == null) return NotFound(new { message = "Payout record not found" });
        return Ok(settled);
    }
}
