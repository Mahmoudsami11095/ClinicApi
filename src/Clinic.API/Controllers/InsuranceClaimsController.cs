using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
[Route("api/insurance")]
[Authorize(Roles = "admin,doctor,assistant")]
public class InsuranceClaimsController : ControllerBase
{
    private readonly ClinicDbContext _context;
    private readonly IInsuranceClaimRepository _claimRepo;

    public InsuranceClaimsController(ClinicDbContext context, IInsuranceClaimRepository claimRepo)
    {
        _context = context;
        _claimRepo = claimRepo;
    }

    // ── 1. Insurance Providers Catalog ──
    [HttpGet("providers")]
    public async Task<IActionResult> GetProviders()
    {
        var providers = await _context.InsuranceProviders
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

        // Seed top regional payers if table is currently empty
        if (!providers.Any())
        {
            var seedProviders = new List<InsuranceProvider>
            {
                new() { Name = "Bupa Global / Bupa Egypt", PayerCode = "BUPA-EGY-01", PreAuthThreshold = 2000m, ContactEmail = "claims@bupa.com.eg", ContactPhone = "+20224003300" },
                new() { Name = "AXA OneHealth Insurance", PayerCode = "AXA-EGY-02", PreAuthThreshold = 1500m, ContactEmail = "approvals@axa-onehealth.com", ContactPhone = "+20221228800" },
                new() { Name = "MetLife Alico Dental", PayerCode = "METLIFE-03", PreAuthThreshold = 1800m, ContactEmail = "dental-claims@metlife.eg", ContactPhone = "+20227989900" },
                new() { Name = "NextCare TPA Network", PayerCode = "NEXTCARE-04", PreAuthThreshold = 1200m, ContactEmail = "tpa@nextcare.com.eg", ContactPhone = "+20226901122" },
                new() { Name = "Misr Healthcare Network", PayerCode = "MISR-HLTH-05", PreAuthThreshold = 1000m, ContactEmail = "claims@misr-health.com", ContactPhone = "+20223945566" }
            };

            await _context.InsuranceProviders.AddRangeAsync(seedProviders);
            await _context.SaveChangesAsync();
            providers = seedProviders;
        }

        return Ok(new { data = providers.Select(p => new InsuranceProviderDto
        {
            Id = p.Id,
            Name = p.Name,
            PayerCode = p.PayerCode,
            PreAuthThreshold = p.PreAuthThreshold,
            ContactEmail = p.ContactEmail,
            ContactPhone = p.ContactPhone,
            IsActive = p.IsActive
        }) });
    }

    [HttpPost("providers")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateProvider([FromBody] CreateInsuranceProviderDto dto)
    {
        var provider = new InsuranceProvider
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            PayerCode = dto.PayerCode.ToUpper().Trim(),
            PreAuthThreshold = dto.PreAuthThreshold,
            ContactEmail = dto.ContactEmail,
            ContactPhone = dto.ContactPhone,
            IsActive = true
        };

        await _context.InsuranceProviders.AddAsync(provider);
        await _context.SaveChangesAsync();

        return Created($"/api/insurance/providers/{provider.Id}", new { data = provider });
    }

    // ── 2. Insurance Claims Lifecycle ──
    [HttpPost("claims")]
    public async Task<IActionResult> CreateClaim([FromBody] CreateInsuranceClaimDto dto)
    {
        var provider = await _context.InsuranceProviders.FindAsync(dto.InsuranceProviderId);
        if (provider == null)
            return NotFound(new { message = "Selected insurance provider not found." });

        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null)
            return NotFound(new { message = "Patient record not found." });

        var clinic = await _context.Clinics.FindAsync(dto.ClinicId);
        if (clinic == null)
            return NotFound(new { message = "Clinic facility not found." });

        // BR-INS-01: Calculate Copay & Claim split
        var copayAmount = Math.Round(dto.TotalGrossAmount * (dto.CopayPercentage / 100m), 2);
        var claimedAmount = Math.Round(dto.TotalGrossAmount - copayAmount, 2);

        // BR-INS-02: Check pre-authorization requirement
        var requiresPreAuth = dto.TotalGrossAmount >= provider.PreAuthThreshold;
        var initialStatus = requiresPreAuth ? "PreAuthorized" : "Draft";

        var claimNumber = await _claimRepo.GetNextClaimNumberAsync();

