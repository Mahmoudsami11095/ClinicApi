namespace Clinic.Domain.Entities;

public class PaymentLog
{
    public decimal Amount { get; set; }
    public string Date { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
}
