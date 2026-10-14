using System;
using System.ComponentModel.DataAnnotations;

namespace Clinic.Application.DTOs;

public class RegisterMobileDeviceDto
{
    [Required]
    public string DeviceToken { get; set; } = string.Empty;

    public string Platform { get; set; } = "web";

    public string AppVersion { get; set; } = "7.0.0";
}

public class UnregisterMobileDeviceDto
{
    [Required]
    public string DeviceToken { get; set; } = string.Empty;
}

public class MobileDeviceResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActiveAt { get; set; }
}

public class SendTestPushNotificationDto
{
    [Required]
    public string Title { get; set; } = "Smart Clinic Alert";

    [Required]
    public string Body { get; set; } = "Test push notification from Smart Clinic.";

    public string? TargetUserId { get; set; }
}
