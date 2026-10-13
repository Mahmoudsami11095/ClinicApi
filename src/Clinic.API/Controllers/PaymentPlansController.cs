using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clinic.Application.Common;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/payment-plans")]
[Authorize]
public class PaymentPlansController : ControllerBase
{
    private readonly IPaymentPlanRepository _planRepo;
    private readonly IClinicRepository _clinicRepo;
    private readonly IPatientRepository _patientRepo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly IWhatsAppNotificationService _whatsAppService;
    private readonly ClinicDbContext _context;

    public PaymentPlansController(
        IPaymentPlanRepository planRepo,
        IClinicRepository clinicRepo,
        IPatientRepository patientRepo,
        IDoctorRepository doctorRepo,
        IWhatsAppNotificationService whatsAppService,
        ClinicDbContext context)
    {
        _planRepo = planRepo;
        _clinicRepo = clinicRepo;
        _patientRepo = patientRepo;
        _doctorRepo = doctorRepo;
        _whatsAppService = whatsAppService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPlans([FromQuery] string? clinicId, [FromQuery] string? status, [FromQuery] string? search)
    {
        var targetClinicId = clinicId;
        var userClinicId = User.GetClinicId();
        var userDoctorId = User.GetDoctorId();

        if (!string.IsNullOrEmpty(userClinicId))
        {
            targetClinicId = userClinicId;
        }

        var query = _context.PaymentPlans
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Clinic)
            .Include(p => p.Installments.OrderBy(i => i.InstallmentNumber))
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrEmpty(targetClinicId) && targetClinicId != "all")
        {
            query = query.Where(p => p.ClinicId == targetClinicId);
        }

