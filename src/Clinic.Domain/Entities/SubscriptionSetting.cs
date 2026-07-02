namespace Clinic.Domain.Entities;

public class SubscriptionSetting
{
    public string Id { get; set; } = string.Empty;
    public decimal InitialSetupFee { get; set; } = 100.00m;
    public decimal AnnualSubscriptionFee { get; set; } = 300.00m;
    public int TrialDurationMonths { get; set; } = 6;
}
