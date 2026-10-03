namespace Clinic.Application.DTOs;

public class MaterialDto
{
    public string Id { get; set; } = string.Empty;
    public string ClinicId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Unit { get; set; }
    public int MinStockAlert { get; set; } = 5;
    public DateTime? ExpirationDate { get; set; }
    public string? BatchNumber { get; set; }
    public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value.Date < DateTime.UtcNow.Date;

    // REQ-INV-02: Supplier & Purchase Order Workflow
    public string? SupplierName { get; set; }
    public decimal? UnitCost { get; set; }
    public string? LastRestockedAt { get; set; }
    public string? PurchaseOrderRef { get; set; }
}

public class ConsumedMaterialDto
{
    public string MaterialId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
