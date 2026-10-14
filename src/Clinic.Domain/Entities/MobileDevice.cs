using System;

namespace Clinic.Domain.Entities;

public class MobileDevice
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = "web"; // ios, android, web
    public string AppVersion { get; set; } = "7.0.0";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User User { get; set; } = null!;
}
