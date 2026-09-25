using System.Net;
using System.Text.Json;
using Xunit;

namespace Clinic.IntegrationTests;

public class HealthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnAwakeStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var status = doc.RootElement.GetProperty("status").GetString();

        Assert.Equal("awake", status);
    }

    [Fact]
    public async Task DebugErrorEndpoint_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/debug-error");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
