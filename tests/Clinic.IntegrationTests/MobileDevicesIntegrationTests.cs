using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Clinic.API.Controllers;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clinic.IntegrationTests;

public class MobileDevicesIntegrationTests
{
    private ClinicDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ClinicDbContext(options);
    }

    private MobileDevicesController CreateController(ClinicDbContext context, string userId = "test-user-1")
    {
        var controller = new MobileDevicesController(context);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "Test Doctor"),
            new Claim(ClaimTypes.Role, "doctor")
        }, "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return controller;
    }

    [Fact]
    public async Task RegisterDevice_WhenNew_ShouldPersistDeviceToken()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context, "user-42");

        var dto = new RegisterMobileDeviceDto
        {
            DeviceToken = "apns-token-xyz-123",
            Platform = "ios",
            AppVersion = "7.0.0"
        };

        // Act
        var result = await controller.RegisterDevice(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var savedDevice = await context.MobileDevices.FirstOrDefaultAsync(d => d.DeviceToken == "apns-token-xyz-123");
        Assert.NotNull(savedDevice);
        Assert.Equal("user-42", savedDevice.UserId);
        Assert.Equal("ios", savedDevice.Platform);
        Assert.True(savedDevice.IsActive);
    }

    [Fact]
    public async Task RegisterDevice_WhenExisting_ShouldUpdateLastActiveAndKeepActive()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var existingDevice = new MobileDevice
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "user-42",
            DeviceToken = "fcm-token-android-456",
            Platform = "android",
            AppVersion = "6.9.0",
            IsActive = false,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            LastActiveAt = DateTime.UtcNow.AddDays(-10)
        };
        context.MobileDevices.Add(existingDevice);
        await context.SaveChangesAsync();

        var controller = CreateController(context, "user-42");

        var dto = new RegisterMobileDeviceDto
        {
            DeviceToken = "fcm-token-android-456",
            Platform = "android",
            AppVersion = "7.0.0"
        };

        // Act
        var result = await controller.RegisterDevice(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = await context.MobileDevices.FirstAsync(d => d.DeviceToken == "fcm-token-android-456");
        Assert.True(updated.IsActive);
        Assert.Equal("7.0.0", updated.AppVersion);
    }

    [Fact]
    public async Task UnregisterDevice_ShouldSetIsActiveToFalse()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var device = new MobileDevice
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "user-42",
            DeviceToken = "token-to-deactivate",
            IsActive = true
        };
        context.MobileDevices.Add(device);
        await context.SaveChangesAsync();

        var controller = CreateController(context, "user-42");

        // Act
        var result = await controller.UnregisterDevice(new UnregisterMobileDeviceDto { DeviceToken = "token-to-deactivate" });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updated = await context.MobileDevices.FirstAsync(d => d.DeviceToken == "token-to-deactivate");
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task GetMyDevices_ShouldReturnOnlyActiveDevicesForUser()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.MobileDevices.AddRange(
            new MobileDevice { Id = "d1", UserId = "user-42", DeviceToken = "tok-1", IsActive = true, Platform = "ios" },
            new MobileDevice { Id = "d2", UserId = "user-42", DeviceToken = "tok-2", IsActive = false, Platform = "web" },
            new MobileDevice { Id = "d3", UserId = "other-user", DeviceToken = "tok-3", IsActive = true, Platform = "android" }
        );
        await context.SaveChangesAsync();

        var controller = CreateController(context, "user-42");

        // Act
        var result = await controller.GetMyDevices();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic data = okResult.Value!;
        var list = (IEnumerable<MobileDeviceResponseDto>)data.data;
        Assert.Single(list);
        Assert.Equal("tok-1", list.First().DeviceToken);
    }

    [Fact]
    public async Task SendTestNotification_ShouldReturnSimulationSuccess()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context, "user-42");

        var dto = new SendTestPushNotificationDto
        {
            Title = "Doctor Call Alert",
            Body = "Patient arrived for consultation"
        };

        // Act
        var result = await controller.SendTestNotification(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic data = okResult.Value!;
        Assert.True((bool)data.success);
        Assert.Equal("Doctor Call Alert", (string)data.title);
    }
}
