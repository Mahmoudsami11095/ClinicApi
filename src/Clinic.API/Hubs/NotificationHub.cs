using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Clinic.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override Task OnConnectedAsync()
    {
        return base.OnConnectedAsync();
    }

    public async Task JoinClinicRoom(string clinicId)
    {
        if (!string.IsNullOrEmpty(clinicId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"clinic_{clinicId}");
        }
    }

    public async Task LeaveClinicRoom(string clinicId)
    {
        if (!string.IsNullOrEmpty(clinicId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"clinic_{clinicId}");
        }
    }

    public async Task BroadcastChairStatus(string clinicId, object chairUpdate)
    {
        if (!string.IsNullOrEmpty(clinicId))
        {
            await Clients.Group($"clinic_{clinicId}").SendAsync("ReceiveChairStatusUpdate", chairUpdate);
        }
        else
        {
            await Clients.All.SendAsync("ReceiveChairStatusUpdate", chairUpdate);
        }
    }
}
