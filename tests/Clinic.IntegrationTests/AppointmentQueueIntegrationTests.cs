using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Clinic.IntegrationTests;

public class AppointmentQueueIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AppointmentQueueIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string GenerateJwtToken(string doctorId = "doc-test-1", string role = "doctor", string? clinicId = "clinic-1")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("SuperSecretTestingKeyThatIsAtLeast32BytesLong!");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-test-1"),
            new(ClaimTypes.Name, "Dr. Test Doctor"),
            new(ClaimTypes.Email, "doctor@example.com"),
            new(ClaimTypes.Role, role),
            new("doctorId", doctorId)
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
    public async Task UAT_APT_02_CheckIn_AdvancesStatusToWaiting_AndAssignsQueueNumber()
    {
        // Arrange - Seed clinic, patient, appointment
        const string apptId = "appt-queue-test-1";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Clinics.AnyAsync(c => c.Id == "clinic-1"))
            {
                db.Clinics.Add(new ClinicEntity { Id = "clinic-1", Name = "Clinic 1", CreatorDoctorId = "doc-test-1" });
            }
            if (!await db.Patients.AnyAsync(p => p.Id == "pat-queue-1"))
            {
                db.Patients.Add(new Patient { Id = "pat-queue-1", FirstName = "Hoda", LastName = "Mahmoud", ClinicId = "clinic-1", Gender = "Female", DateOfBirth = "1998-04-12" });
            }
            if (!await db.Appointments.AnyAsync(a => a.Id == apptId))
            {
                db.Appointments.Add(new Appointment
                {
                    Id = apptId,
                    ClinicId = "clinic-1",
                    PatientId = "pat-queue-1",
                    DoctorId = "doc-test-1",
                    Date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Status = "scheduled",
                    Type = "General Consultation"
                });
            }
            await db.SaveChangesAsync();
        }

        var token = GenerateJwtToken(role: "assistant");

        // Act - Receptionist checks in arriving patient
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/appointments/{apptId}/check-in");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        // Assert - UAT-APT-02: Status becomes "waiting", QueueNumber assigned
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        Assert.Equal("waiting", data.GetProperty("status").GetString());
        Assert.NotNull(data.GetProperty("arrivedAt").GetString());
        Assert.True(data.GetProperty("queueNumber").GetInt32() >= 1);
    }

    [Fact]
    public async Task REQ_APT_02_StartAndCompleteConsultation_UpdatesStatusAndTimestamps()
    {
        // Arrange - Seed clinic and waiting appointment
        const string apptId = "appt-queue-test-2";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Clinics.AnyAsync(c => c.Id == "clinic-1"))
            {
                db.Clinics.Add(new ClinicEntity { Id = "clinic-1", Name = "Clinic 1", CreatorDoctorId = "doc-test-1" });
            }
            if (!await db.Appointments.AnyAsync(a => a.Id == apptId))
            {
                db.Appointments.Add(new Appointment
                {
                    Id = apptId,
                    ClinicId = "clinic-1",
                    PatientId = "pat-queue-1",
                    DoctorId = "doc-test-1",
                    Date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Status = "waiting",
                    ArrivedAt = DateTime.UtcNow.ToString("o"),
                    QueueNumber = 1
                });
            }
            await db.SaveChangesAsync();
        }

        var token = GenerateJwtToken();

        // 1. Doctor calls patient into consultation room
        var startReq = new HttpRequestMessage(HttpMethod.Post, $"/api/appointments/{apptId}/start-consultation");
        startReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var startRes = await _client.SendAsync(startReq);
        Assert.Equal(HttpStatusCode.OK, startRes.StatusCode);

        var startJson = await startRes.Content.ReadAsStringAsync();
        using var startDoc = JsonDocument.Parse(startJson);
        Assert.Equal("in_consultation", startDoc.RootElement.GetProperty("data").GetProperty("status").GetString());
        Assert.NotNull(startDoc.RootElement.GetProperty("data").GetProperty("consultationStartedAt").GetString());

        // 2. Doctor completes consultation
        var compReq = new HttpRequestMessage(HttpMethod.Post, $"/api/appointments/{apptId}/complete");
        compReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var compRes = await _client.SendAsync(compReq);
        Assert.Equal(HttpStatusCode.OK, compRes.StatusCode);

        var compJson = await compRes.Content.ReadAsStringAsync();
        using var compDoc = JsonDocument.Parse(compJson);
        Assert.Equal("completed", compDoc.RootElement.GetProperty("data").GetProperty("status").GetString());
        Assert.NotNull(compDoc.RootElement.GetProperty("data").GetProperty("consultationEndedAt").GetString());
    }

    [Fact]
    public async Task REQ_APT_02_GetLiveQueue_ReturnsActiveWaitingAndConsultingPatients()
    {
        // Arrange
        var token = GenerateJwtToken();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/appointments/live-queue?clinicId=clinic-1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("data", out var data));
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
    }
}
