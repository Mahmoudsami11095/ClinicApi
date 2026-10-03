using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Clinic.IntegrationTests;

public class SubscriptionTierIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public SubscriptionTierIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string GenerateJwtToken(string email = "sub_tier_doc@example.com", string role = "doctor", string doctorId = "doc-tier-1")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("SuperSecretTestingKeyThatIsAtLeast32BytesLong!");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-sub-tier-1"),
            new(ClaimTypes.Name, "Dr. Subscription Doctor"),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new("DoctorId", doctorId)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "ClinicApiTesting",
            Audience = "ClinicAppTesting",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task REQ_SUB_01_GetStatus_ReturnsTierQuotaAndStorageMetrics()
    {
        const string docId = "doc-tier-1";
        const string email = "sub_tier_doc@example.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Doctors.AnyAsync(d => d.Id == docId))
            {
                db.Doctors.Add(new Doctor
                {
                    Id = docId,
                    FirstName = "Karim",
                    LastName = "Nabil",
                    Email = email,
                    SubscriptionStatus = "Active",
                    SubscriptionEndDate = DateTime.UtcNow.AddMonths(11)
                });
                await db.SaveChangesAsync();
            }
        }

        var token = GenerateJwtToken(email, "doctor", docId);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/subscriptions/status");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("tierQuota", out var quota));
        Assert.Equal("Professional", quota.GetProperty("tierName").GetString());
        Assert.True(quota.GetProperty("maxDoctorSeats").GetInt32() >= 2);
        Assert.True(quota.GetProperty("maxStorageGb").GetDouble() > 0);
        Assert.True(quota.TryGetProperty("isStorageThresholdWarning", out _));
    }

    [Fact]
    public async Task REQ_SUB_01_UpgradeTier_RecordsUpgradeRequestSuccessfully()
    {
        const string docId = "doc-tier-2";
        const string email = "sub_tier_doc2@example.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Doctors.AnyAsync(d => d.Id == docId))
            {
                db.Doctors.Add(new Doctor
                {
                    Id = docId,
                    FirstName = "Tamer",
                    LastName = "Hosny",
                    Email = email,
                    SubscriptionStatus = "Active",
                    SubscriptionEndDate = DateTime.UtcNow.AddMonths(8)
                });
                await db.SaveChangesAsync();
            }
        }

        var token = GenerateJwtToken(email, "doctor", docId);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/subscriptions/upgrade-tier")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { TargetTier = "Enterprise" }),
                Encoding.UTF8,
                "application/json"
            )
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Enterprise", doc.RootElement.GetProperty("targetTier").GetString());
    }
}
