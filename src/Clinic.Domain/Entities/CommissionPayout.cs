namespace Clinic.Domain.Entities;

public class CommissionPayout
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DoctorId { get; set; } = string.Empty;
    public string? ClinicId { get; set; }
    
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public decimal TotalGrossRevenue { get; set; }
    public decimal TotalLabFeesDeducted { get; set; }
    public decimal TotalNetCommission { get; set; }
    public decimal ClinicRetainedRevenue { get; set; }

    // "Draft", "Approved", "Paid", "Voided"
    public string Status { get; set; } = "Draft";

    public string? PaymentReference { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Owned collection
    public List<CommissionPayoutItem> Items { get; set; } = new();

    // Navigation properties
    public Doctor? Doctor { get; set; }
    public ClinicEntity? Clinic { get; set; }
}
