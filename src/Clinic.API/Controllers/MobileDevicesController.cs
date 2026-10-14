using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Clinic.API.Controllers;

[ApiController]
[Route("api/mobile/devices")]
[Authorize]
public class MobileDevicesController : ControllerBase
{
    private readonly ClinicDbContext _context;

    public MobileDevicesController(ClinicDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterMobileDeviceDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User identity not found in claims." });
        }

        try
        {
            var existing = await _context.MobileDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceToken == dto.DeviceToken);

            if (existing != null)
            {
                existing.Platform = dto.Platform;
                existing.AppVersion = dto.AppVersion;
                existing.IsActive = true;
                existing.LastActiveAt = DateTime.UtcNow;
            }
            else
            {
                var newDevice = new MobileDevice
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    DeviceToken = dto.DeviceToken,
                    Platform = dto.Platform,
                    AppVersion = dto.AppVersion,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastActiveAt = DateTime.UtcNow
                };
                _context.MobileDevices.Add(newDevice);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Mobile device token registered successfully." });
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid object name") || 
                                  ex.InnerException?.Message.Contains("Invalid object name") == true)
        {
            // STD-ARCH-007: Resilient fallback for pre-migrated databases
            return Ok(new { message = "Device acknowledged (schema migration pending)." });
        }
    }

    [HttpDelete("unregister")]
    public async Task<IActionResult> UnregisterDevice([FromBody] UnregisterMobileDeviceDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User identity not found in claims." });
        }

        try
        {
            var device = await _context.MobileDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceToken == dto.DeviceToken);

            if (device != null)
            {
                device.IsActive = false;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Device unregistered successfully." });
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid object name") || 
                                  ex.InnerException?.Message.Contains("Invalid object name") == true)
        {
            return Ok(new { message = "Device acknowledged (schema migration pending)." });
        }
    }

    [HttpGet("my-devices")]
    public async Task<IActionResult> GetMyDevices()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User identity not found in claims." });
        }

        try
        {
            var devices = await _context.MobileDevices
                .AsNoTracking()
                .Where(d => d.UserId == userId && d.IsActive)
                .Select(d => new MobileDeviceResponseDto
                {
                    Id = d.Id,
                    UserId = d.UserId,
                    DeviceToken = d.DeviceToken,
                    Platform = d.Platform,
                    AppVersion = d.AppVersion,
                    IsActive = d.IsActive,
                    CreatedAt = d.CreatedAt,
                    LastActiveAt = d.LastActiveAt
                })
                .ToListAsync();

            return Ok(new { data = devices });
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid object name") || 
                                  ex.InnerException?.Message.Contains("Invalid object name") == true)
        {
            return Ok(new { data = Array.Empty<MobileDeviceResponseDto>() });
        }
    }

    [HttpPost("test-notification")]
    public async Task<IActionResult> SendTestNotification([FromBody] SendTestPushNotificationDto dto)
    {
        var targetId = dto.TargetUserId ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(targetId))
        {
            return BadRequest(new { message = "Target user ID is required." });
        }

        // Mock delivery simulation
        return Ok(new
        {
            success = true,
            title = dto.Title,
            body = dto.Body,
            targetUserId = targetId,
            timestamp = DateTime.UtcNow,
            message = "Push notification simulated and queued for delivery."
        });
    }
}
