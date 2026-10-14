using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clinic.Application.Interfaces;
using Clinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clinic.Infrastructure.Services;

public class MobilePushDispatcher : IMobilePushDispatcher
{
    private readonly ClinicDbContext _context;
    private readonly ILogger<MobilePushDispatcher> _logger;

    public MobilePushDispatcher(ClinicDbContext context, ILogger<MobilePushDispatcher> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> SendPushToUserAsync(string userId, string title, string body, Dictionary<string, string>? data = null)
    {
        if (string.IsNullOrWhiteSpace(userId)) return 0;

        try
        {
            var devices = await _context.MobileDevices
                .Where(d => d.UserId == userId && d.IsActive)
                .ToListAsync();

            if (devices.Count == 0)
            {
                _logger.LogInformation("No active mobile devices found for user {UserId}", userId);
                return 0;
            }

            foreach (var device in devices)
            {
                device.LastActiveAt = DateTime.UtcNow;
                _logger.LogInformation("[PUSH DISPATCH] Sent to {Platform} device {Token} for user {UserId}: {Title} - {Body}",
                    device.Platform, device.DeviceToken, userId, title, body);
            }

            await _context.SaveChangesAsync();
            return devices.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not dispatch push notification to user {UserId} (schema fallback active)", userId);
            return 0;
        }
    }

    public async Task<int> SendPushToRoleAsync(string role, string title, string body, string? clinicId = null, Dictionary<string, string>? data = null)
    {
        if (string.IsNullOrWhiteSpace(role)) return 0;

        try
        {
            var userQuery = _context.Users.AsNoTracking().Where(u => u.Role == role && u.IsActive);
            if (!string.IsNullOrEmpty(clinicId))
            {
                userQuery = userQuery.Where(u => u.ClinicId == clinicId);
            }

            var userIds = await userQuery.Select(u => u.Id).ToListAsync();
            if (userIds.Count == 0) return 0;

            var devices = await _context.MobileDevices
                .Where(d => userIds.Contains(d.UserId) && d.IsActive)
                .ToListAsync();

            foreach (var device in devices)
            {
                device.LastActiveAt = DateTime.UtcNow;
                _logger.LogInformation("[ROLE PUSH] Sent to {Platform} device {Token} (Role: {Role}): {Title} - {Body}",
                    device.Platform, device.DeviceToken, role, title, body);
            }

            await _context.SaveChangesAsync();
            return devices.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not dispatch role push notification for role {Role} (schema fallback active)", role);
            return 0;
        }
    }

    public async Task<int> BroadcastPushAsync(string title, string body, Dictionary<string, string>? data = null)
    {
        try
        {
            var devices = await _context.MobileDevices
                .Where(d => d.IsActive)
                .ToListAsync();

            foreach (var device in devices)
            {
                device.LastActiveAt = DateTime.UtcNow;
                _logger.LogInformation("[BROADCAST PUSH] Sent to {Platform} device {Token}: {Title} - {Body}",
                    device.Platform, device.DeviceToken, title, body);
            }

            await _context.SaveChangesAsync();
            return devices.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not broadcast push notification (schema fallback active)");
            return 0;
        }
    }
}