        if (!string.IsNullOrEmpty(userDoctorId) && string.IsNullOrEmpty(userClinicId) && !User.IsInRole("admin"))
        {
            var allowedClinics = await _clinicRepo.GetAllowedClinicIdsForDoctorAsync(userDoctorId);
            query = query.Where(p => allowedClinics.Contains(p.ClinicId));
        }

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            query = query.Where(p => p.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => 
                p.PlanNumber.ToLower().Contains(s) ||
                p.Title.ToLower().Contains(s) ||
                (p.Patient != null && (p.Patient.FirstName.ToLower().Contains(s) || p.Patient.LastName.ToLower().Contains(s) || p.Patient.PhoneNumber.Contains(s)))
            );
        }

        var plans = await query
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        // Evaluate overdue installments dynamically
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var dtos = plans.Select(p => MapToDto(p, today)).ToList();

        return Ok(new { data = dtos });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var plan = await _planRepo.GetByIdAsync(id);
        if (plan == null)
        {
            return NotFound(new { message = "Payment plan not found" });
        }

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return Ok(new { data = MapToDto(plan, today) });
    }

    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetByPatient(string patientId)
    {
        var plans = await _planRepo.GetByPatientAsync(patientId);
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return Ok(new { data = plans.Select(p => MapToDto(p, today)).ToList() });
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] string? clinicId)
    {
        var targetClinicId = clinicId;
        var userClinicId = User.GetClinicId();
        if (!string.IsNullOrEmpty(userClinicId))
        {
            targetClinicId = userClinicId;
        }

        var query = _context.PaymentPlans
            .Include(p => p.Installments)
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrEmpty(targetClinicId) && targetClinicId != "all")
        {
            query = query.Where(p => p.ClinicId == targetClinicId);
        }

        var plans = await query.AsNoTracking().ToListAsync();
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        decimal totalFinanced = 0.0m;
        decimal totalCollected = 0.0m;
        decimal totalOverdue = 0.0m;

        foreach (var p in plans)
        {
            totalFinanced += p.TotalPayable;
            totalCollected += p.DownPayment;

            foreach (var inst in p.Installments)
            {
                totalCollected += inst.AmountPaid;
                var remaining = inst.AmountDue - inst.AmountPaid;

                if (remaining > 0 && string.Compare(inst.DueDate, today, StringComparison.Ordinal) < 0)
                {
                    totalOverdue += remaining;
                }
            }
        }

        var totalOutstanding = Math.Max(0.0m, totalFinanced - totalCollected);
        var collectionRate = totalFinanced > 0 ? (double)(totalCollected / totalFinanced) * 100.0 : 100.0;

        var summary = new PaymentPlanSummaryDto
        {
            TotalPlans = plans.Count,
            ActivePlans = plans.Count(p => p.Status == "Active"),
            CompletedPlans = plans.Count(p => p.Status == "Completed"),
            AtRiskPlans = plans.Count(p => p.Status == "AtRisk" || p.Status == "Defaulted"),
            TotalFinancedCapital = totalFinanced,
            TotalCollectedAmount = totalCollected,
            TotalOutstandingReceivables = totalOutstanding,
            TotalOverdueArrears = totalOverdue,
            CollectionRatePercentage = Math.Round(collectionRate, 1)
        };

        return Ok(new { data = summary });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePaymentPlanDto dto)
    {
        if (dto.TotalAmount <= 0)
        {
            return BadRequest(new { message = "Total amount must be greater than zero." });
        }

        if (dto.DownPayment < 0 || dto.DownPayment > dto.TotalAmount)
        {
            return BadRequest(new { message = "Down payment cannot be negative or exceed total amount." });
        }

        if (dto.NumberOfInstallments < 1)
        {
            return BadRequest(new { message = "Number of installments must be at least 1." });
        }

        var clinic = await _clinicRepo.GetByIdAsync(dto.ClinicId);
        if (clinic == null) return NotFound(new { message = "Clinic not found" });

        var patient = await _patientRepo.GetByIdAsync(dto.PatientId);
        if (patient == null) return NotFound(new { message = "Patient not found" });

        var doctor = await _doctorRepo.GetByIdAsync(dto.DoctorId);
        if (doctor == null) return NotFound(new { message = "Doctor not found" });

        var financedAmount = dto.TotalAmount - dto.DownPayment;
        var interestMultiplier = 1.0m + (dto.InterestRate / 100.0m);
        var totalPayable = dto.DownPayment + (financedAmount * interestMultiplier);
        var financedToSplit = financedAmount * interestMultiplier;

        var planNumber = await _planRepo.GetNextPlanNumberAsync();

        var plan = new PaymentPlan
        {
            PlanNumber = planNumber,
            ClinicId = dto.ClinicId,
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            TreatmentPlanId = dto.TreatmentPlanId,
            BillingRecordId = dto.BillingRecordId,
            Title = string.IsNullOrWhiteSpace(dto.Title) ? "Patient Financing Plan" : dto.Title.Trim(),
            TotalAmount = dto.TotalAmount,
            DownPayment = dto.DownPayment,
            FinancedAmount = financedAmount,
            InterestRate = dto.InterestRate,
            TotalPayable = totalPayable,
            NumberOfInstallments = dto.NumberOfInstallments,
            InstallmentFrequency = dto.InstallmentFrequency ?? "Monthly",
            StartDate = dto.StartDate,
            Status = financedToSplit <= 0 ? "Completed" : "Active",
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        // Cent-perfect installment schedule generation (BR-PF-01 / BR-FIN-02)
        if (financedToSplit > 0)
        {
            var baseAmount = Math.Floor((financedToSplit / dto.NumberOfInstallments) * 100m) / 100m;
            var allocatedTotal = baseAmount * (dto.NumberOfInstallments - 1);
            var lastAmount = financedToSplit - allocatedTotal;

            if (!DateTime.TryParse(dto.StartDate, out var startDt))
            {
                startDt = DateTime.UtcNow;
            }

            for (var i = 1; i <= dto.NumberOfInstallments; i++)
            {
                var dueDate = plan.InstallmentFrequency == "BiWeekly" 
                    ? startDt.AddDays((i - 1) * 14).ToString("yyyy-MM-dd")
                    : startDt.AddMonths(i - 1).ToString("yyyy-MM-dd");

                var amountDue = (i == dto.NumberOfInstallments) ? lastAmount : baseAmount;

                plan.Installments.Add(new PaymentPlanInstallment
                {
                    InstallmentNumber = i,
                    DueDate = dueDate,
                    AmountDue = amountDue,
                    AmountPaid = 0.0m,
                    Status = "Pending"
                });
            }
        }

        await _planRepo.AddAsync(plan);

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        plan.Clinic = clinic;
        plan.Patient = patient;
        plan.Doctor = doctor;

        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, new { data = MapToDto(plan, today) });
    }

    [HttpPost("{id}/installments/{installmentId}/pay")]
    public async Task<IActionResult> RecordPayment(string id, string installmentId, [FromBody] RecordInstallmentPaymentDto dto)
    {
        if (dto.Amount <= 0)
        {
            return BadRequest(new { message = "Payment amount must be greater than zero." });
        }

        var plan = await _context.PaymentPlans
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Clinic)
            .Include(p => p.Installments)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (plan == null) return NotFound(new { message = "Payment plan not found" });

        var installment = plan.Installments.FirstOrDefault(i => i.Id == installmentId);
        if (installment == null) return NotFound(new { message = "Installment not found" });

        if (installment.Status == "Paid")
        {
            return BadRequest(new { message = "This installment is already fully settled." });
        }

        installment.AmountPaid += dto.Amount;
        installment.PaymentMethod = dto.PaymentMethod;
        installment.TransactionReference = dto.TransactionReference;
        installment.PaidAt = DateTime.UtcNow.ToString("o");
        installment.Notes = dto.Notes;

        if (installment.AmountPaid >= installment.AmountDue)
        {
            installment.Status = "Paid";
        }
        else
        {
            installment.Status = "PartiallyPaid";
        }

        // Check if all installments are settled
        if (plan.Installments.All(i => i.Status == "Paid"))
        {
            plan.Status = "Completed";
            plan.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var hasSevereOverdue = plan.Installments.Any(i => 
                i.Status != "Paid" && 
                DateTime.TryParse(i.DueDate, out var d) && 
                (DateTime.UtcNow - d).TotalDays > 30);

            if (hasSevereOverdue)
            {
                plan.Status = "AtRisk";
            }
            else if (plan.Status == "AtRisk")
            {
                plan.Status = "Active";
            }
        }

        await _context.SaveChangesAsync();

        var todayStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return Ok(new
        {
            message = "Payment recorded successfully",
            data = MapToDto(plan, todayStr)
        });
    }

    [HttpPost("{id}/installments/{installmentId}/remind")]
    public async Task<IActionResult> SendReminder(string id, string installmentId)
    {
        var plan = await _context.PaymentPlans
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Clinic)
            .Include(p => p.Installments)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (plan == null) return NotFound(new { message = "Payment plan not found" });

        var installment = plan.Installments.FirstOrDefault(i => i.Id == installmentId);
        if (installment == null) return NotFound(new { message = "Installment not found" });

        var patientPhone = plan.Patient?.PhoneNumber ?? string.Empty;
        if (string.IsNullOrWhiteSpace(patientPhone))
        {
            return BadRequest(new { message = "Patient does not have a registered phone number." });
        }

        var patientName = plan.Patient != null ? $"{plan.Patient.FirstName} {plan.Patient.LastName}".Trim() : "Patient";
        var clinicName = plan.Clinic?.Name ?? "Clinic";
        var remaining = installment.AmountDue - installment.AmountPaid;

        var message = $"Dear {patientName}, this is a friendly reminder from {clinicName}. Installment #{installment.InstallmentNumber} of {remaining:C} for your treatment plan '{plan.Title}' is due on {installment.DueDate}. You can view receipts or pay online here: https://clinic-app-ten-topaz.vercel.app/portal/dashboard";

        try
        {
            await _whatsAppService.SendNotificationAsync(patientPhone, message);
        }
        catch
        {
            // Fallback non-blocking
        }

        installment.ReminderSentAt = DateTime.UtcNow.ToString("o");
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "WhatsApp payment reminder sent successfully.",
            reminderSentAt = installment.ReminderSentAt
        });
    }

    private static PaymentPlanDto MapToDto(PaymentPlan p, string todayStr)
    {
        decimal totalPaid = p.DownPayment;

        var installmentsDto = p.Installments.Select(i =>
        {
            totalPaid += i.AmountPaid;

            // Evaluate overdue state dynamically
            var effectiveStatus = i.Status;
            if (effectiveStatus == "Pending" || effectiveStatus == "PartiallyPaid")
            {
                if (string.Compare(i.DueDate, todayStr, StringComparison.Ordinal) < 0)
                {
                    effectiveStatus = "Overdue";
                }
            }

            return new PaymentPlanInstallmentDto
            {
                Id = i.Id,
                PaymentPlanId = i.PaymentPlanId,
                InstallmentNumber = i.InstallmentNumber,
                DueDate = i.DueDate,
                AmountDue = i.AmountDue,
                AmountPaid = i.AmountPaid,
                Status = effectiveStatus,
                PaidAt = i.PaidAt,
                PaymentMethod = i.PaymentMethod,
                TransactionReference = i.TransactionReference,
                ReminderSentAt = i.ReminderSentAt,
                Notes = i.Notes
            };
        }).ToList();

        var remainingBalance = Math.Max(0.0m, p.TotalPayable - totalPaid);

        return new PaymentPlanDto
        {
            Id = p.Id,
            PlanNumber = p.PlanNumber,
            ClinicId = p.ClinicId,
            ClinicName = p.Clinic?.Name ?? "Clinic",
            PatientId = p.PatientId,
            PatientName = p.Patient != null ? $"{p.Patient.FirstName} {p.Patient.LastName}".Trim() : "Patient",
            PatientPhone = p.Patient?.PhoneNumber ?? string.Empty,
            DoctorId = p.DoctorId,
            DoctorName = p.Doctor != null ? $"Dr. {p.Doctor.FirstName} {p.Doctor.LastName}".Trim() : "Doctor",
            TreatmentPlanId = p.TreatmentPlanId,
            BillingRecordId = p.BillingRecordId,
            Title = p.Title,
            TotalAmount = p.TotalAmount,
            DownPayment = p.DownPayment,
            FinancedAmount = p.FinancedAmount,
            InterestRate = p.InterestRate,
            TotalPayable = p.TotalPayable,
            TotalPaid = totalPaid,
            RemainingBalance = remainingBalance,
            NumberOfInstallments = p.NumberOfInstallments,
            InstallmentFrequency = p.InstallmentFrequency,
            StartDate = p.StartDate,
            Status = p.Status,
            Notes = p.Notes,
            CreatedAt = p.CreatedAt,
            CompletedAt = p.CompletedAt,
            Installments = installmentsDto
        };
    }
}
