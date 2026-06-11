namespace Clinic.Domain.Entities;

public class BillingRecord
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string? AppointmentId { get; set; }
    public decimal Amount { get; set; }
    public decimal? PaidAmount { get; set; }
    public string Status { get; set; } = string.Empty; // "paid", "pending", "overdue", "partially_paid"
    public string DateIssued { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? Description { get; set; }
    public string? ClinicId { get; set; }

    // Owned collection
    public List<PaymentLog> Payments { get; set; } = new();

    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public Appointment? Appointment { get; set; }
    public ClinicEntity? Clinic { get; set; }
}
