namespace Clinic.Domain.Entities;

public class PromoCode
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DiscountType { get; set; } = "Percent"; // "Percent" | "Flat" | "FreeMonths"
    public double Value { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int MaxUses { get; set; } = 100;
    public int CurrentUses { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
