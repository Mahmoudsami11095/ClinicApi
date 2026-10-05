namespace Clinic.Domain.Entities;

public class Equipment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ClinicId { get; set; } = string.Empty;
    public string? DoctorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "General"; // "Handpieces", "Sterilization", "Operatory", "Diagnostic", "Surgical", "Endodontic"
    public string? SerialNumber { get; set; }
    public string? ModelNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? RoomOrChair { get; set; } // e.g. "Chair 1", "Operatory 2", "Sterilization Bay"
    public string Status { get; set; } = "Operational"; // "Operational", "Maintenance Due", "In Repair", "Decommissioned"
    public decimal? PurchaseCost { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? MaintenanceNotes { get; set; }
    public string? ServiceProvider { get; set; }
    public string? ServiceContactPhone { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsMaintenanceDue => NextMaintenanceDate.HasValue && NextMaintenanceDate.Value.Date <= DateTime.UtcNow.Date;
    public bool IsWarrantyExpired => WarrantyExpiryDate.HasValue && WarrantyExpiryDate.Value.Date < DateTime.UtcNow.Date;

    // Navigation properties
    public ClinicEntity Clinic { get; set; } = null!;
}
