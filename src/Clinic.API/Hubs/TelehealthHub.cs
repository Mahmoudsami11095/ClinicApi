using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Clinic.API.Hubs;

[Authorize]
public class TelehealthHub : Hub
{
    public async Task JoinTelehealthRoom(string roomToken, string? role = null, string? participantName = null)
    {
        if (!string.IsNullOrEmpty(roomToken))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomToken}");
            await Clients.OthersInGroup($"room_{roomToken}").SendAsync("UserJoinedRoom", new
            {
                ConnectionId = Context.ConnectionId,
                Role = role ?? "Participant",
                ParticipantName = participantName ?? "User"
            });
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

    // ── Release v12.0.0: Diagnostic Co-Viewer & Synchronized Annotations (BR-TELE-02) ──

    public async Task BroadcastViewportSync(string roomToken, ViewportSyncDto syncPayload)
    {
        if (!string.IsNullOrEmpty(roomToken) && syncPayload != null)
        {
            await Clients.OthersInGroup($"room_{roomToken}").SendAsync("ReceiveViewportSync", syncPayload);
        }
    }

    public async Task BroadcastSharedAnnotation(string roomToken, SharedAnnotationDto annotationPayload)
    {
        if (!string.IsNullOrEmpty(roomToken) && annotationPayload != null)
        {
            await Clients.OthersInGroup($"room_{roomToken}").SendAsync("ReceiveSharedAnnotation", annotationPayload);
        }
    }

    public async Task ReportNetworkQuality(string roomToken, string connectionId, int rttMs, double packetLossPct, string resolutionTier)
    {
        if (!string.IsNullOrEmpty(roomToken))
        {
            await Clients.Group($"room_{roomToken}").SendAsync("ReceiveNetworkQuality", new
            {
                ConnectionId = connectionId,
                RttMs = rttMs,
                PacketLossPct = packetLossPct,
                ResolutionTier = resolutionTier
            });
        }
    }
}
