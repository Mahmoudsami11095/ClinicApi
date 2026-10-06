using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Clinic.IntegrationTests;

public class RadiologyIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public RadiologyIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string GenerateJwtToken(string role = "doctor", string doctorId = "doc-rad-1", string clinicId = "clinic-rad-1")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("SuperSecretTestingKeyThatIsAtLeast32BytesLong!");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-rad-1"),
            new(ClaimTypes.Name, "Dr. Radiologist"),
            new(ClaimTypes.Email, "radiology@clinic.com"),
            new(ClaimTypes.Role, role),
            new("doctorId", doctorId),
            new("clinicId", clinicId)
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
    public async Task GetCenters_WithAuthenticatedUser_Returns200Ok()
    {
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/radiology/centers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateCenter_WithValidDetails_Returns201CreatedOr200Ok()
    {
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = JsonSerializer.Serialize(new
        {
            name = "Alpha Scan Diagnostic Center",
            phone = "+201019998888",
            address = "Tahrir Square, Cairo",
            notes = "Specializes in Cone Beam Computed Tomography (CBCT)"
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/radiology/centers", content);

        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRecords_WithDoctorClaim_Returns200Ok()
    {
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/radiology/records?doctorId=doc-rad-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeScanWithAi_ShouldReturn200Ok_WithMultiHeadFindings()
    {
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync("/api/radiology/records/rec-sample-1/ai-analyze", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("findings", out var findings));
        Assert.True(findings.GetArrayLength() >= 4);
        Assert.True(doc.RootElement.GetProperty("overallConfidence").GetDouble() > 80.0);
    }

    [Fact]
    public async Task SyncAiFindingsToOdontogram_ShouldReturn200Ok()
    {
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = JsonSerializer.Serialize(new
        {
            acceptedFindingIds = new[] { "ai-find-101", "ai-find-102" },
            doctorNotes = "Verified by Dr. Jenkins during clinical inspection."
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/radiology/records/rec-sample-1/ai-sync-odontogram", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
