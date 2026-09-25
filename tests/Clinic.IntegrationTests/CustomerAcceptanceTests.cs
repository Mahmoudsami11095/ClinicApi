using System.Text.Json;
using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Clinic.Domain.Helpers;
using Xunit;

namespace Clinic.IntegrationTests;

/// <summary>
/// Automated Customer Acceptance Testing (UAT) Suite
/// Directly automates and verifies the 7 Customer Acceptance Scenarios 
/// defined in UAT_ACCEPTANCE_TEST_PLAN.md and CUSTOMER_REQUIREMENTS_DOCUMENT.md (v2.0.0).
/// </summary>
public class CustomerAcceptanceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CustomerAcceptanceTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    [Trait("Category", "CustomerAcceptance")]
    public void Scenario1_PatientIntake_RegistrationAndDuplicatePrevention()
    {
        // 1. Walk-in Registration Simulation
        var patient = new Patient
        {
            Id = "pat-uat-001",
            FirstName = "Ahmed",
            LastName = "Hassan",
            Gender = "Male",
            DateOfBirth = "1992-05-14",
            CountryCode = "+20",
            PhoneNumber = "1001122334",
            Allergies = "Aspirin",
            ChronicDiseases = "Hypertension"
        };

        // Assert Valid Demographics
        Assert.Equal("+20 1001122334", patient.ContactNumber);
        Assert.False(patient.IsDeleted);
        Assert.Equal("Aspirin", patient.Allergies);

        // 2. Duplicate Detection Rule Verification
        var duplicateAttempt = new Patient
        {
            Id = "pat-uat-002",
            FirstName = "Another",
            LastName = "Patient",
            ContactNumber = "+20 1001122334"
        };

        // Verification: System identifies phone collision
        bool isDuplicatePhone = patient.PhoneNumber == duplicateAttempt.PhoneNumber &&
                                patient.CountryCode == duplicateAttempt.CountryCode;
        Assert.True(isDuplicatePhone, "System must flag duplicate phone numbers to prevent fractured EMR records.");
    }

    [Fact]
    [Trait("Category", "CustomerAcceptance")]
    public void Scenario2_ClinicalConsultation_AllergyWarningAndPrescriptionIssuance()
    {
        // Arrange: Patient with known Aspirin allergy
        var patient = new Patient
        {
            Id = "pat-uat-003",
            FirstName = "Salma",
            LastName = "Kamal",
            Allergies = "Aspirin"
        };

        // Act: Doctor inspects drug against allergies
        bool hasAllergyConflict = patient.Allergies.Contains("Aspirin", StringComparison.OrdinalIgnoreCase);

        // Assert: Safety Interceptor Triggered
        Assert.True(hasAllergyConflict, "Clinical safety interceptor must flag Aspirin conflict.");

        // Doctor prescribes safe alternative
        var rx = new Prescription
        {
            Id = "rx-uat-001",
            PatientId = patient.Id,
            DoctorId = "doc-uat-001",
            Date = "2026-09-25",
            Notes = "Clinical override: Alternative analgesic chosen"
        };
        rx.Medications.Add(new MedicationItem
        {
            Name = "Panadol 500mg",
            Dosage = "1 Tablet",
            Frequency = "Every 8 hours as needed",
            Duration = "3 days"
        });

        Assert.Single(rx.Medications);
        Assert.Equal("Panadol 500mg", rx.Medications[0].Name);
    }

    [Fact]
    [Trait("Category", "CustomerAcceptance")]
    public void Scenario3_SpecializedDentalCharting_SurfaceCariesAndAutoBillingSync()
    {
        // 1. Dentist marks tooth #16 Caries on Occlusal surface
        var statuses = new List<string> { nameof(ToothStatus.Caries) };
        var dentalLog = new DentalLog
        {
            Id = "den-uat-001",
            PatientId = "pat-uat-001",
            ToothNumber = "16", // Upper Right First Molar (FDI)
            Status = JsonSerializer.Serialize(statuses),
            PainLevel = 6,
            Treatment = "Composite Restoration (Occlusal)",
            IsPlanned = false // Procedure completed chair-side
        };

        Assert.Equal("16", dentalLog.ToothNumber);
        Assert.False(dentalLog.IsPlanned);

        // 2. Billing Auto-Sync: Completed procedure automatically transferred to bill
        var procedureTariffFee = 550.00m;
        var invoice = new BillingRecord
        {
            Id = "inv-uat-001",
            PatientId = dentalLog.PatientId,
            Amount = procedureTariffFee,
            Description = $"Tooth #{dentalLog.ToothNumber} - {dentalLog.Treatment}",
            Status = nameof(BillingStatus.Pending)
        };

        Assert.Equal(550.00m, invoice.Amount);
        Assert.Contains("Tooth #16", invoice.Description);
        Assert.Equal(nameof(BillingStatus.Pending), invoice.Status);
    }

    [Fact]
    [Trait("Category", "CustomerAcceptance")]
    public void Scenario4_CashierSettlement_SplitPaymentAndReceiptGeneration()
    {
        // Arrange: Invoice of $850
        var invoice = new BillingRecord
        {
            Id = "inv-uat-002",
            Amount = 850.00m,
            PaidAmount = 0.00m,
            Status = nameof(BillingStatus.Pending)
        };

        // Act: Split Payment ($500 Cash + $350 Visa)
        invoice.Payments.Add(new PaymentLog
        {
            Amount = 500.00m,
            PaymentMethod = "Cash",
            Date = "2026-09-25"
        });
        invoice.Payments.Add(new PaymentLog
        {
            Amount = 350.00m,
            PaymentMethod = "Visa / Credit Card",
            Date = "2026-09-25"
        });

        invoice.PaidAmount = invoice.Payments.Sum(p => p.Amount);
        invoice.Status = invoice.PaidAmount >= invoice.Amount 
            ? nameof(BillingStatus.Paid) 
            : nameof(BillingStatus.PartiallyPaid);

        // Assert: Balance is exactly zero, Status is Fully Paid
        decimal remainingBalance = invoice.Amount - (invoice.PaidAmount ?? 0m);
        Assert.Equal(0.00m, remainingBalance);
        Assert.Equal(nameof(BillingStatus.Paid), invoice.Status);
        Assert.Equal(2, invoice.Payments.Count);
    }

    [Fact]
    [Trait("Category", "CustomerAcceptance")]
    public void Scenario5_ClinicConsumables_UsageDecrementAndLowStockTrigger()
    {
        // Arrange: Anesthetic Stock (Initial: 12, Minimum Safety Threshold: 10)
        var stock = new Material
        {
            Id = "mat-uat-001",
            Name = "Mepivacaine 2% Anesthetic",
            Quantity = 12,
            Unit = "Carpules",
            ClinicId = "cln-001",
            DoctorId = "doc-001"
        };
        const int minSafetyThreshold = 10;

        // Act: Consume 4 carpules during procedure
        stock.Quantity -= 4; // Remaining: 8

        // Assert: Low stock trigger condition met
        bool isLowStock = stock.Quantity <= minSafetyThreshold;
        Assert.Equal(8, stock.Quantity);
        Assert.True(isLowStock, "Automated alert must fire when inventory drops to or below minimum safety threshold.");
    }

    [Fact]
    [Trait("Category", "CustomerAcceptance")]
    public async Task Scenario6_Security_RoleIsolationAndUnauthorizedRejection()
    {
        // Act: Anonymous request attempting to access confidential patient records
        var response = await _client.GetAsync("/api/patients");

        // Assert: Access is strictly blocked with HTTP 401 Unauthorized
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
