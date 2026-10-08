using System;

namespace Clinic.Domain.Entities;

public class DrugInteractionRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DrugA { get; set; } = string.Empty; // Generic molecule (e.g. "Ibuprofen")
    public string DrugB { get; set; } = string.Empty; // Generic molecule (e.g. "Warfarin")
    public string Severity { get; set; } = "Critical"; // "Critical", "Moderate", "Minor"
    public string ClinicalEffect { get; set; } = string.Empty;
    public string Mechanism { get; set; } = string.Empty;
    public string SuggestedAlternative { get; set; } = string.Empty;
    public string ReferenceAuthority { get; set; } = "FDA / BNF";
    public bool IsActive { get; set; } = true;
}
