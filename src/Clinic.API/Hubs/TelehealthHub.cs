using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Clinic.API.Hubs;

[Authorize]
public class TelehealthHub : Hub
{
    public async Task JoinTelehealthRoom(string roomToken)
    {
        if (!string.IsNullOrEmpty(roomToken))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomToken}");
            await Clients.OthersInGroup($"room_{roomToken}").SendAsync("UserJoinedRoom", Context.ConnectionId);
        }
    }

    public async Task LeaveTelehealthRoom(string roomToken)
    {
        if (!string.IsNullOrEmpty(roomToken))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room_{roomToken}");
            await Clients.OthersInGroup($"room_{roomToken}").SendAsync("UserLeftRoom", Context.ConnectionId);
        }
    }

    public async Task SendSignalOffer(string roomToken, object offer)
    {
        if (!string.IsNullOrEmpty(roomToken))
        {
            await Clients.OthersInGroup($"room_{roomToken}").SendAsync("ReceiveSignalOffer", offer);
        }
    }

    public async Task SendSignalAnswer(string roomToken, object answer)
    {
        if (!string.IsNullOrEmpty(roomToken))
        {
            await Clients.OthersInGroup($"room_{roomToken}").SendAsync("ReceiveSignalAnswer", answer);
        }
    }

    public async Task SendIceCandidate(string roomToken, object candidate)
    {
        if (!string.IsNullOrEmpty(roomToken))
        {
            await Clients.OthersInGroup($"room_{roomToken}").SendAsync("ReceiveIceCandidate", candidate);
        }
    }
}
