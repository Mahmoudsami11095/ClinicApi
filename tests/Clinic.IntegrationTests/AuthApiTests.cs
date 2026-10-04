using System.Net;
using System.Text;
using System.Text.Json;
using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Clinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clinic.IntegrationTests;

public class AuthApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
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
    public async Task Login_WithNonExistentCredentials_ShouldReturnNotFound()
    {
        // Arrange
        var payload = JsonSerializer.Serialize(new
        {
            email = "unregistered_test_account@clinic.com",
            password = "WrongPassword999!"
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidDoctorCredentials_Returns200AndValidJwtToken()
    {
        const string email = "valid_auth_doc@example.com";
        const string password = "DoctorPassword123!";
        const string userId = "u-auth-test-1";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Users.AnyAsync(u => u.Email == email))
            {
                db.Users.Add(new User
                {
                    Id = userId,
                    Email = email,
                    Name = "Dr. Auth Test",
                    Role = UserRole.Doctor,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    DoctorId = "doc-auth-1"
                });
                await db.SaveChangesAsync();
            }
        }

        var payload = JsonSerializer.Serialize(new { email, password });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/auth/login", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("token", out var tokenProp));
        Assert.False(string.IsNullOrEmpty(tokenProp.GetString()));
        Assert.True(root.TryGetProperty("data", out var dataProp));
        Assert.Equal("doctor", dataProp.GetProperty("role").GetString(), ignoreCase: true);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_Returns401Unauthorized()
    {
        const string email = "wrong_pwd_user@example.com";
        const string correctPassword = "CorrectPassword123!";
        const string wrongPassword = "WrongPassword999!";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Users.AnyAsync(u => u.Email == email))
            {
                db.Users.Add(new User
                {
                    Id = "u-auth-wrong-pwd",
                    Email = email,
                    Name = "Password Test User",
                    Role = UserRole.Doctor,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(correctPassword)
                });
                await db.SaveChangesAsync();
            }
        }

        var payload = JsonSerializer.Serialize(new { email, password = wrongPassword });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/auth/login", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SendOtp_WithUnregisteredEmail_ReturnsNotFound()
    {
        var payload = JsonSerializer.Serialize(new { email = "unknown_user_otp@clinic.com" });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/auth/send-otp", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SendOtp_WithRegisteredUser_Returns200AndOtpCode()
    {
        const string email = "registered_otp_user@example.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Users.AnyAsync(u => u.Email == email))
            {
                db.Users.Add(new User
                {
                    Id = "u-registered-otp",
                    Email = email,
                    Name = "OTP User",
                    Role = UserRole.Patient
                });
                await db.SaveChangesAsync();
            }
        }

        var payload = JsonSerializer.Serialize(new { email });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/auth/send-otp", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("otp", out var otpProp));
        Assert.False(string.IsNullOrEmpty(otpProp.GetString()));
    }

    [Fact]
    public async Task RegisterSendOtp_WithDuplicateEmail_Returns400BadRequest()
    {
        const string duplicateEmail = "duplicate_register_user@example.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Users.AnyAsync(u => u.Email == duplicateEmail))
            {
                db.Users.Add(new User
                {
                    Id = "u-dup-reg-user",
                    Email = duplicateEmail,
                    Name = "Duplicate Test User",
                    Role = UserRole.Patient
                });
                await db.SaveChangesAsync();
            }
        }

        var payload = JsonSerializer.Serialize(new { email = duplicateEmail });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/auth/register-send-otp", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("already registered", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterSendOtp_WithMissingEmail_Returns400BadRequest()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/auth/register-send-otp", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMissingRequiredFields_Returns400BadRequest()
    {
        var payload = JsonSerializer.Serialize(new
        {
            email = "incomplete_reg@example.com"
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/auth/register", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidOtpCode_Returns400BadRequest()
    {
        var payload = JsonSerializer.Serialize(new
        {
            name = "Test New User",
            email = "new_valid_email@example.com",
            role = "patient",
            otpCode = "000000",
            phone = "+201019998888",
            phoneOtpCode = "000000"
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/auth/register", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
