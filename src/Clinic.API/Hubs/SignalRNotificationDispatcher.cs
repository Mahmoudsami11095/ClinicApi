using System.Threading.Tasks;
using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace Clinic.API.Hubs;

public class SignalRNotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationDispatcher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(string userId, Notification notification)
    {
        await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification);
    }
}