        var claim = new InsuranceClaim
        {
            Id = Guid.NewGuid().ToString(),
            ClaimNumber = claimNumber,
            ClinicId = dto.ClinicId,
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            InsuranceProviderId = dto.InsuranceProviderId,
            PolicyNumber = dto.PolicyNumber,
            MemberId = dto.MemberId,
            ToothNumber = dto.ToothNumber,
            DiagnosisCode = dto.DiagnosisCode,
            ProcedureDescription = dto.ProcedureDescription,
            TotalGrossAmount = dto.TotalGrossAmount,
            CopayPercentage = dto.CopayPercentage,
            PatientCopayAmount = copayAmount,
            ClaimedAmount = claimedAmount,
            ApprovedAmount = requiresPreAuth ? null : claimedAmount,
            Status = initialStatus,
            PreAuthNotes = dto.PreAuthNotes,
            ClaimFileUrls = dto.ClaimFileUrls != null ? JsonSerializer.Serialize(dto.ClaimFileUrls) : "[]",
            CreatedAt = DateTime.UtcNow
        };

        await _claimRepo.AddAsync(claim);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = claim.Id }, MapToDto(claim, clinic.Name, $"{patient.FirstName} {patient.LastName}", "Attending Doctor", provider.Name));
    }

    [HttpGet("claims")]
    public async Task<IActionResult> GetClaims([FromQuery] string? clinicId, [FromQuery] string? status, [FromQuery] string? patientId)
    {
        var query = _context.InsuranceClaims
            .Include(c => c.Clinic)
            .Include(c => c.Patient)
            .Include(c => c.Doctor)
            .Include(c => c.InsuranceProvider)
            .Where(c => !c.IsDeleted);

        if (!string.IsNullOrEmpty(clinicId) && clinicId != "all")
            query = query.Where(c => c.ClinicId == clinicId);

        if (!string.IsNullOrEmpty(status) && status != "all")
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrEmpty(patientId))
            query = query.Where(c => c.PatientId == patientId);

        var list = await query.OrderByDescending(c => c.CreatedAt).AsNoTracking().ToListAsync();

        return Ok(new { data = list.Select(c => MapToDto(c, c.Clinic?.Name, $"{c.Patient?.FirstName} {c.Patient?.LastName}", $"{c.Doctor?.FirstName} {c.Doctor?.LastName}", c.InsuranceProvider?.Name)) });
    }

    [HttpGet("claims/summary")]
    public async Task<IActionResult> GetClaimsSummary([FromQuery] string? clinicId)
    {
        var query = _context.InsuranceClaims.AsNoTracking().Where(c => !c.IsDeleted);
        if (!string.IsNullOrEmpty(clinicId) && clinicId != "all")
            query = query.Where(c => c.ClinicId == clinicId);

        var claims = await query.ToListAsync();

        var summary = new InsuranceClaimsSummaryDto
        {
            TotalClaimsCount = claims.Count,
            TotalClaimedAmount = claims.Sum(c => c.ClaimedAmount),
            TotalApprovedAmount = claims.Where(c => c.Status == "Approved" || c.Status == "Settled").Sum(c => c.ApprovedAmount ?? c.ClaimedAmount),
            PendingPreAuthCount = claims.Count(c => c.Status == "PreAuthorized"),
            RejectionCount = claims.Count(c => c.Status == "Rejected")
        };

        return Ok(new { data = summary });
    }

    [HttpGet("claims/{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var claim = await _claimRepo.GetByIdAsync(id);
        if (claim == null)
            return NotFound(new { message = "Insurance claim record not found." });

        return Ok(new { data = MapToDto(claim, claim.Clinic?.Name, $"{claim.Patient?.FirstName} {claim.Patient?.LastName}", $"{claim.Doctor?.FirstName} {claim.Doctor?.LastName}", claim.InsuranceProvider?.Name) });
    }

    [HttpPut("claims/{id}/submit")]
    public async Task<IActionResult> SubmitClaim(string id)
    {
        var claim = await _claimRepo.GetByIdAsync(id);
        if (claim == null)
            return NotFound(new { message = "Claim not found." });

        if (claim.Status != "Draft" && claim.Status != "PreAuthorized")
            return BadRequest(new { message = $"Cannot submit claim in status '{claim.Status}'." });

        claim.Status = "Submitted";
        claim.SubmittedAt = DateTime.UtcNow;

        await _claimRepo.UpdateAsync(claim);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Claim submitted to insurance payer for electronic adjudication.", data = MapToDto(claim, claim.Clinic?.Name, $"{claim.Patient?.FirstName} {claim.Patient?.LastName}", $"{claim.Doctor?.FirstName} {claim.Doctor?.LastName}", claim.InsuranceProvider?.Name) });
    }

    [HttpPut("claims/{id}/adjudicate")]
    public async Task<IActionResult> AdjudicateClaim(string id, [FromBody] AdjudicateClaimDto dto)
    {
        var claim = await _claimRepo.GetByIdAsync(id);
        if (claim == null)
            return NotFound(new { message = "Claim not found." });

        claim.Status = dto.Status;
        claim.AdjudicatedAt = DateTime.UtcNow;
        claim.AdjudicationNotes = dto.AdjudicationNotes;
        claim.RejectionReason = dto.RejectionReason;

        if (dto.Status == "Approved")
        {
            claim.ApprovedAmount = dto.ApprovedAmount ?? claim.ClaimedAmount;
        }
        else if (dto.Status == "PartiallyApproved")
        {
            claim.ApprovedAmount = dto.ApprovedAmount ?? (claim.ClaimedAmount * 0.8m);
        }
        else if (dto.Status == "Rejected")
        {
            claim.ApprovedAmount = 0m;
        }

        await _claimRepo.UpdateAsync(claim);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Claim status updated to '{claim.Status}'.", data = MapToDto(claim, claim.Clinic?.Name, $"{claim.Patient?.FirstName} {claim.Patient?.LastName}", $"{claim.Doctor?.FirstName} {claim.Doctor?.LastName}", claim.InsuranceProvider?.Name) });
    }

    [HttpPut("claims/{id}/settle")]
    public async Task<IActionResult> SettleClaim(string id, [FromBody] JsonElement payload)
    {
        var claim = await _claimRepo.GetByIdAsync(id);
        if (claim == null)
            return NotFound(new { message = "Claim not found." });

        // BR-INS-03: Only Approved or PartiallyApproved claims can be settled
        if (claim.Status != "Approved" && claim.Status != "PartiallyApproved")
            return BadRequest(new { message = "BR-INS-03: Only approved or partially approved claims can be marked settled." });

        claim.Status = "Settled";
        claim.SettledAt = DateTime.UtcNow;

        await _claimRepo.UpdateAsync(claim);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Insurance remittance recorded. Claim is settled.", data = MapToDto(claim, claim.Clinic?.Name, $"{claim.Patient?.FirstName} {claim.Patient?.LastName}", $"{claim.Doctor?.FirstName} {claim.Doctor?.LastName}", claim.InsuranceProvider?.Name) });
    }

    private static InsuranceClaimResponseDto MapToDto(
        InsuranceClaim c,
        string? clinicName,
        string? patientName,
        string? doctorName,
        string? providerName)
    {
        List<string> files = new();
        if (!string.IsNullOrEmpty(c.ClaimFileUrls))
        {
            try { files = JsonSerializer.Deserialize<List<string>>(c.ClaimFileUrls) ?? new(); }
            catch { files = new(); }
        }

        return new InsuranceClaimResponseDto
        {
            Id = c.Id,
            ClaimNumber = c.ClaimNumber,
            ClinicId = c.ClinicId,
            ClinicName = clinicName ?? c.ClinicId,
            PatientId = c.PatientId,
            PatientName = patientName ?? c.PatientId,
            DoctorId = c.DoctorId,
            DoctorName = doctorName ?? c.DoctorId,
            InsuranceProviderId = c.InsuranceProviderId,
            InsuranceProviderName = providerName ?? c.InsuranceProviderId,
            PolicyNumber = c.PolicyNumber,
            MemberId = c.MemberId,
            ToothNumber = c.ToothNumber,
            DiagnosisCode = c.DiagnosisCode,
            ProcedureDescription = c.ProcedureDescription,
            TotalGrossAmount = c.TotalGrossAmount,
            CopayPercentage = c.CopayPercentage,
            PatientCopayAmount = c.PatientCopayAmount,
            ClaimedAmount = c.ClaimedAmount,
            ApprovedAmount = c.ApprovedAmount,
            Status = c.Status,
            PreAuthNotes = c.PreAuthNotes,
            AdjudicationNotes = c.AdjudicationNotes,
            RejectionReason = c.RejectionReason,
            ClaimFileUrls = files,
            CreatedAt = c.CreatedAt,
            SubmittedAt = c.SubmittedAt,
            AdjudicatedAt = c.AdjudicatedAt,
            SettledAt = c.SettledAt
        };
    }
}
