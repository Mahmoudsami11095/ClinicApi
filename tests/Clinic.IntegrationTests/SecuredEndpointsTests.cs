using System.Net;
using Xunit;

namespace Clinic.IntegrationTests;

public class SecuredEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecuredEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/patients")]
    [InlineData("/api/appointments")]
    [InlineData("/api/billing")]
    [InlineData("/api/prescriptions")]
    [InlineData("/api/materials/doctor/doc-1")]
    public async Task SecuredEndpoints_WithoutJwtToken_ShouldReturnUnauthorized(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert - Anonymous access must be rejected with 401 Unauthorized
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
