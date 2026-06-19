using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;

namespace Clinic.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public NotificationService(
        INotificationRepository notificationRepository,
        INotificationDispatcher notificationDispatcher)
    {
        _notificationRepository = notificationRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Notification> CreateNotificationAsync(string userId, string title, string message, string type)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _notificationRepository.AddAsync(notification);

        // Dispatch via SignalR
        await _notificationDispatcher.SendNotificationAsync(userId, created);

        return created;
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, int count = 20)
    {
        // Simple fetch all and filter for now. In a real app, the repository should have a GetByUserIdAsync method with pagination.
        var all = await _notificationRepository.GetAllAsync();
        var userNotifs = all.FindAll(n => n.UserId == userId);
        userNotifs.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
        
        return userNotifs.GetRange(0, Math.Min(count, userNotifs.Count));
    }

    public async Task MarkAsReadAsync(Guid notificationId, string userId)
    {
        var notif = await _notificationRepository.GetByIdAsync(notificationId.ToString());
        if (notif != null && notif.UserId == userId)
        {
            notif.IsRead = true;
            await _notificationRepository.UpdateAsync(notif);
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var all = await _notificationRepository.GetAllAsync();
        foreach (var notif in all)
        {
            if (notif.UserId == userId && !notif.IsRead)
            {
                notif.IsRead = true;
                await _notificationRepository.UpdateAsync(notif);
            }
        }
    }
}
