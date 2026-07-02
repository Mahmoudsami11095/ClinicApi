using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
    private readonly IGenericRepository<SubscriptionReceipt> _receiptRepo;
    private readonly IWebHostEnvironment _env;

    public SubscriptionsController(
        IDoctorRepository doctorRepo,
        IGenericRepository<PromoCode> promoRepo,
        IGenericRepository<SubscriptionSetting> settingsRepo,
        IGenericRepository<SubscriptionReceipt> receiptRepo,
        IWebHostEnvironment env)
    {
        _doctorRepo = doctorRepo;
        _promoRepo = promoRepo;
        _settingsRepo = settingsRepo;
        _receiptRepo = receiptRepo;
        _env = env;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var doctorId = User.FindFirst("DoctorId")?.Value;
        if (string.IsNullOrEmpty(doctorId))
        {
            // Fallback: check if role is doctor, retrieve details
            var roleStr = User.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.Equals(roleStr, "doctor", StringComparison.OrdinalIgnoreCase))
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

        var doctor = await GetCurrentDoctorAsync();
        if (doctor == null)
            return NotFound(new { message = "Doctor record not found." });

        var promoList = await _promoRepo.GetAllAsync();
        var promo = promoList.FirstOrDefault(p => string.Equals(p.Code, request.Code, StringComparison.OrdinalIgnoreCase));

        if (promo == null || !promo.IsActive || promo.ExpiryDate < DateTime.UtcNow || promo.CurrentUses >= promo.MaxUses)
            return BadRequest(new { message = "Invalid, expired, or fully used promo code." });

        var settingsList = await _settingsRepo.GetAllAsync();
        var settings = settingsList.FirstOrDefault() ?? new SubscriptionSetting();

        decimal initialFee = doctor.IsInitialFeePaid ? 0 : settings.InitialSetupFee;
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
            originalSetupFee = settings.InitialSetupFee,
            setupFeeToPay = initialFee,
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

        // Request manual activation / approval
        doctor.SubscriptionStatus = "PendingApproval";
        doctor.IsInitialFeePaid = true;

        await _doctorRepo.UpdateAsync(doctor);

        return Ok(new
        {
            message = "Subscription payment submitted. Pending administrator approval.",
            subscriptionStatus = doctor.SubscriptionStatus,
            subscriptionEndDate = doctor.SubscriptionEndDate,
            isInitialFeePaid = doctor.IsInitialFeePaid
        });
    }

    [HttpPost("upload-receipt")]
    public async Task<IActionResult> UploadReceipt([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Receipt file is required." });

        var doctor = await GetCurrentDoctorAsync();
        if (doctor == null)
            return NotFound(new { message = "Doctor record not found." });

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
        {
            webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }
        var uploadsFolder = Path.Combine(webRoot, "receipts");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var fileName = $"{doctor.Id}_{DateTime.UtcNow.Ticks}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        doctor.ReceiptUrl = $"/receipts/{fileName}";
        doctor.SubscriptionStatus = "PendingApproval";
        doctor.IsInitialFeePaid = true;

        await _doctorRepo.UpdateAsync(doctor);

        var receipt = new SubscriptionReceipt
        {
            DoctorId = doctor.Id,
            ReceiptUrl = doctor.ReceiptUrl,
            UploadedAt = DateTime.UtcNow,
            Status = "PendingApproval"
        };
        await _receiptRepo.AddAsync(receipt);

        return Ok(new
        {
            message = "Receipt uploaded successfully. Awaiting administrator approval.",
            receiptUrl = doctor.ReceiptUrl,
            subscriptionStatus = doctor.SubscriptionStatus,
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
