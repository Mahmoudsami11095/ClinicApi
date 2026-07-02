using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminSettingsController : ControllerBase
{
    private readonly IGenericRepository<PromoCode> _promoRepo;
    private readonly IGenericRepository<SubscriptionSetting> _settingsRepo;
    private readonly IDoctorRepository _doctorRepo;

    public AdminSettingsController(
        IGenericRepository<PromoCode> promoRepo,
        IGenericRepository<SubscriptionSetting> settingsRepo,
        IDoctorRepository doctorRepo)
    {
        _promoRepo = promoRepo;
        _settingsRepo = settingsRepo;
        _doctorRepo = doctorRepo;
    }

    [HttpGet("subscription-settings")]
    public async Task<IActionResult> GetSettings()
    {
        var settingsList = await _settingsRepo.GetAllAsync();
        var settings = settingsList.FirstOrDefault();

        if (settings == null)
        {
            // Seed a default settings record if none exists
            settings = new SubscriptionSetting
            {
                Id = "s_default",
                InitialSetupFee = 100.00m,
                AnnualSubscriptionFee = 300.00m,
                TrialDurationMonths = 6
            };
            await _settingsRepo.AddAsync(settings);
        }

        return Ok(new { data = settings });
    }

    [HttpPut("subscription-settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] SubscriptionSetting request)
    {
        var settingsList = await _settingsRepo.GetAllAsync();
        var settings = settingsList.FirstOrDefault();

        if (settings == null)
        {
            settings = new SubscriptionSetting { Id = "s_default" };
            settings.InitialSetupFee = request.InitialSetupFee;
            settings.AnnualSubscriptionFee = request.AnnualSubscriptionFee;
            settings.TrialDurationMonths = request.TrialDurationMonths;
            await _settingsRepo.AddAsync(settings);
        }
        else
        {
            settings.InitialSetupFee = request.InitialSetupFee;
            settings.AnnualSubscriptionFee = request.AnnualSubscriptionFee;
            settings.TrialDurationMonths = request.TrialDurationMonths;
            await _settingsRepo.UpdateAsync(settings);
        }

        return Ok(new { message = "Subscription settings updated successfully.", data = settings });
    }

    [HttpGet("promos")]
    public async Task<IActionResult> GetPromos()
    {
        var promos = await _promoRepo.GetAllAsync();
        return Ok(new { data = promos });
    }

    [HttpPost("promos")]
    public async Task<IActionResult> CreatePromo([FromBody] PromoCode request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { message = "Promo code string is required." });

        var promos = await _promoRepo.GetAllAsync();
        if (promos.Any(p => string.Equals(p.Code, request.Code, StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { message = "A promo code with this code already exists." });

        var newPromo = new PromoCode
        {
            Id = Guid.NewGuid().ToString(),
            Code = request.Code.ToUpper().Trim(),
            DiscountType = request.DiscountType,
            Value = request.Value,
            ExpiryDate = request.ExpiryDate == default ? DateTime.UtcNow.AddYears(1) : request.ExpiryDate.ToUniversalTime(),
            MaxUses = request.MaxUses <= 0 ? 100 : request.MaxUses,
            CurrentUses = 0,
            IsActive = true
        };

        await _promoRepo.AddAsync(newPromo);
        return Ok(new { message = "Promo code created successfully.", data = newPromo });
    }

    [HttpDelete("promos/{id}")]
    public async Task<IActionResult> DeletePromo(string id)
    {
        var promo = await _promoRepo.GetByIdAsync(id);
        if (promo == null)
            return NotFound(new { message = "Promo code not found." });

        await _promoRepo.DeleteAsync(id);
        return Ok(new { message = "Promo code deleted successfully." });
    }

    [HttpGet("doctors")]
    public async Task<IActionResult> GetDoctorsSubscriptions()
    {
        var doctors = await _doctorRepo.GetAllAsync();
        var result = doctors.Select(d => new {
            id = d.Id,
            name = d.Name,
            email = d.Email,
            subscriptionStatus = d.SubscriptionStatus,
            trialEndDate = d.TrialEndDate,
            subscriptionEndDate = d.SubscriptionEndDate,
            isInitialFeePaid = d.IsInitialFeePaid,
            appliedPromoCode = d.AppliedPromoCode
        });
        return Ok(new { data = result });
    }

    [HttpPost("doctors/{doctorId}/activate")]
    public async Task<IActionResult> ApproveDoctorSubscription(string doctorId)
    {
        var doctor = await _doctorRepo.GetByIdAsync(doctorId);
        if (doctor == null) return NotFound(new { message = "Doctor not found." });

        var settingsList = await _settingsRepo.GetAllAsync();
        var settings = settingsList.FirstOrDefault() ?? new SubscriptionSetting();

        int extraMonths = 0;
        if (!string.IsNullOrEmpty(doctor.AppliedPromoCode))
        {
            var promoList = await _promoRepo.GetAllAsync();
            var promo = promoList.FirstOrDefault(p => string.Equals(p.Code, doctor.AppliedPromoCode, StringComparison.OrdinalIgnoreCase));
            if (promo != null)
            {
                if (promo.DiscountType == "FreeMonths")
                {
                    extraMonths = (int)promo.Value;
                }
            }
        }

        doctor.SubscriptionStatus = "Active";
        doctor.IsInitialFeePaid = true;
        
        var baseDate = doctor.SubscriptionEndDate.HasValue && doctor.SubscriptionEndDate > DateTime.UtcNow 
            ? doctor.SubscriptionEndDate.Value 
            : DateTime.UtcNow;

        doctor.SubscriptionEndDate = baseDate.AddYears(1).AddMonths(extraMonths);
        await _doctorRepo.UpdateAsync(doctor);

        return Ok(new { message = "Doctor subscription approved and activated successfully." });
    }
}
