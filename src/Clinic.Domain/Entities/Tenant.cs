using System;
using System.Collections.Generic;

namespace Clinic.Domain.Entities;

public class Tenant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string OrganizationName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty; // e.g., "elite-dental" -> elite-dental.cliniccorp.ai
    
    // SaaS Subscription
    public string SubscriptionTier { get; set; } = "Starter"; // Starter, Pro, Enterprise
    public string StripeSubscriptionId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Customization & Whitelabel
    public string? LogoUrl { get; set; }
    public string? PrimaryColorHex { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ClinicEntity> Clinics { get; set; } = new List<ClinicEntity>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
