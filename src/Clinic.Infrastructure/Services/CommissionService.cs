using System.Text.Json;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Services;

public class CommissionService : ICommissionService
{
    private readonly ClinicDbContext _db;

    public CommissionService(ClinicDbContext db)
    {
        _db = db;
    }

    public async Task<DoctorCommissionPlanDto?> GetPlanByDoctorIdAsync(string doctorId, string? clinicId = null, CancellationToken ct = default)
    {
        var plan = await _db.DoctorCommissionPlans
            .Include(p => p.Doctor)
            .Where(p => p.DoctorId == doctorId && (clinicId == null || p.ClinicId == null || p.ClinicId == clinicId) && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (plan == null)
        {
            var doctor = await _db.Doctors.FindAsync(new object[] { doctorId }, ct);
            if (doctor == null) return null;

            // Return default 30% plan template
            return new DoctorCommissionPlanDto
            {
                Id = string.Empty,
                DoctorId = doctorId,
                DoctorName = $"{doctor.FirstName} {doctor.LastName}".Trim(),
                ClinicId = clinicId,
                DefaultCommissionRate = 30.0m,
                LabFeeDeductionType = "BeforeCommission",
                SpecialtyRates = new Dictionary<string, decimal>
                {
                    { "Endodontics", 35.0m },
                    { "Implantology", 40.0m },
                    { "Orthodontics", 35.0m },
                    { "Surgery", 40.0m },
                    { "Restorative", 30.0m },
                    { "Preventive", 25.0m }
                },
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        return MapPlanToDto(plan);
    }

    public async Task<DoctorCommissionPlanDto> UpsertPlanAsync(CreateOrUpdateCommissionPlanDto dto, CancellationToken ct = default)
    {
        var plan = await _db.DoctorCommissionPlans
            .Include(p => p.Doctor)
            .FirstOrDefaultAsync(p => p.DoctorId == dto.DoctorId && (dto.ClinicId == null || p.ClinicId == dto.ClinicId), ct);

        var specialtyJson = dto.SpecialtyRates != null && dto.SpecialtyRates.Count > 0
            ? JsonSerializer.Serialize(dto.SpecialtyRates)
            : "{}";

        if (plan == null)
        {
            plan = new DoctorCommissionPlan
            {
                DoctorId = dto.DoctorId,
                ClinicId = dto.ClinicId,
                DefaultCommissionRate = dto.DefaultCommissionRate,
                LabFeeDeductionType = dto.LabFeeDeductionType,
                SpecialtyRatesJson = specialtyJson,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            _db.DoctorCommissionPlans.Add(plan);
        }
        else
        {
            plan.DefaultCommissionRate = dto.DefaultCommissionRate;
            plan.LabFeeDeductionType = dto.LabFeeDeductionType;
            plan.SpecialtyRatesJson = specialtyJson;
            plan.IsActive = dto.IsActive;
            plan.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return MapPlanToDto(plan);
    }

    public async Task<CommissionAnalyticsDto> GetAnalyticsAsync(
        string? clinicId, 
        string? doctorId, 
        DateTime? startDate, 
        DateTime? endDate, 
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var start = startDate ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = endDate ?? start.AddMonths(1).AddTicks(-1);

        // Fetch all active commission plans
        var plans = await _db.DoctorCommissionPlans
            .Include(p => p.Doctor)
            .Where(p => p.IsActive)
            .ToListAsync(ct);

        var planMap = plans.ToDictionary(p => $"{p.DoctorId}_{p.ClinicId ?? "ALL"}", p => p);

        // Fetch billing records linked to appointments
        var billingsQuery = _db.BillingRecords
            .Include(b => b.Patient)
            .Include(b => b.Appointment)
                .ThenInclude(a => a!.Doctor)
            .Where(b => b.Status != "voided");

        if (!string.IsNullOrEmpty(clinicId))
        {
            billingsQuery = billingsQuery.Where(b => b.ClinicId == clinicId || (b.Appointment != null && b.Appointment.ClinicId == clinicId));
        }

        var allBills = await billingsQuery.ToListAsync(ct);

        // Filter by date range (parsed from DateIssued or fallback to now)
        var bills = allBills.Where(b =>
        {
            if (DateTime.TryParse(b.DateIssued, out var dt))
            {
                var utcDt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                return utcDt >= start && utcDt <= end;
            }
            return true;
        }).ToList();

        // Also fetch all doctors to guarantee presence in summary
        var doctorsQuery = _db.Doctors.AsQueryable();
        if (!string.IsNullOrEmpty(doctorId))
        {
            doctorsQuery = doctorsQuery.Where(d => d.Id == doctorId);
        }
        var allDoctors = await doctorsQuery.ToListAsync(ct);

        var encounterItems = new List<CommissionItemDto>();

        foreach (var bill in bills)
        {
            var doc = bill.Appointment?.Doctor;
            if (doc == null && allDoctors.Count > 0)
            {
                doc = allDoctors.First();
            }
            if (doc == null) continue;

            if (!string.IsNullOrEmpty(doctorId) && doc.Id != doctorId)
            {
                continue;
            }

            var patientName = bill.Patient != null 
                ? $"{bill.Patient.FirstName} {bill.Patient.LastName}".Trim() 
                : "Walk-in Patient";

            var category = DeduceCategory(bill.Description, doc.Specialization);
            var gross = bill.PaidAmount ?? bill.Amount;
            if (gross <= 0) gross = bill.Amount;

            var labFee = CalculateLabFee(gross, category, bill.Description);

            // Determine doctor plan
            DoctorCommissionPlan? plan = null;
            if (planMap.TryGetValue($"{doc.Id}_{bill.ClinicId}", out var specificPlan))
            {
                plan = specificPlan;
            }
            else if (planMap.TryGetValue($"{doc.Id}_ALL", out var generalPlan))
            {
                plan = generalPlan;
            }

            var rate = plan?.DefaultCommissionRate ?? 30.0m;
            var deductionType = plan?.LabFeeDeductionType ?? "BeforeCommission";

            if (plan != null && !string.IsNullOrEmpty(plan.SpecialtyRatesJson))
            {
                try
                {
                    var overrides = JsonSerializer.Deserialize<Dictionary<string, decimal>>(plan.SpecialtyRatesJson);
                    if (overrides != null && overrides.TryGetValue(category, out var customRate))
                    {
                        rate = customRate;
                    }
                }
                catch { }
            }

            var commission = CalculateNetCommission(gross, labFee, rate, deductionType);
            var clinicShare = Math.Max(0, gross - commission - labFee);

            DateTime serviceDt = DateTime.UtcNow;
            if (DateTime.TryParse(bill.DateIssued, out var parsedDt))
            {
                serviceDt = DateTime.SpecifyKind(parsedDt, DateTimeKind.Utc);
            }

            encounterItems.Add(new CommissionItemDto
            {
                Id = Guid.NewGuid().ToString(),
                BillingRecordId = bill.Id,
                AppointmentId = bill.AppointmentId,
                DoctorId = doc.Id,
                DoctorName = $"Dr. {doc.FirstName} {doc.LastName}".Trim(),
                PatientName = patientName,
                ServiceCategory = category,
                Description = bill.Description ?? $"{category} Procedure",
                ServiceDate = serviceDt,
                GrossAmount = gross,
                LabFee = labFee,
                CommissionRate = rate,
                CommissionAmount = commission,
                ClinicAmount = clinicShare
            });
        }

        // Aggregate doctor summaries
        var doctorSummaries = new List<DoctorCommissionSummaryDto>();
        foreach (var doc in allDoctors)
        {
            var docItems = encounterItems.Where(i => i.DoctorId == doc.Id).ToList();
            var docGross = docItems.Sum(i => i.GrossAmount);
            var docLab = docItems.Sum(i => i.LabFee);
            var docComm = docItems.Sum(i => i.CommissionAmount);
            var docClinic = docItems.Sum(i => i.ClinicAmount);
            var effectiveRate = docGross > 0 ? Math.Round((docComm / docGross) * 100m, 1) : 0m;

            var hasPlan = planMap.ContainsKey($"{doc.Id}_{clinicId ?? "ALL"}") || planMap.ContainsKey($"{doc.Id}_ALL");

            doctorSummaries.Add(new DoctorCommissionSummaryDto
            {
                DoctorId = doc.Id,
                DoctorName = $"Dr. {doc.FirstName} {doc.LastName}".Trim(),
                Specialization = doc.Specialization,
                GrossRevenue = docGross,
                LabFeesDeducted = docLab,
                NetCommission = docComm,
                ClinicShare = docClinic,
                EffectiveRate = effectiveRate,
                TotalProcedures = docItems.Count,
                HasActivePlan = hasPlan
            });
        }

        var totalGross = encounterItems.Sum(i => i.GrossAmount);
        var totalLab = encounterItems.Sum(i => i.LabFee);
        var totalCommission = encounterItems.Sum(i => i.CommissionAmount);
        var totalClinic = encounterItems.Sum(i => i.ClinicAmount);

        return new CommissionAnalyticsDto
        {
            PeriodStart = start,
            PeriodEnd = end,
            TotalGrossRevenue = totalGross,
            TotalLabFeesDeducted = totalLab,
            TotalNetCommission = totalCommission,
            TotalClinicRetainedRevenue = totalClinic,
            DoctorCount = doctorSummaries.Count(d => d.TotalProcedures > 0),
            ProcedureCount = encounterItems.Count,
            Doctors = doctorSummaries.OrderByDescending(d => d.GrossRevenue).ToList(),
            EncounterItems = encounterItems.OrderByDescending(i => i.ServiceDate).ToList()
        };
    }

    public async Task<CommissionPayoutDto> CreatePayoutAsync(CreateCommissionPayoutDto dto, CancellationToken ct = default)
    {
        var analytics = await GetAnalyticsAsync(dto.ClinicId, dto.DoctorId, dto.PeriodStart, dto.PeriodEnd, ct);
        var docSummary = analytics.Doctors.FirstOrDefault(d => d.DoctorId == dto.DoctorId);

        var doctor = await _db.Doctors.FindAsync(new object[] { dto.DoctorId }, ct);
        var docName = doctor != null ? $"Dr. {doctor.FirstName} {doctor.LastName}".Trim() : "Doctor";

        var payout = new CommissionPayout
        {
            DoctorId = dto.DoctorId,
            ClinicId = dto.ClinicId,
            PeriodStart = analytics.PeriodStart,
            PeriodEnd = analytics.PeriodEnd,
            TotalGrossRevenue = docSummary?.GrossRevenue ?? 0m,
            TotalLabFeesDeducted = docSummary?.LabFeesDeducted ?? 0m,
            TotalNetCommission = docSummary?.NetCommission ?? 0m,
            ClinicRetainedRevenue = docSummary?.ClinicShare ?? 0m,
            Status = "Draft",
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        var docItems = analytics.EncounterItems.Where(i => i.DoctorId == dto.DoctorId).ToList();
        foreach (var item in docItems)
        {
            payout.Items.Add(new CommissionPayoutItem
            {
                CommissionPayoutId = payout.Id,
                BillingRecordId = item.BillingRecordId,
                AppointmentId = item.AppointmentId,
                PatientName = item.PatientName,
                ServiceCategory = item.ServiceCategory,
                Description = item.Description,
                GrossAmount = item.GrossAmount,
                LabFee = item.LabFee,
                CommissionRate = item.CommissionRate,
                CommissionAmount = item.CommissionAmount,
                ServiceDate = item.ServiceDate
            });
        }

        _db.CommissionPayouts.Add(payout);
        await _db.SaveChangesAsync(ct);

        return MapPayoutToDto(payout, docName);
    }

    public async Task<CommissionPayoutDto?> SettlePayoutAsync(string payoutId, SettleCommissionPayoutDto dto, CancellationToken ct = default)
    {
        var payout = await _db.CommissionPayouts
            .Include(p => p.Doctor)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == payoutId, ct);

        if (payout == null) return null;

        payout.Status = "Paid";
        payout.PaymentReference = dto.PaymentReference;
        payout.PaidAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(dto.Notes))
        {
            payout.Notes = string.IsNullOrEmpty(payout.Notes) ? dto.Notes : $"{payout.Notes}; {dto.Notes}";
        }

        await _db.SaveChangesAsync(ct);

        var docName = payout.Doctor != null ? $"Dr. {payout.Doctor.FirstName} {payout.Doctor.LastName}".Trim() : "Doctor";
        return MapPayoutToDto(payout, docName);
    }

    public async Task<List<CommissionPayoutDto>> GetPayoutsAsync(string? clinicId, string? doctorId, CancellationToken ct = default)
    {
        var query = _db.CommissionPayouts
            .Include(p => p.Doctor)
            .Include(p => p.Items)
            .AsQueryable();

        if (!string.IsNullOrEmpty(clinicId))
        {
            query = query.Where(p => p.ClinicId == clinicId);
        }

        if (!string.IsNullOrEmpty(doctorId))
        {
            query = query.Where(p => p.DoctorId == doctorId);
        }

        var list = await query.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);

        return list.Select(p =>
        {
            var docName = p.Doctor != null ? $"Dr. {p.Doctor.FirstName} {p.Doctor.LastName}".Trim() : "Doctor";
            return MapPayoutToDto(p, docName);
        }).ToList();
    }

    public static decimal CalculateNetCommission(decimal gross, decimal labFee, decimal ratePercent, string deductionType)
    {
        var rate = ratePercent / 100m;
        switch (deductionType)
        {
            case "BeforeCommission":
                // Net Revenue = Gross - LabFee; Commission = Net * Rate
                var netRev = Math.Max(0m, gross - labFee);
                return Math.Round(netRev * rate, 2);

            case "AfterCommission":
                // Commission = (Gross * Rate) - LabFee
                var grossComm = gross * rate;
                return Math.Max(0m, Math.Round(grossComm - labFee, 2));

            case "None":
            default:
                // Commission = Gross * Rate
                return Math.Round(gross * rate, 2);
        }
    }

    public static decimal CalculateLabFee(decimal gross, string category, string? description)
    {
        var desc = (description ?? string.Empty).ToLowerInvariant();
        if (desc.Contains("crown") || desc.Contains("bridge") || desc.Contains("zirconia") || desc.Contains("porcelain"))
        {
            return Math.Round(gross * 0.25m, 2); // 25% lab fee for crown/bridge
        }
        if (desc.Contains("denture") || desc.Contains("prosthetic"))
        {
            return Math.Round(gross * 0.30m, 2); // 30% lab fee
        }
        if (desc.Contains("implant") || category.Equals("Implantology", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Round(gross * 0.20m, 2); // 20% lab fee
        }
        if (desc.Contains("aligner") || category.Equals("Orthodontics", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Round(gross * 0.15m, 2); // 15% lab fee
        }
        return 0m;
    }

    public static string DeduceCategory(string? description, string? doctorSpecialization)
    {
        var desc = (description ?? string.Empty).ToLowerInvariant();
        if (desc.Contains("root canal") || desc.Contains("endo") || desc.Contains("nerve")) return "Endodontics";
        if (desc.Contains("crown") || desc.Contains("bridge") || desc.Contains("veneer") || desc.Contains("implant")) return "Implantology";
        if (desc.Contains("brace") || desc.Contains("aligner") || desc.Contains("ortho")) return "Orthodontics";
        if (desc.Contains("extract") || desc.Contains("surg") || desc.Contains("wisdom")) return "Surgery";
        if (desc.Contains("fill") || desc.Contains("composite") || desc.Contains("decay")) return "Restorative";
        if (desc.Contains("clean") || desc.Contains("scale") || desc.Contains("polish") || desc.Contains("hygiene")) return "Preventive";

        if (!string.IsNullOrEmpty(doctorSpecialization))
        {
            if (doctorSpecialization.Contains("Endo", StringComparison.OrdinalIgnoreCase)) return "Endodontics";
            if (doctorSpecialization.Contains("Ortho", StringComparison.OrdinalIgnoreCase)) return "Orthodontics";
            if (doctorSpecialization.Contains("Surg", StringComparison.OrdinalIgnoreCase)) return "Surgery";
            if (doctorSpecialization.Contains("Implant", StringComparison.OrdinalIgnoreCase)) return "Implantology";
        }

        return "General Consultation";
    }

    private static DoctorCommissionPlanDto MapPlanToDto(DoctorCommissionPlan plan)
    {
        var rates = new Dictionary<string, decimal>();
        if (!string.IsNullOrEmpty(plan.SpecialtyRatesJson))
        {
            try
            {
                rates = JsonSerializer.Deserialize<Dictionary<string, decimal>>(plan.SpecialtyRatesJson) ?? new();
            }
            catch { }
        }

        return new DoctorCommissionPlanDto
        {
            Id = plan.Id,
            DoctorId = plan.DoctorId,
            DoctorName = plan.Doctor != null ? $"{plan.Doctor.FirstName} {plan.Doctor.LastName}".Trim() : null,
            ClinicId = plan.ClinicId,
            DefaultCommissionRate = plan.DefaultCommissionRate,
            LabFeeDeductionType = plan.LabFeeDeductionType,
            SpecialtyRates = rates,
            IsActive = plan.IsActive,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt
        };
    }

    private static CommissionPayoutDto MapPayoutToDto(CommissionPayout payout, string doctorName)
    {
        return new CommissionPayoutDto
        {
            Id = payout.Id,
            DoctorId = payout.DoctorId,
            DoctorName = doctorName,
            ClinicId = payout.ClinicId,
            PeriodStart = payout.PeriodStart,
            PeriodEnd = payout.PeriodEnd,
            TotalGrossRevenue = payout.TotalGrossRevenue,
            TotalLabFeesDeducted = payout.TotalLabFeesDeducted,
            TotalNetCommission = payout.TotalNetCommission,
            ClinicRetainedRevenue = payout.ClinicRetainedRevenue,
            Status = payout.Status,
            PaymentReference = payout.PaymentReference,
            PaidAt = payout.PaidAt,
            Notes = payout.Notes,
            CreatedAt = payout.CreatedAt,
            Items = payout.Items.Select(i => new CommissionPayoutItemDto
            {
                Id = i.Id,
                BillingRecordId = i.BillingRecordId,
                AppointmentId = i.AppointmentId,
                PatientName = i.PatientName,
                ServiceCategory = i.ServiceCategory,
                Description = i.Description,
                GrossAmount = i.GrossAmount,
                LabFee = i.LabFee,
                CommissionRate = i.CommissionRate,
                CommissionAmount = i.CommissionAmount,
                ServiceDate = i.ServiceDate
            }).ToList()
        };
    }
}
