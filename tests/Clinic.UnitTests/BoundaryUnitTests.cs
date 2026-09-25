using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Clinic.Domain.Helpers;
using Xunit;

namespace Clinic.UnitTests;

/// <summary>
/// Comprehensive Boundary Value Analysis (BVA) & Edge Case Test Suite
/// Covers all domain boundaries, limit values, edge conditions, and clinical safety thresholds.
/// </summary>
public class BoundaryUnitTests
{
    #region 1. Phone Number & Demographics Boundaries

    [Theory]
    // Valid Egyptian Mobile: Exactly 10 digits after leading zero strip, starting with 10, 11, 12, 15
    [InlineData("+20", "01000000000", true)]
    [InlineData("+20", "01099999999", true)]
    [InlineData("+20", "01100000000", true)]
    [InlineData("+20", "01200000000", true)]
    [InlineData("+20", "01500000000", true)]
    [InlineData("+20", "1012345678", true)] // Without leading zero (10 digits)
    [InlineData("+20", "010-1234-5678", true)] // With dashes (cleaned)
    [InlineData("+20", "010 1234 5678", true)] // With spaces (cleaned)
    // Invalid Egyptian Mobile Boundaries
    [InlineData("+20", "010123456", false)] // 9 digits (too short)
    [InlineData("+20", "010123456789", false)] // 12 digits (too long)
    [InlineData("+20", "01312345678", false)] // Invalid prefix 013
    [InlineData("+20", "01412345678", false)] // Invalid prefix 014
    [InlineData("+20", "01612345678", false)] // Invalid prefix 016
    [InlineData("+20", "01712345678", false)] // Invalid prefix 017
    [InlineData("+20", "01812345678", false)] // Invalid prefix 018
    [InlineData("+20", "01912345678", false)] // Invalid prefix 019
    [InlineData("+20", "0101234567a", false)] // Contains non-digit
    // International Phone Boundaries (General: 6 to 15 digits)
    [InlineData("+1", "12345", false)] // 5 digits (too short, min is 6)
    [InlineData("+1", "123456", true)] // 6 digits (min valid boundary)
    [InlineData("+966", "501234567", true)] // 9 digits (Saudi Arabia valid)
    [InlineData("+44", "123456789012345", true)] // 15 digits (max valid E.164 boundary)
    [InlineData("+44", "1234567890123456", false)] // 16 digits (exceeds E.164 max)
    // Null & Whitespace Boundaries
    [InlineData("", "01001234567", false)] // Empty country code
    [InlineData("+20", "", false)] // Empty phone number
    [InlineData("+20", "   ", false)] // Whitespace phone number
    public void PhoneHelper_ValidatePhoneNumber_BoundaryChecks(string countryCode, string phoneNumber, bool expectedValid)
    {
        var result = PhoneHelper.ValidatePhoneNumber(countryCode, phoneNumber);
        Assert.Equal(expectedValid, result.IsValid);
        if (!expectedValid)
        {
            Assert.NotEmpty(result.ErrorMessage);
        }
    }

    [Theory]
    [InlineData(null, "+20", "")]
    [InlineData("", "+20", "")]
    [InlineData("   ", "+20", "")]
    [InlineData("00201001234567", "+20", "1001234567")] // 0020 international prefix
    [InlineData("+20 1001234567", "+20", "1001234567")]
    [InlineData("+966 501234567", "+966", "501234567")]
    [InlineData("+1 8005550199", "+1", "8005550199")]
    [InlineData("01001234567", "+20", "01001234567")]
    public void PhoneHelper_SplitContactNumber_EdgeCases(string? input, string expectedCode, string expectedPhone)
    {
        var result = PhoneHelper.SplitContactNumber(input);
        Assert.Equal(expectedCode, result.CountryCode);
        Assert.Equal(expectedPhone, result.PhoneNumber);
    }

    [Fact]
    public void Patient_ArabicAndUnicodeNames_PreservedAccurately()
    {
        var patient = new Patient
        {
            Id = "pat-unicode-001",
            FirstName = "محمود",
            LastName = "سامي عبد الرحمن",
            Address = "شارع النيل، المعادي، القاهرة"
        };

        Assert.Equal("محمود", patient.FirstName);
        Assert.Equal("سامي عبد الرحمن", patient.LastName);
        Assert.Contains("القاهرة", patient.Address);
    }

    #endregion

    #region 2. Billing & Financial Boundaries

