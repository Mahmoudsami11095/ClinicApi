namespace Clinic.Contracts.Common;

public interface IHasTenant
{
    string TenantId { get; set; }
}
