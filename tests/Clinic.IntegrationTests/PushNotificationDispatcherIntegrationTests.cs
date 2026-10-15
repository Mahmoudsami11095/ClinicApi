using System;
using System.Threading.Tasks;
using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Clinic.Infrastructure.Data;
using Clinic.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clinic.IntegrationTests;

public class PushNotificationDispatcherIntegrationTests
{
    private ClinicDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ClinicDbContext(options);
    }

    [Fact]
    public async Task SendPushToUserAsync_ShouldTargetOnlyActiveDevicesForUser()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var dispatcher = new MobilePushDispatcher(context, new NullLogger<MobilePushDispatcher>());

        context.MobileDevices.AddRange(
            new MobileDevice { Id = "1", UserId = "user-abc", DeviceToken = "tok-1", IsActive = true, Platform = "ios" },
            new MobileDevice { Id = "2", UserId = "user-abc", DeviceToken = "tok-2", IsActive = false, Platform = "android" },
            new MobileDevice { Id = "3", UserId = "user-other", DeviceToken = "tok-3", IsActive = true, Platform = "web" }
        );
        await context.SaveChangesAsync();

        // Act
        var dispatchedCount = await dispatcher.SendPushToUserAsync("user-abc", "Test", "Message");

        // Assert
        Assert.Equal(1, dispatchedCount);
        
        // Verify LastActiveAt was updated on the dispatched device
        var activeDevice = await context.MobileDevices.FindAsync("1");
        Assert.True(activeDevice!.LastActiveAt > DateTime.UtcNow.AddSeconds(-2));
    }

    [Fact]
    public async Task SendPushToRoleAsync_ShouldTargetOnlyActiveDevicesForSpecificRole()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var dispatcher = new MobilePushDispatcher(context, new NullLogger<MobilePushDispatcher>());

        context.Users.AddRange(
            new User { Id = "doc-1", Role = UserRole.Doctor, IsDeleted = false },
            new User { Id = "asst-1", Role = UserRole.Assistant, IsDeleted = false, ClinicId = "clinic-x" },
            new User { Id = "asst-2", Role = UserRole.Assistant, IsDeleted = false, ClinicId = "clinic-y" }
        );
        
        context.MobileDevices.AddRange(
            new MobileDevice { Id = "1", UserId = "doc-1", DeviceToken = "tok-d", IsActive = true },
            new MobileDevice { Id = "2", UserId = "asst-1", DeviceToken = "tok-a1", IsActive = true },
            new MobileDevice { Id = "3", UserId = "asst-2", DeviceToken = "tok-a2", IsActive = true }
        );
        await context.SaveChangesAsync();

        // Act - send to all assistants in clinic-x
        var count = await dispatcher.SendPushToRoleAsync("assistant", "Sterilize", "Action req", "clinic-x");

        // Assert
        Assert.Equal(1, count); // Should only hit asst-1
    }

    [Fact]
    public async Task BroadcastPushAsync_ShouldTargetAllActiveDevices()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var dispatcher = new MobilePushDispatcher(context, new NullLogger<MobilePushDispatcher>());

        context.MobileDevices.AddRange(
            new MobileDevice { Id = "1", UserId = "u1", IsActive = true },
            new MobileDevice { Id = "2", UserId = "u2", IsActive = true },
            new MobileDevice { Id = "3", UserId = "u3", IsActive = false }
        );
        await context.SaveChangesAsync();

        // Act
        var count = await dispatcher.BroadcastPushAsync("Emergency", "Evacuate");

        // Assert
        Assert.Equal(2, count);
    }
}
