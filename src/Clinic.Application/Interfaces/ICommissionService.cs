using Clinic.Application.DTOs;

namespace Clinic.Application.Interfaces;

public interface ICommissionService
{
    Task<DoctorCommissionPlanDto?> GetPlanByDoctorIdAsync(string doctorId, string? clinicId = null, CancellationToken ct = default);
    Task<DoctorCommissionPlanDto> UpsertPlanAsync(CreateOrUpdateCommissionPlanDto dto, CancellationToken ct = default);
    Task<CommissionAnalyticsDto> GetAnalyticsAsync(string? clinicId, string? doctorId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<CommissionPayoutDto> CreatePayoutAsync(CreateCommissionPayoutDto dto, CancellationToken ct = default);
    Task<CommissionPayoutDto?> SettlePayoutAsync(string payoutId, SettleCommissionPayoutDto dto, CancellationToken ct = default);
    Task<List<CommissionPayoutDto>> GetPayoutsAsync(string? clinicId, string? doctorId, CancellationToken ct = default);
}
