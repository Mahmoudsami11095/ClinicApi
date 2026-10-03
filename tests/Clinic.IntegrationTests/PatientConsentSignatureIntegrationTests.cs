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

public class PatientConsentSignatureIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public PatientConsentSignatureIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string GenerateJwtToken(string role = "doctor", string doctorId = "doc-test-1", string? clinicId = "clinic-1")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("SuperSecretTestingKeyThatIsAtLeast32BytesLong!");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-doc-sig-1"),
            new(ClaimTypes.Name, "Dr. Signature Doctor"),
            new(ClaimTypes.Role, role),
            new("DoctorId", doctorId)
        };
        if (!string.IsNullOrEmpty(clinicId))
        {
            claims.Add(new("clinicId", clinicId));
        }

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
    public async Task REQ_PAT_03_SaveConsentSignature_StoresSignatureAndReturnsUpdatedPatient()
    {
        const string patientId = "pat-consent-sig-1";
        const string sampleSignature = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUA";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Clinics.AnyAsync(c => c.Id == "clinic-1"))
            {
                db.Clinics.Add(new ClinicEntity { Id = "clinic-1", Name = "Clinic 1", CreatorDoctorId = "doc-test-1" });
            }
            if (!await db.Patients.AnyAsync(p => p.Id == patientId))
            {
                db.Patients.Add(new Patient
                {
                    Id = patientId,
                    FirstName = "Heba",
                    LastName = "Gamal",
                    ClinicId = "clinic-1",
                    Gender = "Female",
                    ContactNumber = "+201012345999"
                });
                await db.SaveChangesAsync();
            }
        }

        var token = GenerateJwtToken();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/patients/{patientId}/consent-signature")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { Signature = sampleSignature }),
                Encoding.UTF8,
                "application/json"
            )
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        Assert.Equal(sampleSignature, data.GetProperty("consentSignature").GetString());
        Assert.NotNull(data.GetProperty("consentSignedAt").GetString());
    }
}
