using System.Threading.Tasks;

namespace Clinic.Application.Interfaces;

public interface IWhatsAppNotificationService
{
    Task<bool> SendAppointmentConfirmationAsync(string phoneNumber, string patientName, string clinicName, string appointmentType, string date, string time);
    Task<bool> SendAppointmentCancellationAsync(string phoneNumber, string patientName, string clinicName, string date, string time);
}
