using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Clinic.IntegrationTests;

public class PortalControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PortalControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SendOtp_ShouldReturnSuccess_WithDebugOtp()
    {
        // Arrange
        var request = new { phoneNumber = "+201012345678" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/portal/auth/send-otp", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var success = doc.RootElement.GetProperty("success").GetBoolean();
        Assert.True(success);
        Assert.True(doc.RootElement.TryGetProperty("debugOtp", out _));
    }

    [Fact]
    public async Task VerifyOtp_WithValidOtp_ShouldReturnToken()
    {
        // Arrange
        var phone = "+201098765432";
        var sendRes = await _client.PostAsJsonAsync("/api/portal/auth/send-otp", new { phoneNumber = phone });
        var sendJson = await sendRes.Content.ReadAsStringAsync();
        using var sendDoc = JsonDocument.Parse(sendJson);
        var code = sendDoc.RootElement.GetProperty("debugOtp").GetString();

        // Act
        var verifyRes = await _client.PostAsJsonAsync("/api/portal/auth/verify-otp", new { phoneNumber = phone, code });

        // Assert
        Assert.Equal(HttpStatusCode.OK, verifyRes.StatusCode);
        var verifyJson = await verifyRes.Content.ReadAsStringAsync();
        using var verifyDoc = JsonDocument.Parse(verifyJson);
        Assert.True(verifyDoc.RootElement.TryGetProperty("token", out var tokenProp));
        Assert.False(string.IsNullOrEmpty(tokenProp.GetString()));
    }

    [Fact]
    public async Task VerifyOtp_WithInvalidCode_ShouldReturnUnauthorized()
    {
        // Act
        var verifyRes = await _client.PostAsJsonAsync("/api/portal/auth/verify-otp", new { phoneNumber = "+201000000000", code = "000000" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, verifyRes.StatusCode);
    }

    [Fact]
    public async Task GetAvailableSlots_ShouldReturnTimeSlots()
    {
        // Arrange
        var doctorId = "doc-123";
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync($"/api/portal/doctors/available-slots?doctorId={doctorId}&date={date}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("slots", out var slots));
        Assert.True(slots.GetArrayLength() > 0);
    }
}