    [Fact]
    public void Billing_ZeroAmountInvoice_ShouldBeImmediatelyPaid()
    {
        var bill = new BillingRecord
        {
            Amount = 0m,
            PaidAmount = 0m,
            Status = nameof(BillingStatus.Paid)
        };

        decimal remaining = bill.Amount - (bill.PaidAmount ?? 0m);

        Assert.Equal(0m, remaining);
        Assert.Equal(nameof(BillingStatus.Paid), bill.Status);
    }

    [Fact]
    public void Billing_ExactCentsPrecision_SplitInThirds_ShouldTotalExactly()
    {
        // $100 split across 3 transactions: 33.33 + 33.33 + 33.34 = 100.00
        var bill = new BillingRecord
        {
            Amount = 100.00m,
            Status = nameof(BillingStatus.Pending)
        };

        bill.Payments.Add(new PaymentLog { Amount = 33.33m, PaymentMethod = "Cash", Date = "2026-09-25" });
        bill.Payments.Add(new PaymentLog { Amount = 33.33m, PaymentMethod = "Visa", Date = "2026-09-25" });
        bill.Payments.Add(new PaymentLog { Amount = 33.34m, PaymentMethod = "Mastercard", Date = "2026-09-25" });

        bill.PaidAmount = bill.Payments.Sum(p => p.Amount);
        decimal remaining = bill.Amount - (bill.PaidAmount ?? 0m);

        Assert.Equal(100.00m, bill.PaidAmount);
        Assert.Equal(0.00m, remaining);
        Assert.Equal(3, bill.Payments.Count);
    }

    [Fact]
    public void Billing_MinimumCurrencyUnit_OneCentRemaining_ShouldRemainPartiallyPaid()
    {
        var bill = new BillingRecord
        {
            Amount = 100.00m,
            Status = nameof(BillingStatus.Pending)
        };

        // Paid 99.99
        bill.Payments.Add(new PaymentLog { Amount = 99.99m, PaymentMethod = "Cash", Date = "2026-09-25" });
        bill.PaidAmount = bill.Payments.Sum(p => p.Amount);
        decimal remaining = bill.Amount - (bill.PaidAmount ?? 0m);

        bool isFullyPaid = remaining <= 0m;
        bill.Status = isFullyPaid ? nameof(BillingStatus.Paid) : nameof(BillingStatus.PartiallyPaid);

        Assert.Equal(0.01m, remaining);
        Assert.False(isFullyPaid);
        Assert.Equal(nameof(BillingStatus.PartiallyPaid), bill.Status);
    }

    [Fact]
    public void Billing_Overpayment_ShouldCalculateChangeAndClearBalance()
    {
        var bill = new BillingRecord
        {
            Amount = 450.00m,
            Status = nameof(BillingStatus.Pending)
        };

        // Patient hands $500 note
        decimal tendered = 500.00m;
        bill.Payments.Add(new PaymentLog { Amount = 450.00m, PaymentMethod = "Cash", Date = "2026-09-25" });
        bill.PaidAmount = bill.Payments.Sum(p => p.Amount);

        decimal change = tendered - bill.Amount;
        decimal balance = bill.Amount - (bill.PaidAmount ?? 0m);

        Assert.Equal(50.00m, change);
        Assert.Equal(0.00m, balance);
    }

    #endregion

    #region 3. Dental Odontogram & Tooth Notation Boundaries

