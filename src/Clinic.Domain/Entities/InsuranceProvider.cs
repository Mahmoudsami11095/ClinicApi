using System;
using System.Collections.Generic;

namespace Clinic.Domain.Entities;

public class InsuranceProvider
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string PayerCode { get; set; } = string.Empty;
    public decimal PreAuthThreshold { get; set; } = 1500m; // EGP threshold requiring pre-authorization
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public List<InsuranceClaim> Claims { get; set; } = new();
}
