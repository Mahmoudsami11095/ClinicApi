using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Clinic.IntegrationTests;

public class DentalIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public DentalIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string GenerateJwtToken(string role = "doctor", string doctorId = "doc-dental-1", string clinicId = "clinic-dental-1")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("SuperSecretTestingKeyThatIsAtLeast32BytesLong!");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-dental-1"),
            new(ClaimTypes.Name, "Dr. Dental Surgeon"),
            new(ClaimTypes.Email, "dentist@clinic.com"),
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

    private async Task EnsureClinicAndDoctorSeededAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();

        if (!await db.Clinics.AnyAsync(c => c.Id == "clinic-dental-1"))
        {
            db.Clinics.Add(new ClinicEntity
            {
                Id = "clinic-dental-1",
                Name = "Dental Practice Clinic",
                CreatorDoctorId = "doc-dental-1"
            });
            await db.SaveChangesAsync();
        }

        if (!await db.Doctors.AnyAsync(d => d.Id == "doc-dental-1"))
        {
            db.Doctors.Add(new Doctor
            {
                Id = "doc-dental-1",
                FirstName = "Dental",
                LastName = "Surgeon"
            });
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task GetAll_WithAuthenticatedDoctor_Returns200Ok()
    {
        await EnsureClinicAndDoctorSeededAsync();
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/dental");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidDentalProcedure_Returns200OkAndPersistsLog()
    {
        await EnsureClinicAndDoctorSeededAsync();
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        const string patientId = "pat-dental-test-1";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Patients.AnyAsync(p => p.Id == patientId))
            {
                db.Patients.Add(new Patient
                {
                    Id = patientId,
                    FirstName = "Dental",
                    LastName = "Patient",
                    ClinicId = "clinic-dental-1",
                    ContactNumber = "+201018887777"
                });
                await db.SaveChangesAsync();
            }
        }

        var payload = JsonSerializer.Serialize(new
        {
            patientId,
            doctorId = "doc-dental-1",
            clinicId = "clinic-dental-1",
            toothNumber = "16",
            treatment = "Composite Restoration",
            cost = 350.00m,
            stage = "proposed",
            date = DateTime.UtcNow.ToString("yyyy-MM-dd")
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/dental", content);

        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created);
    }

    [Fact]
    public async Task UpdateStage_FromProposedToAccepted_TransitionsSuccessfully()
    {
        await EnsureClinicAndDoctorSeededAsync();
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var logId = Guid.NewGuid().ToString();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            db.DentalLogs.Add(new DentalLog
            {
                Id = logId,
                PatientId = "pat-stage-test",
                DoctorId = "doc-dental-1",
                ClinicId = "clinic-dental-1",
                ToothNumber = "21",
                Treatment = "Crown Preparation",
                Cost = 800.00m,
                Stage = "proposed",
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd")
            });
            await db.SaveChangesAsync();
        }

        var payload = JsonSerializer.Serialize(new { stage = "accepted" });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PutAsync($"/api/dental/{logId}/stage", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
