using System.Collections.Generic;
using System.Threading.Tasks;
using Clinic.Application.DTOs;

namespace Clinic.Application.Interfaces;

public interface IExecutiveAnalyticsService
{
    Task<ExecutiveNetworkSummaryDto> GetNetworkSummaryAsync(bool forceRefresh = false);
    Task<List<BranchBenchmarkDto>> GetBranchBenchmarksAsync(bool forceRefresh = false);
    Task<List<DoctorProductivityDto>> GetDoctorProductivityAsync(bool forceRefresh = false);
    Task<List<SupplyChainVelocityDto>> GetSupplyChainVelocityAsync(bool forceRefresh = false);
    void InvalidateCache();
}
