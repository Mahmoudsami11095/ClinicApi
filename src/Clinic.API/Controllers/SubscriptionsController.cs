using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly IDoctorRepository _doctorRepo;
    private readonly IGenericRepository<PromoCode> _promoRepo;
    private readonly IGenericRepository<SubscriptionSetting> _settingsRepo;

    public SubscriptionsController(
        IDoctorRepository doctorRepo,
        IGenericRepository<PromoCode> promoRepo,
        IGenericRepository<SubscriptionSetting> settingsRepo)
    {
        _doctorRepo = doctorRepo;
        _promoRepo = promoRepo;
        _settingsRepo = settingsRepo;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var doctorId = User.FindFirst("DoctorId")?.Value;
        if (string.IsNullOrEmpty(doctorId))
        {
            // Fallback: check if role is doctor, retrieve details
            var roleStr = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleStr != "Doctor")
                return BadRequest(new { message = "Only doctor accounts have subscription details." });
        }

        // Fetch doctor
        var doctor = await GetCurrentDoctorAsync();
        if (doctor == null)
            return NotFound(new { message = "Doctor record not found." });

        var settingsList = await _settingsRepo.GetAllAsync();
        var settings = settingsList.FirstOrDefault() ?? new SubscriptionSetting();

        return Ok(new
        {
            subscriptionStatus = doctor.SubscriptionStatus,
            trialEndDate = doctor.TrialEndDate,
            subscriptionEndDate = doctor.SubscriptionEndDate,
            isInitialFeePaid = doctor.IsInitialFeePaid,
            appliedPromoCode = doctor.AppliedPromoCode,
            pricing = new
            {
                initialSetupFee = settings.InitialSetupFee,
                annualSubscriptionFee = settings.AnnualSubscriptionFee
            }
        });
    }

    [HttpPost("validate-promo")]
    public async Task<IActionResult> ValidatePromo([FromBody] PromoValidationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { message = "Promo code is required." });

        var promoList = await _promoRepo.GetAllAsync();
        var promo = promoList.FirstOrDefault(p => string.Equals(p.Code, request.Code, StringComparison.OrdinalIgnoreCase));

        if (promo == null || !promo.IsActive || promo.ExpiryDate < DateTime.UtcNow || promo.CurrentUses >= promo.MaxUses)
            return BadRequest(new { message = "Invalid, expired, or fully used promo code." });

        var settingsList = await _settingsRepo.GetAllAsync();
        var settings = settingsList.FirstOrDefault() ?? new SubscriptionSetting();

        decimal initialFee = settings.InitialSetupFee;
        decimal annualFee = settings.AnnualSubscriptionFee;
        decimal discountAmount = 0;
        int extraMonths = 0;

        if (promo.DiscountType == "Percent")
        {
            discountAmount = annualFee * (decimal)(promo.Value / 100.0);
        }
        else if (promo.DiscountType == "Flat")
        {
            discountAmount = (decimal)promo.Value;
        }
        else if (promo.DiscountType == "FreeMonths")
        {
            extraMonths = (int)promo.Value;
        }

        decimal finalAnnualFee = Math.Max(0, annualFee - discountAmount);

        return Ok(new
        {
            valid = true,
            code = promo.Code,
            discountType = promo.DiscountType,
            value = promo.Value,
            originalSetupFee = initialFee,
            originalAnnualFee = annualFee,
            discountAmount,
            finalAnnualFee,
            extraMonths,
            totalDue = initialFee + finalAnnualFee
        });
    }

    [HttpPost("activate-manual")]
    public async Task<IActionResult> ActivateManual([FromBody] PromoValidationRequest request)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor == null)
            return NotFound(new { message = "Doctor record not found." });

        var settingsList = await _settingsRepo.GetAllAsync();
        var settings = settingsList.FirstOrDefault() ?? new SubscriptionSetting();

        int extraMonths = 0;
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var promoList = await _promoRepo.GetAllAsync();
            var promo = promoList.FirstOrDefault(p => string.Equals(p.Code, request.Code, StringComparison.OrdinalIgnoreCase));

            if (promo != null && promo.IsActive && promo.ExpiryDate >= DateTime.UtcNow && promo.CurrentUses < promo.MaxUses)
            {
                doctor.AppliedPromoCode = promo.Code;
                if (promo.DiscountType == "FreeMonths")
                {
                    extraMonths = (int)promo.Value;
                }
                promo.CurrentUses++;
                await _promoRepo.UpdateAsync(promo);
            }
        }

        // Perform manual activation
        doctor.SubscriptionStatus = "Active";
        doctor.IsInitialFeePaid = true;

        // Reset subscription expiration date
        var baseDate = doctor.SubscriptionEndDate.HasValue && doctor.SubscriptionEndDate > DateTime.UtcNow 
            ? doctor.SubscriptionEndDate.Value 
            : DateTime.UtcNow;

        doctor.SubscriptionEndDate = baseDate.AddYears(1).AddMonths(extraMonths);
        await _doctorRepo.UpdateAsync(doctor);

        return Ok(new
        {
            message = "Subscription manually activated successfully.",
            subscriptionStatus = doctor.SubscriptionStatus,
            subscriptionEndDate = doctor.SubscriptionEndDate,
            isInitialFeePaid = doctor.IsInitialFeePaid
        });
    }

    private async Task<Doctor?> GetCurrentDoctorAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return null;

        var doctors = await _doctorRepo.GetAllAsync();
        // Since User entity holds DoctorId, we check the doctor connected to the logged in email or Id
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        return doctors.FirstOrDefault(d => string.Equals(d.Email, email, StringComparison.OrdinalIgnoreCase));
    }
}

public class PromoValidationRequest
{
    public string? Code { get; set; }
}
