namespace Clinic.Domain.Entities;

public class BillingRecord
{
    public string Id { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string? AppointmentId { get; set; }

    // BR-FIN-03: Sequential gapless invoice numbering per clinic (e.g. INV-2026-00001)
    public string InvoiceNumber { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountReason { get; set; }
    public string? DiscountAuthorizedBy { get; set; }
    public decimal Amount { get; set; }
    public decimal? PaidAmount { get; set; }
    public string Status { get; set; } = string.Empty; // "paid", "pending", "overdue", "partially_paid", "voided"
    public string DateIssued { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? Description { get; set; }
    public string? ClinicId { get; set; }

    // BR-FIN-03: Void-only (no permanent deletion), mandatory void reason
    public string? VoidReason { get; set; }
    public string? VoidedAt { get; set; }

    // Owned collection
    public List<PaymentLog> Payments { get; set; } = new();

    // Navigation properties
    public Patient Patient { get; set; } = null!;
    public Appointment? Appointment { get; set; }
    public ClinicEntity? Clinic { get; set; }
}

