using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Clinic.IntegrationTests;

public class AuthApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithEmptyPayload_ShouldReturnBadRequest()
    {
        // Arrange
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", content);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_WithNonExistentCredentials_ShouldReturnNotFoundOrUnauthorized()
    {
        // Arrange
        var payload = JsonSerializer.Serialize(new
        {
            email = "nonexistent.doctor@clinic.com",
            password = "WrongPassword999!"
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", content);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized || 
            response.StatusCode == HttpStatusCode.BadRequest);
    }
}
