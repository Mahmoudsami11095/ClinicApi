using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Services;

public class CdsEngineService : ICdsEngineService
{
    private readonly ClinicDbContext _context;

    public CdsEngineService(ClinicDbContext context)
    {
        _context = context;
    }

    public async Task<List<DrugInteractionRule>> GetInteractionRulesAsync()
    {
        var rules = await _context.DrugInteractionRules
            .AsNoTracking()
            .Where(r => r.IsActive)
            .ToListAsync();

        if (!rules.Any())
        {
            var seedRules = GetBaselineRules();
            await _context.DrugInteractionRules.AddRangeAsync(seedRules);
            await _context.SaveChangesAsync();
            return seedRules;
        }

        return rules;
    }

    public async Task<CdsEvaluationResponseDto> EvaluatePrescriptionSafetyAsync(CdsEvaluationRequestDto request)
    {
        var response = new CdsEvaluationResponseDto();
        var rules = await GetInteractionRulesAsync();

        var prescribed = (request.PrescribedMolecules ?? new())
            .Select(m => m.Trim().ToLowerInvariant())
            .Where(m => !string.IsNullOrEmpty(m))
            .Distinct()
            .ToList();

        var chronicMedications = (request.PatientChronicMedications ?? new())
            .Select(m => m.Trim().ToLowerInvariant())
            .Where(m => !string.IsNullOrEmpty(m))
            .Distinct()
            .ToList();

        var chronicConditions = (request.PatientChronicConditions ?? new())
            .Select(c => c.Trim().ToLowerInvariant())
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .ToList();

        // ── 1. Vector A: Intra-Prescription Drug-Drug Interactions ──
        for (int i = 0; i < prescribed.Count; i++)
        {
            for (int j = i + 1; j < prescribed.Count; j++)
            {
                var d1 = prescribed[i];
                var d2 = prescribed[j];

                var matched = rules.FirstOrDefault(r =>
                    (r.DrugA.ToLowerInvariant() == d1 && r.DrugB.ToLowerInvariant() == d2) ||
                    (r.DrugA.ToLowerInvariant() == d2 && r.DrugB.ToLowerInvariant() == d1));

                if (matched != null)
                {
                    response.Alerts.Add(new CdsAlertDto
                    {
                        Severity = matched.Severity,
                        AlertType = "DrugDrugInteraction",
                        DrugA = matched.DrugA,
                        DrugB = matched.DrugB,
                        Title = $"Prescription DDI: {matched.DrugA} + {matched.DrugB}",
                        Message = $"Concurrent prescription of {matched.DrugA} and {matched.DrugB} triggers a {matched.Severity} interaction.",
                        ClinicalEffect = matched.ClinicalEffect,
                        SuggestedAlternative = matched.SuggestedAlternative
                    });
                }
            }
        }

        // ── 2. Vector B: Prescribed vs. Patient Chronic Medications ──
        foreach (var pDrug in prescribed)
        {
            foreach (var cDrug in chronicMedications)
            {
                var matched = rules.FirstOrDefault(r =>
                    (r.DrugA.ToLowerInvariant() == pDrug && r.DrugB.ToLowerInvariant() == cDrug) ||
                    (r.DrugA.ToLowerInvariant() == cDrug && r.DrugB.ToLowerInvariant() == pDrug));

                if (matched != null)
                {
                    response.Alerts.Add(new CdsAlertDto
                    {
                        Severity = matched.Severity,
                        AlertType = "DrugDrugInteraction",
                        DrugA = matched.DrugA,
                        DrugB = matched.DrugB,
                        Title = $"Chronic Medication Interaction: {matched.DrugA} with {matched.DrugB}",
                        Message = $"Prescribing {pDrug} conflicts with patient's chronic therapy ({cDrug}).",
                        ClinicalEffect = matched.ClinicalEffect,
                        SuggestedAlternative = matched.SuggestedAlternative
                    });
                }
            }
        }

        // ── 3. Vector C: Chronic Medical Conditions Contraindications ──
        foreach (var pDrug in prescribed)
        {
            // NSAIDs in Peptic Ulcer or Severe Renal Failure
            if ((pDrug.Contains("ibuprofen") || pDrug.Contains("ketorolac") || pDrug.Contains("diclofenac")) &&
                (chronicConditions.Any(c => c.Contains("ulcer") || c.Contains("renal") || c.Contains("kidney"))))
            {
                response.Alerts.Add(new CdsAlertDto
                {
                    Severity = "Critical",
                    AlertType = "DiseaseContraindication",
                    DrugA = pDrug,
                    Title = $"Disease Contraindication: NSAIDs in Ulcer / Renal Disease",
                    Message = $"{pDrug} is contraindicated in patients with active peptic ulcer or renal impairment.",
                    ClinicalEffect = "High risk of acute gastrointestinal perforation or worsening nephrotoxicity.",
                    SuggestedAlternative = "Paracetamol (Acetaminophen) 500mg-1000mg"
                });
            }

            // Tetracyclines in Pregnancy or Young Pediatrics
            if ((pDrug.Contains("doxycycline") || pDrug.Contains("tetracycline")) &&
                (chronicConditions.Any(c => c.Contains("pregnan")) || (request.PatientAgeYears.HasValue && request.PatientAgeYears.Value < 8)))
            {
                response.Alerts.Add(new CdsAlertDto
                {
                    Severity = "Critical",
                    AlertType = "DiseaseContraindication",
                    DrugA = pDrug,
                    Title = "Contraindication: Tetracyclines in Pregnancy / Pediatrics",
                    Message = $"{pDrug} is contraindicated during pregnancy and in children under 8 years of age.",
                    ClinicalEffect = "Permanent enamel hypoplasia and brown tooth discoloration; fetal bone growth restriction.",
                    SuggestedAlternative = "Amoxicillin or Clarithromycin"
                });
            }
        }

        // ── 4. Pediatric & Weight-Based Dosing Engine ──
        if (request.PatientWeightKg.HasValue && request.PatientWeightKg.Value > 0)
        {
            foreach (var pDrug in prescribed)
            {
                var dosing = CalculatePediatricDose(pDrug, request.PatientWeightKg.Value, request.PatientAgeYears ?? 10);
                if (dosing.CalculatedDoseMg > 0)
                {
                    response.PediatricSuggestions.Add(dosing);
                }
            }
        }

        response.TotalAlertsCount = response.Alerts.Count;
        response.HasCriticalAlerts = response.Alerts.Any(a => a.Severity == "Critical");
        response.IsSafe = !response.HasCriticalAlerts;

        return response;
    }

