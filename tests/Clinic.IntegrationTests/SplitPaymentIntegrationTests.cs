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

public class SplitPaymentIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public SplitPaymentIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private string GenerateJwtToken(string role = "admin", string? clinicId = "clinic-1")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("SuperSecretTestingKeyThatIsAtLeast32BytesLong!");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-cashier-1"),
            new(ClaimTypes.Name, "Cashier Sarah"),
            new(ClaimTypes.Role, role)
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
    public async Task REQ_BIL_02_AddPayment_RecordsSplitPaymentsAndUpdatesBalance()
    {
        const string invoiceId = "inv-split-test-1";
        const string patientId = "pat-split-1";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
            if (!await db.Clinics.AnyAsync(c => c.Id == "clinic-1"))
            {
                db.Clinics.Add(new ClinicEntity { Id = "clinic-1", Name = "Clinic 1" });
            }
            if (!await db.Patients.AnyAsync(p => p.Id == patientId))
            {
                db.Patients.Add(new Patient
                {
                    Id = patientId,
                    FirstName = "Nour",
                    LastName = "Eldin",
                    ClinicId = "clinic-1",
                    Gender = "Male",
                    ContactNumber = "+201011223344"
                });
            }
            if (!await db.BillingRecords.AnyAsync(b => b.Id == invoiceId))
            {
                db.BillingRecords.Add(new BillingRecord
                {
                    Id = invoiceId,
                    ClinicId = "clinic-1",
                    PatientId = patientId,
                    InvoiceNumber = "INV-2026-90001",
                    Amount = 300m,
                    Subtotal = 300m,
                    PaidAmount = 0m,
                    Status = "pending",
                    DateIssued = DateTime.UtcNow.ToString("o"),
                    PaymentMethod = "Cash"
                });
            }
            await db.SaveChangesAsync();
        }

        var token = GenerateJwtToken();

        // 1. Act - Add first payment: $100 Cash
        var request1 = new HttpRequestMessage(HttpMethod.Post, $"/api/billing/{invoiceId}/payments")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { Amount = 100m, PaymentMethod = "Cash" }),
                Encoding.UTF8,
                "application/json"
            )
        };
        request1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response1 = await _client.SendAsync(request1);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        var json1 = await response1.Content.ReadAsStringAsync();
        using var doc1 = JsonDocument.Parse(json1);
        var data1 = doc1.RootElement.GetProperty("data");
        Assert.Equal(100m, data1.GetProperty("paidAmount").GetDecimal());
        Assert.Equal("partially_paid", data1.GetProperty("status").GetString());

        // 2. Act - Add second payment: $200 Credit Card (Completing the bill)
        var request2 = new HttpRequestMessage(HttpMethod.Post, $"/api/billing/{invoiceId}/payments")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { Amount = 200m, PaymentMethod = "Credit Card" }),
                Encoding.UTF8,
                "application/json"
            )
        };
        request2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response2 = await _client.SendAsync(request2);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var json2 = await response2.Content.ReadAsStringAsync();
        using var doc2 = JsonDocument.Parse(json2);
        var data2 = doc2.RootElement.GetProperty("data");
        Assert.Equal(300m, data2.GetProperty("paidAmount").GetDecimal());
        Assert.Equal("paid", data2.GetProperty("status").GetString());
        Assert.Equal("Split Payment", data2.GetProperty("paymentMethod").GetString());
        Assert.Equal(2, data2.GetProperty("payments").GetArrayLength());
    }
}
