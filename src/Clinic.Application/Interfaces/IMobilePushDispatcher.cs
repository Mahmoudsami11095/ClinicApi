using System.Collections.Generic;
using System.Threading.Tasks;

namespace Clinic.Application.Interfaces;

public interface IMobilePushDispatcher
{
    Task<int> SendPushToUserAsync(string userId, string title, string body, Dictionary<string, string>? data = null);
    Task<int> SendPushToRoleAsync(string role, string title, string body, string? clinicId = null, Dictionary<string, string>? data = null);
    Task<int> BroadcastPushAsync(string title, string body, Dictionary<string, string>? data = null);
}
