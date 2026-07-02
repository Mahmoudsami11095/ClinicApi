using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminSettingsController : ControllerBase
{
    private readonly IGenericRepository<PromoCode> _promoRepo;
    private readonly IGenericRepository<SubscriptionSetting> _settingsRepo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly IGenericRepository<SubscriptionReceipt> _receiptRepo;
    private readonly IUserRepository _userRepo;
    private readonly IGenericRepository<Patient> _patientRepo;
    private readonly IWebHostEnvironment _env;

    public AdminSettingsController(
        IGenericRepository<PromoCode> promoRepo,
        IGenericRepository<SubscriptionSetting> settingsRepo,
        IDoctorRepository doctorRepo,
        IGenericRepository<SubscriptionReceipt> receiptRepo,
        IUserRepository userRepo,
        IGenericRepository<Patient> patientRepo,
        IWebHostEnvironment env)
    {
        _promoRepo = promoRepo;
        _settingsRepo = settingsRepo;
        _doctorRepo = doctorRepo;
        _receiptRepo = receiptRepo;
        _userRepo = userRepo;
        _patientRepo = patientRepo;
        _env = env;
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

    [HttpPost("accounts/{email}/soft-delete")]
    public async Task<IActionResult> SoftDeleteAccount(string email)
    {
        var currentUserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.Equals(currentUserEmail, email, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "You cannot delete your own admin account." });

        var user = await _userRepo.GetByEmailAsync(email);

        if (user != null)
        {
            // User record exists — soft-delete user and linked doctor/patient
            user.IsDeleted = true;
            await _userRepo.UpdateAsync(user);

            if (!string.IsNullOrEmpty(user.DoctorId))
            {
                var doctor = await _doctorRepo.GetByIdAsync(user.DoctorId);
                if (doctor != null)
                {
                    doctor.IsDeleted = true;
                    await _doctorRepo.UpdateAsync(doctor);
                }
            }
            
            if (!string.IsNullOrEmpty(user.PatientId))
            {
                var patient = await _patientRepo.GetByIdAsync(user.PatientId);
                if (patient != null)
                {
                    patient.IsDeleted = true;
                    await _patientRepo.UpdateAsync(patient);
                }
            }

            return Ok(new { message = "Account successfully deactivated." });
        }

        // No User record found — check if a Doctor exists directly by email
        var doctors = await _doctorRepo.GetAllAsync();
        var directDoctor = doctors.FirstOrDefault(d => string.Equals(d.Email, email, StringComparison.OrdinalIgnoreCase));
        if (directDoctor != null)
        {
            directDoctor.IsDeleted = true;
            await _doctorRepo.UpdateAsync(directDoctor);
            return Ok(new { message = "Doctor account successfully deactivated." });
        }

        // No User or Doctor found — check patients by email
        var patients = await _patientRepo.GetAllAsync();
        var directPatient = patients.FirstOrDefault(p => string.Equals(p.Email, email, StringComparison.OrdinalIgnoreCase));
        if (directPatient != null)
        {
            directPatient.IsDeleted = true;
            await _patientRepo.UpdateAsync(directPatient);
            return Ok(new { message = "Patient account successfully deactivated." });
        }

        return NotFound(new { message = "No account found with this email." });
    }

    [HttpGet("doctors")]
    public async Task<IActionResult> GetDoctorsSubscriptions()
    {
        var doctors = await _doctorRepo.GetAllAsync();
        var receiptsList = await _receiptRepo.GetAllAsync();

        var result = doctors.Where(d => !d.IsDeleted).Select(d => new {
            id = d.Id,
            name = d.FirstName + " " + d.LastName,
            email = d.Email,
            subscriptionStatus = d.SubscriptionStatus,
            trialEndDate = d.TrialEndDate,
            subscriptionEndDate = d.SubscriptionEndDate,
            isInitialFeePaid = d.IsInitialFeePaid,
            appliedPromoCode = d.AppliedPromoCode,
            receiptUrl = d.ReceiptUrl,
            receipts = receiptsList.Where(r => r.DoctorId == d.Id)
                                   .OrderByDescending(r => r.UploadedAt)
                                   .Select(r => new {
                                       id = r.Id,
                                       receiptUrl = r.ReceiptUrl,
                                       uploadedAt = r.UploadedAt,
                                       status = r.Status
                                   })
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

        // Approve all pending receipts for this doctor
        var receipts = await _receiptRepo.GetAllAsync();
        var pendingReceipts = receipts.Where(r => r.DoctorId == doctor.Id && r.Status == "PendingApproval");
        foreach (var r in pendingReceipts)
        {
            r.Status = "Approved";
            await _receiptRepo.UpdateAsync(r);
        }

        return Ok(new { message = "Doctor subscription approved and activated successfully." });
    }

    [HttpPost("doctors/{doctorId}/deactivate")]
    public async Task<IActionResult> DeactivateDoctorSubscription(string doctorId)
    {
        var doctor = await _doctorRepo.GetByIdAsync(doctorId);
        if (doctor == null) return NotFound(new { message = "Doctor not found." });

        doctor.SubscriptionStatus = "Suspended";
        await _doctorRepo.UpdateAsync(doctor);

        return Ok(new { message = "Doctor subscription deactivated/suspended successfully." });
    }

    [HttpPost("receipts/{receiptId}/approve")]
    public async Task<IActionResult> ApproveReceipt(string receiptId)
    {
        var receipt = await _receiptRepo.GetByIdAsync(receiptId);
        if (receipt == null) return NotFound(new { message = "Receipt not found." });

        receipt.Status = "Approved";
        await _receiptRepo.UpdateAsync(receipt);

        var doctor = await _doctorRepo.GetByIdAsync(receipt.DoctorId);
        if (doctor != null)
        {
            int extraMonths = 0;
            if (!string.IsNullOrEmpty(doctor.AppliedPromoCode))
            {
                var promoList = await _promoRepo.GetAllAsync();
                var promo = promoList.FirstOrDefault(p => string.Equals(p.Code, doctor.AppliedPromoCode, StringComparison.OrdinalIgnoreCase));
                if (promo != null && promo.DiscountType == "FreeMonths")
                {
                    extraMonths = (int)promo.Value;
                }
            }

            doctor.SubscriptionStatus = "Active";
            doctor.IsInitialFeePaid = true;

            var baseDate = doctor.SubscriptionEndDate.HasValue && doctor.SubscriptionEndDate > DateTime.UtcNow 
                ? doctor.SubscriptionEndDate.Value 
                : DateTime.UtcNow;

            doctor.SubscriptionEndDate = baseDate.AddYears(1).AddMonths(extraMonths);
            await _doctorRepo.UpdateAsync(doctor);
        }

        return Ok(new { message = "Receipt approved and doctor subscription activated successfully." });
    }

    [HttpPost("receipts/{receiptId}/reject")]
    public async Task<IActionResult> RejectReceipt(string receiptId)
    {
        var receipt = await _receiptRepo.GetByIdAsync(receiptId);
        if (receipt == null) return NotFound(new { message = "Receipt not found." });

        receipt.Status = "Rejected";
        await _receiptRepo.UpdateAsync(receipt);

        var doctor = await _doctorRepo.GetByIdAsync(receipt.DoctorId);
        if (doctor != null)
        {
            bool isTrialActive = doctor.TrialEndDate > DateTime.UtcNow;
            doctor.SubscriptionStatus = isTrialActive ? "Trial" : "Expired";

            await _doctorRepo.UpdateAsync(doctor);
        }

        return Ok(new { message = "Receipt rejected successfully." });
    }

    [HttpDelete("accounts/{email}")]
    public async Task<IActionResult> DeleteAccount(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email is required." });

        var currentUserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.Equals(currentUserEmail, email, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "You cannot delete your own admin account." });

        var contentRoot = _env.ContentRootPath;
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
        {
            webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var user = await _userRepo.GetByEmailAsync(email);
        if (user != null)
        {
            try
            {
                await _userRepo.DeleteUserWithRelatedDataAsync(user.Id, contentRoot, webRoot);
                return Ok(new { message = "Account and all associated records deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the account.", error = ex.Message });
            }
        }

        // No User record — try deleting a Doctor directly by email
        var doctors = await _doctorRepo.GetAllAsync();
        var doctor = doctors.FirstOrDefault(d => string.Equals(d.Email, email, StringComparison.OrdinalIgnoreCase));
        if (doctor != null)
        {
            try
            {
                await _doctorRepo.DeleteAsync(doctor.Id);
                return Ok(new { message = "Doctor account and all associated records deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the doctor.", error = ex.Message });
            }
        }

        // No User or Doctor — try deleting a Patient directly by email
        var patients = await _patientRepo.GetAllAsync();
        var patient = patients.FirstOrDefault(p => string.Equals(p.Email, email, StringComparison.OrdinalIgnoreCase));
        if (patient != null)
        {
            try
            {
                await _patientRepo.DeleteAsync(patient.Id);
                return Ok(new { message = "Patient account and all associated records deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the patient.", error = ex.Message });
            }
        }

        return NotFound(new { message = "No account found with this email." });
    }
}