    public PediatricDosingSuggestionDto CalculatePediatricDose(string drugName, double weightKg, int ageYears)
    {
        var cleanName = drugName.ToLowerInvariant();
        double mgPerKg = 0;
        double maxAdult = 500;
        string interval = "Every 8 hours (TID)";

        if (cleanName.Contains("amoxicillin"))
        {
            mgPerKg = 40.0 / 3.0; // ~13.3 mg/kg per dose
            maxAdult = 500.0;
            interval = "Every 8 hours (TID)";
        }
        else if (cleanName.Contains("ibuprofen"))
        {
            mgPerKg = 10.0;
            maxAdult = 400.0;
            interval = "Every 6-8 hours with food";
        }
        else if (cleanName.Contains("paracetamol") || cleanName.Contains("acetaminophen"))
        {
            mgPerKg = 15.0;
            maxAdult = 1000.0;
            interval = "Every 4-6 hours (max 4 doses/day)";
        }
        else
        {
            return new PediatricDosingSuggestionDto
            {
                DrugName = drugName,
                RecommendedMgPerKg = 0,
                CalculatedDoseMg = 0,
                MaxAdultDoseMg = 0,
                FinalCappedDoseMg = 0
            };
        }

        var calculated = Math.Round(weightKg * mgPerKg, 0);
        var finalCapped = Math.Min(calculated, maxAdult); // BR-CDS-03: Capped at adult max

        return new PediatricDosingSuggestionDto
        {
            DrugName = drugName,
            RecommendedMgPerKg = Math.Round(mgPerKg, 1),
            CalculatedDoseMg = calculated,
            MaxAdultDoseMg = maxAdult,
            FinalCappedDoseMg = finalCapped,
            DosingInterval = interval
        };
    }

    private static List<DrugInteractionRule> GetBaselineRules()
    {
        return new List<DrugInteractionRule>
        {
            new()
            {
                DrugA = "Ibuprofen",
                DrugB = "Warfarin",
                Severity = "Critical",
                ClinicalEffect = "Severe gastrointestinal bleeding and life-threatening INR elongation.",
                Mechanism = "Competitive protein binding displacement and additive platelet aggregation inhibition.",
                SuggestedAlternative = "Paracetamol (Acetaminophen) up to 1000mg"
            },
            new()
            {
                DrugA = "Ketorolac",
                DrugB = "Aspirin",
                Severity = "Critical",
                ClinicalEffect = "Profound gastric mucosal ulceration and massive hemorrhage risk.",
                Mechanism = "Synergistic dual non-selective COX-1/COX-2 enzyme blockade.",
                SuggestedAlternative = "Paracetamol + Codeine"
            },
            new()
            {
                DrugA = "Clarithromycin",
                DrugB = "Atorvastatin",
                Severity = "Critical",
                ClinicalEffect = "Acute rhabdomyolysis, severe myopathy, and acute kidney injury.",
                Mechanism = "Potent CYP3A4 hepatic enzyme inhibition leading to massive statin plasma elevation.",
                SuggestedAlternative = "Azithromycin or Amoxicillin"
            },
            new()
            {
                DrugA = "Metronidazole",
                DrugB = "Warfarin",
                Severity = "Critical",
                ClinicalEffect = "Severe hemorrhagic episodes and dangerously elevated INR.",
                Mechanism = "Inhibition of CYP2C9 metabolism of the active S-enantiomer of warfarin.",
                SuggestedAlternative = "Amoxicillin-Clavulanic Acid"
            },
            new()
            {
                DrugA = "Amoxicillin",
                DrugB = "Methotrexate",
                Severity = "Moderate",
                ClinicalEffect = "Methotrexate toxicity resulting in leukopenia and stomatitis.",
                Mechanism = "Penicillin blocks renal tubular secretion of methotrexate.",
                SuggestedAlternative = "Clindamycin"
            }
        };
    }
}
