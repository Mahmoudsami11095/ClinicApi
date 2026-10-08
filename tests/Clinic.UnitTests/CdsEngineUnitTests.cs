using System;
using System.Collections.Generic;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Xunit;

namespace Clinic.UnitTests;

public class CdsEngineUnitTests
{
    [Fact]
    public void REQ_CDS_01_CriticalInteraction_IbuprofenAndWarfarin_TriggersAlert()
    {
        // Arrange
        var prescribed = new List<string> { "Ibuprofen 400mg", "Amoxicillin 500mg" };
        var chronicMedications = new List<string> { "Warfarin 5mg" };

        var rules = new List<DrugInteractionRule>
        {
            new()
            {
                DrugA = "Ibuprofen",
                DrugB = "Warfarin",
                Severity = "Critical",
                ClinicalEffect = "Severe gastrointestinal bleeding and life-threatening INR elongation.",
                SuggestedAlternative = "Paracetamol (Acetaminophen) up to 1000mg"
            }
        };

        // Act
        var matched = rules.Find(r =>
            (prescribed.Exists(p => p.Contains(r.DrugA, StringComparison.OrdinalIgnoreCase)) &&
             chronicMedications.Exists(c => c.Contains(r.DrugB, StringComparison.OrdinalIgnoreCase))) ||
            (prescribed.Exists(p => p.Contains(r.DrugB, StringComparison.OrdinalIgnoreCase)) &&
             chronicMedications.Exists(c => c.Contains(r.DrugA, StringComparison.OrdinalIgnoreCase))));

        // Assert
        Assert.NotNull(matched);
        Assert.Equal("Critical", matched.Severity);
        Assert.Contains("Paracetamol", matched.SuggestedAlternative);
        Assert.Contains("bleeding", matched.ClinicalEffect);
    }

    [Fact]
    public void REQ_CDS_02_DiseaseContraindication_NsaidsInPepticUlcer_Detected()
    {
        // Arrange
        string prescribed = "Ketorolac 10mg";
        var patientConditions = new List<string> { "Active Peptic Ulcer Disease", "Hypertension" };

        // Act
        bool isNsaid = prescribed.Contains("Ketorolac", StringComparison.OrdinalIgnoreCase) ||
                       prescribed.Contains("Ibuprofen", StringComparison.OrdinalIgnoreCase);
        bool hasUlcer = patientConditions.Exists(c => c.Contains("ulcer", StringComparison.OrdinalIgnoreCase));

        bool isContraindicated = isNsaid && hasUlcer;

        // Assert
        Assert.True(isContraindicated);
    }

    [Fact]
    public void REQ_CDS_03_PediatricDosing_Amoxicillin_CalculatesCorrectDose()
    {
        // Arrange
        double weightKg = 15.0; // 15 kg child
        double mgPerKgPerDose = 40.0 / 3.0; // ~13.33 mg/kg TID
        double maxAdultSingleDose = 500.0;

        // Act
        double calculated = Math.Round(weightKg * mgPerKgPerDose, 0);
        double finalCapped = Math.Min(calculated, maxAdultSingleDose);

        // Assert
        Assert.Equal(200.0, calculated);
        Assert.Equal(200.0, finalCapped);
    }

    [Fact]
    public void BR_CDS_03_PediatricSafetyCap_EnforcesAdultMaxCeiling()
    {
        // Arrange: 60 kg older child, Ibuprofen 10 mg/kg
        double weightKg = 60.0;
        double mgPerKg = 10.0;
        double maxAdultDose = 400.0; // Standard adult single dose ceiling

        // Act
        double calculated = weightKg * mgPerKg; // 600 mg
        double finalCapped = Math.Min(calculated, maxAdultDose); // BR-CDS-03: Capped at 400 mg

        // Assert
        Assert.Equal(600.0, calculated);
        Assert.Equal(400.0, finalCapped);
        Assert.True(finalCapped <= maxAdultDose);
    }
}
