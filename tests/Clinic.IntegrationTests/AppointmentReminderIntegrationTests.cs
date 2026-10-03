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

public class AppointmentReminderIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AppointmentReminderIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task REQ_NOTIF_02_SendReminder_DispatchesReminder_AndUpdatesCount()
    {
        // Arrange - Seed clinic, patient, appointment
        const string apptId = "appt-rem-test-1";
        const string patId = "pat-rem-1";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Clinics.AnyAsync(c => c.Id == "clinic-1"))
            {
                db.Clinics.Add(new ClinicEntity { Id = "clinic-1", Name = "Clinic 1", CreatorDoctorId = "doc-test-1" });
            }
            if (!await db.Patients.AnyAsync(p => p.Id == patId))
            {
                db.Patients.Add(new Patient
                {
                    Id = patId,
                    FirstName = "Salma",
                    LastName = "Tariq",
                    ContactNumber = "+201099887766",
                    ClinicId = "clinic-1",
                    Gender = "Female",
                    DateOfBirth = "1994-06-15"
                });
            }
            if (!await db.Appointments.AnyAsync(a => a.Id == apptId))
            {
                db.Appointments.Add(new Appointment
                {
                    Id = apptId,
                    ClinicId = "clinic-1",
                    PatientId = patId,
                    DoctorId = "doc-test-1",
                    Date = DateTime.UtcNow.AddHours(18).ToString("yyyy-MM-ddTHH:mm:ss"),
                    Status = "scheduled",
                    Type = "Dental Consultation"
                });
            }
            await db.SaveChangesAsync();
        }

        var token = GenerateJwtToken();

        // Act - Trigger individual reminder
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/appointments/{apptId}/send-reminder");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        // Assert - REQ-NOTIF-02: Reminder dispatched and tracked
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        Assert.NotNull(data.GetProperty("lastReminderSentAt").GetString());
        Assert.True(data.GetProperty("reminderCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task REQ_NOTIF_02_SendBatchReminders_ProcessesUpcomingVisits()
    {
        // Arrange
        var token = GenerateJwtToken();

        // Act - Trigger batch reminders for the clinic
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/appointments/send-batch-reminders?clinicId=clinic-1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("count", out var countProp));
        Assert.True(countProp.GetInt32() >= 0);
    }
}
