namespace Clinic.Domain.Entities;

public class Material
{
    public string Id { get; set; } = string.Empty;
    public string ClinicId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsDefault { get; set; }
    public int Quantity { get; set; }
    public string? Unit { get; set; } // e.g. "Boxes", "Pieces", "ml"
    public int MinStockAlert { get; set; } = 5;
    public DateTime? ExpirationDate { get; set; }
    public string? BatchNumber { get; set; }

    // REQ-INV-02: Supplier & Purchase Order Workflow
    public string? SupplierName { get; set; }
    public decimal? UnitCost { get; set; }
    public string? LastRestockedAt { get; set; }
    public string? PurchaseOrderRef { get; set; }
    public string? ImageUrl { get; set; }

    public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value.Date < DateTime.UtcNow.Date;

    // Navigation properties
    public Doctor Doctor { get; set; } = null!;
    public ClinicEntity Clinic { get; set; } = null!;
}
