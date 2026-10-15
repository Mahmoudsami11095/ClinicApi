using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Clinic.Application.Interfaces;
using Clinic.Application.Services;
using Clinic.Domain.Entities;
using Moq;
using Xunit;

namespace Clinic.UnitTests;

public class NotificationUnitTests
{
    private readonly Mock<INotificationRepository> _mockRepo;
    private readonly Mock<INotificationDispatcher> _mockSignalRDispatcher;
    private readonly Mock<IMobilePushDispatcher> _mockPushDispatcher;
    private readonly NotificationService _service;

    public NotificationUnitTests()
    {
        _mockRepo = new Mock<INotificationRepository>();
        _mockSignalRDispatcher = new Mock<INotificationDispatcher>();
        _mockPushDispatcher = new Mock<IMobilePushDispatcher>();

        _service = new NotificationService(
            _mockRepo.Object,
            _mockSignalRDispatcher.Object,
            _mockPushDispatcher.Object
        );
    }

    [Fact]
    public void Notification_DefaultProperties_AreInitializedCorrectly()
    {
        var notif = new Notification
        {
            UserId = "user-1",
            Title = "Chair Ready",
            Message = "Operatory 2 is sanitized",
            Type = "Operatory"
        };

        Assert.NotEqual(Guid.Empty, notif.Id);
        Assert.False(notif.IsRead);
        Assert.Equal("Operatory", notif.Type);
        Assert.True(notif.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateNotificationAsync_PersistsDispatchesSignalRAndBridgesMobilePush()
    {
        // Arrange
        var createdNotif = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = "doc-1",
            Title = "Low Stock Alert",
            Message = "Dental composite is below threshold",
            Type = "Inventory"
        };

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Notification>()))
            .ReturnsAsync(createdNotif);

        // Act
        var result = await _service.CreateNotificationAsync("doc-1", "Low Stock Alert", "Dental composite is below threshold", "Inventory");

        // Assert
        Assert.NotNull(result);
        _mockRepo.Verify(r => r.AddAsync(It.Is<Notification>(n => n.UserId == "doc-1" && n.Type == "Inventory")), Times.Once);
        _mockSignalRDispatcher.Verify(d => d.SendNotificationAsync("doc-1", createdNotif), Times.Once);
        _mockPushDispatcher.Verify(p => p.SendPushToUserAsync(
            "doc-1",
            "Low Stock Alert",
            "Dental composite is below threshold",
            It.Is<Dictionary<string, string>>(data => data["type"] == "Inventory")
        ), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationExistsAndMatchesUser_UpdatesIsRead()
    {
        // Arrange
        var notifId = Guid.NewGuid();
        var existing = new Notification
        {
            Id = notifId,
            UserId = "doc-1",
            IsRead = false
        };

        _mockRepo.Setup(r => r.GetByIdAsync(notifId.ToString()))
            .ReturnsAsync(existing);

        // Act
        await _service.MarkAsReadAsync(notifId, "doc-1");

        // Assert
        Assert.True(existing.IsRead);
        _mockRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_UpdatesOnlyUnreadNotificationsForTargetUser()
    {
        // Arrange
        var notifs = new List<Notification>
        {
            new() { Id = Guid.NewGuid(), UserId = "doc-1", IsRead = false },
            new() { Id = Guid.NewGuid(), UserId = "doc-1", IsRead = true },
            new() { Id = Guid.NewGuid(), UserId = "other-user", IsRead = false }
        };

        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(notifs);

        // Act
        await _service.MarkAllAsReadAsync("doc-1");

        // Assert
        Assert.True(notifs[0].IsRead);
        Assert.True(notifs[1].IsRead);
        Assert.False(notifs[2].IsRead); // untouched
        _mockRepo.Verify(r => r.UpdateAsync(notifs[0]), Times.Once);
        _mockRepo.Verify(r => r.UpdateAsync(notifs[1]), Times.Never);
        _mockRepo.Verify(r => r.UpdateAsync(notifs[2]), Times.Never);
    }

    [Fact]
    public async Task DeleteNotificationAsync_WhenOwnedByUser_DeletesFromRepository()
    {
        // Arrange
        var notifId = Guid.NewGuid();
        var existing = new Notification
        {
            Id = notifId,
            UserId = "doc-1"
        };

        _mockRepo.Setup(r => r.GetByIdAsync(notifId.ToString())).ReturnsAsync(existing);

        // Act
        await _service.DeleteNotificationAsync(notifId, "doc-1");

        // Assert
        _mockRepo.Verify(r => r.DeleteAsync(notifId.ToString()), Times.Once);
    }

    [Fact]
    public async Task DeleteNotificationAsync_WhenNotOwnedByUser_DoesNotDelete()
    {
        // Arrange
        var notifId = Guid.NewGuid();
        var existing = new Notification
        {
            Id = notifId,
            UserId = "other-doc"
        };

        _mockRepo.Setup(r => r.GetByIdAsync(notifId.ToString())).ReturnsAsync(existing);

        // Act
        await _service.DeleteNotificationAsync(notifId, "doc-1");

        // Assert
        _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }
}
