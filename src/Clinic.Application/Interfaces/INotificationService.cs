using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Clinic.Domain.Entities;

namespace Clinic.Application.Interfaces;

public interface INotificationService
{
    Task<Notification> CreateNotificationAsync(string userId, string title, string message, string type);
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, int count = 20);
    Task MarkAsReadAsync(Guid notificationId, string userId);
    Task MarkAllAsReadAsync(string userId);
}