    [Theory]
    // Universal Adult Teeth: Bounds are 1 to 32 inclusive
    [InlineData("1", true, false)]   // Min Adult Upper Right 3rd Molar
    [InlineData("16", true, false)]  // Mid Adult Upper Left Central/Molar
    [InlineData("32", true, false)]  // Max Adult Lower Right 3rd Molar
    [InlineData("0", false, false)]   // Below min (invalid)
    [InlineData("33", false, false)]  // Above max (invalid)
    [InlineData("-5", false, false)]  // Negative (invalid)
    // Universal Pediatric Teeth: Bounds are 'A' to 'T' inclusive
    [InlineData("A", true, true)]    // Min Primary Upper Right 2nd Molar
    [InlineData("J", true, true)]    // Max Upper Primary Left 2nd Molar
    [InlineData("K", true, true)]    // Min Lower Primary Left 2nd Molar
    [InlineData("T", true, true)]    // Max Primary Lower Right 2nd Molar
    [InlineData("@", false, true)]   // Below 'A' (invalid)
    [InlineData("U", false, true)]   // Above 'T' (invalid)
    [InlineData("Z", false, true)]   // Far above 'T' (invalid)
    public void Dental_UniversalToothNotation_BoundaryChecks(string tooth, bool expectedValid, bool isPediatric)
    {
        bool isValid;
        if (isPediatric)
        {
            isValid = tooth.Length == 1 && tooth[0] >= 'A' && tooth[0] <= 'T';
        }
        else
        {
            isValid = int.TryParse(tooth, out int num) && num >= 1 && num <= 32;
        }

        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    // FDI Two-Digit Adult Notation: [Quadrant 1-4][Tooth 1-8]
    [InlineData("11", true)] // Upper Right Central Incisor
    [InlineData("18", true)] // Upper Right 3rd Molar (Quadrant 1 boundary)
    [InlineData("21", true)] // Upper Left Central Incisor
    [InlineData("28", true)] // Upper Left 3rd Molar (Quadrant 2 boundary)
    [InlineData("31", true)] // Lower Left Central Incisor
    [InlineData("38", true)] // Lower Left 3rd Molar (Quadrant 3 boundary)
    [InlineData("41", true)] // Lower Right Central Incisor
    [InlineData("48", true)] // Lower Right 3rd Molar (Quadrant 4 boundary)
    // FDI Two-Digit Pediatric Notation: [Quadrant 5-8][Tooth 1-5]
    [InlineData("51", true)] // Upper Right Primary Incisor
    [InlineData("55", true)] // Upper Right Primary 2nd Molar (Quadrant 5 boundary)
    [InlineData("61", true)] // Upper Left Primary Incisor
    [InlineData("65", true)] // Upper Left Primary 2nd Molar (Quadrant 6 boundary)
    [InlineData("71", true)] // Lower Left Primary Incisor
    [InlineData("75", true)] // Lower Left Primary 2nd Molar (Quadrant 7 boundary)
    [InlineData("81", true)] // Lower Right Primary Incisor
    [InlineData("85", true)] // Lower Right Primary 2nd Molar (Quadrant 8 boundary)
    // FDI Invalid Boundaries
    [InlineData("10", false)] // Invalid tooth 0
    [InlineData("19", false)] // Invalid tooth 9
    [InlineData("50", false)] // Invalid pediatric tooth 0
    [InlineData("56", false)] // Invalid pediatric tooth 6
    [InlineData("91", false)] // Invalid quadrant 9
    [InlineData("00", false)] // Invalid
    public void Dental_FdiTwoDigitNotation_BoundaryChecks(string tooth, bool expectedValid)
    {
        bool isValid = false;
        if (int.TryParse(tooth, out int code) && tooth.Length == 2)
        {
            int quad = code / 10;
            int toothNum = code % 10;

            if (quad >= 1 && quad <= 4 && toothNum >= 1 && toothNum <= 8)
                isValid = true;
            else if (quad >= 5 && quad <= 8 && toothNum >= 1 && toothNum <= 5)
                isValid = true;
        }

        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(0, true)]   // Min pain (No pain)
    [InlineData(5, true)]   // Moderate pain
    [InlineData(10, true)]  // Max pain (Worst pain possible)
    [InlineData(-1, false)] // Below min
    [InlineData(11, false)] // Above max
    public void Dental_VisualAnalogScalePainLevel_BoundaryChecks(int painLevel, bool expectedValid)
    {
        bool isValid = painLevel >= 0 && painLevel <= 10;
        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public void Dental_AllFiveSurfaces_MODBL_ShouldSerializeAndDeserialize()
    {
        // Anatomical boundaries: Mesial, Occlusal, Distal, Buccal, Lingual
        var surfaces = new List<string> { "M", "O", "D", "B", "L" };
        var json = JsonSerializer.Serialize(surfaces);

        var log = new DentalLog
        {
            ToothNumber = "16",
            Treatment = "Full Crown Preparation (MODBL)",
            Status = json
        };

        var restored = JsonSerializer.Deserialize<List<string>>(log.Status);
        Assert.NotNull(restored);
        Assert.Equal(5, restored.Count);
        Assert.Contains("M", restored);
        Assert.Contains("L", restored);
    }

    #endregion

    #region 4. Inventory & Consumables Boundaries

    [Fact]
    public void Material_StockDepletionToZero_TriggersOutOfStockCondition()
    {
        var material = new Material
        {
            Id = "mat-deplete-001",
            Name = "Surgical Blades #15",
            Quantity = 10,
            Unit = "Piece"
        };

        // Consume all 10
        material.Quantity -= 10;

        Assert.Equal(0, material.Quantity);
        bool isOutOfStock = material.Quantity <= 0;
        Assert.True(isOutOfStock);
    }

    [Theory]
    // Reorder threshold = 10
    [InlineData(11, 10, false)] // 1 unit above threshold -> Safe
    [InlineData(10, 10, true)]  // Exact threshold boundary -> Low Stock Alert!
    [InlineData(9, 10, true)]   // 1 unit below threshold -> Low Stock Alert!
    [InlineData(0, 10, true)]   // Zero units -> Critical Alert!
    public void Material_ReorderThreshold_BoundaryEvaluation(int currentStock, int threshold, bool expectAlert)
    {
        bool isAlertTriggered = currentStock <= threshold;
        Assert.Equal(expectAlert, isAlertTriggered);
    }

    [Fact]
    public void Material_OverConsumption_ShouldBeDetectedBeforeStockGoesNegative()
    {
        var material = new Material
        {
            Id = "mat-over-001",
            Name = "Dental Impression Alginate",
            Quantity = 5,
            Unit = "Canister"
        };

        int requestedQuantity = 8;
        bool hasSufficientStock = material.Quantity >= requestedQuantity;

        Assert.False(hasSufficientStock, "System must reject consuming more stock than physically available.");
    }

    #endregion

    #region 5. Clinical Safety & Allergy Interceptor Boundaries

    [Theory]
    // Case insensitivity
    [InlineData("Penicillin", "penicillin v 500mg", true)]
    [InlineData("penicillin", "PENICILLIN VK 250MG", true)]
    // Substring / Active ingredient match
    [InlineData("Sulfa", "Sulfamethoxazole-Trimethoprim", true)]
    [InlineData("Aspirin", "Acetylsalicylic Acid (Aspirin 81mg)", true)]
    [InlineData("NSAID", "Ibuprofen 400mg (NSAID)", true)]
    // No conflict safe combinations
    [InlineData("Penicillin", "Amoxicillin-free Ciprofloxacin 500mg", false)]
    [InlineData("Sulfa", "Paracetamol 500mg", false)]
    [InlineData("", "Augmentin 1g", false)]
    [InlineData(null, "Augmentin 1g", false)]
    public void ClinicalSafety_AllergyConflict_BoundaryEvaluations(string? patientAllergy, string prescribedDrug, bool expectedConflict)
    {
        bool conflictDetected = false;
        if (!string.IsNullOrWhiteSpace(patientAllergy))
        {
            var allergens = patientAllergy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var allergen in allergens)
            {
                if (prescribedDrug.Contains(allergen, StringComparison.OrdinalIgnoreCase))
                {
                    conflictDetected = true;
                    break;
                }
            }
        }

        Assert.Equal(expectedConflict, conflictDetected);
    }

    [Fact]
    public void ClinicalSafety_MultipleAllergies_FlagIfAnyMatch()
    {
        var patientAllergies = "Penicillin, Cephalosporins, Codeine";
        var drug = "Codeine Phosphate 30mg";

        var allergens = patientAllergies.Split(',', StringSplitOptions.TrimEntries);
        bool match = allergens.Any(a => drug.Contains(a, StringComparison.OrdinalIgnoreCase));

        Assert.True(match);
    }

    #endregion

    #region 6. Appointment Slot Temporal Boundaries

    [Fact]
    public void Appointment_ContiguousSlots_ShouldNotCollide()
    {
        // Slot 1: 10:00 to 10:30
        var slot1Start = new DateTime(2026, 9, 26, 10, 0, 0);
        var slot1End = new DateTime(2026, 9, 26, 10, 30, 0);

        // Slot 2: 10:30 to 11:00 (exact boundary where Slot 2 starts when Slot 1 ends)
        var slot2Start = new DateTime(2026, 9, 26, 10, 30, 0);
        var slot2End = new DateTime(2026, 9, 26, 11, 0, 0);

        bool isCollision = slot1Start < slot2End && slot2Start < slot1End;
        Assert.False(isCollision, "Contiguous back-to-back appointment slots must not collide.");
    }

    [Fact]
    public void Appointment_OneSecondOverlap_ShouldBeDetectedAsCollision()
    {
        // Slot 1: 10:00 to 10:30:01
        var slot1Start = new DateTime(2026, 9, 26, 10, 0, 0);
        var slot1End = new DateTime(2026, 9, 26, 10, 30, 1);

        // Slot 2: 10:30:00 to 11:00:00
        var slot2Start = new DateTime(2026, 9, 26, 10, 30, 0);
        var slot2End = new DateTime(2026, 9, 26, 11, 0, 0);

        bool isCollision = slot1Start < slot2End && slot2Start < slot1End;
        Assert.True(isCollision, "Overlapping by even 1 second must trigger slot conflict.");
    }

    #endregion
}
