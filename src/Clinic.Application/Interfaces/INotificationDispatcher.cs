using System.Threading.Tasks;
using Clinic.Domain.Entities;

namespace Clinic.Application.Interfaces;

public interface INotificationDispatcher
{
    Task SendNotificationAsync(string userId, Notification notification);
}
