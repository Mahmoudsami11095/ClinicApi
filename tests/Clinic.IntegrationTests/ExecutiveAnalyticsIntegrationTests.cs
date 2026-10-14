using System;
using System.Threading.Tasks;
using Clinic.Infrastructure.Data;
using Clinic.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Clinic.IntegrationTests;

public class ExecutiveAnalyticsIntegrationTests
{
    private (ClinicDbContext context, IMemoryCache cache) CreateDependencies()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new ClinicDbContext(options);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return (context, cache);
    }

    [Fact]
    public async Task GetNetworkSummary_WithEmptyDatabase_ReturnsValidDtoWithoutCrashing()
    {
        var (context, cache) = CreateDependencies();
        var service = new ExecutiveAnalyticsService(context, cache);

        var summary = await service.GetNetworkSummaryAsync(forceRefresh: true);

        Assert.NotNull(summary);
        Assert.Equal(0, summary.TotalNetworkRevenue);
        Assert.Equal(0, summary.TotalPatientEncounters);
        Assert.True(summary.ActiveBranchCount >= 1);
    }

    [Fact]
    public async Task GetBranchBenchmarks_WithEmptyDatabase_ReturnsListWithoutCrashing()
    {
        var (context, cache) = CreateDependencies();
        var service = new ExecutiveAnalyticsService(context, cache);

        var benchmarks = await service.GetBranchBenchmarksAsync(forceRefresh: true);

        Assert.NotNull(benchmarks);
        Assert.Empty(benchmarks);
    }

    [Fact]
    public async Task GetDoctorProductivity_WithEmptyDatabase_ReturnsListWithoutCrashing()
    {
        var (context, cache) = CreateDependencies();
        var service = new ExecutiveAnalyticsService(context, cache);

        var productivity = await service.GetDoctorProductivityAsync(forceRefresh: true);

        Assert.NotNull(productivity);
        Assert.Empty(productivity);
    }

    [Fact]
    public async Task GetSupplyChainVelocity_WithEmptyDatabase_ReturnsListWithoutCrashing()
    {
        var (context, cache) = CreateDependencies();
        var service = new ExecutiveAnalyticsService(context, cache);

        var velocity = await service.GetSupplyChainVelocityAsync(forceRefresh: true);

        Assert.NotNull(velocity);
        Assert.Empty(velocity);
    }
}
