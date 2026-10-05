namespace Clinic.Domain.Entities;

public class CommissionPayoutItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CommissionPayoutId { get; set; } = string.Empty;
    public string? BillingRecordId { get; set; }
    public string? AppointmentId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string ServiceCategory { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public decimal LabFee { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public DateTime ServiceDate { get; set; } = DateTime.UtcNow;
}
