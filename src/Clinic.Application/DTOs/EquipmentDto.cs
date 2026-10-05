namespace Clinic.Application.DTOs;

public class EquipmentDto
{
    public string? Id { get; set; }
    public string ClinicId { get; set; } = string.Empty;
    public string? DoctorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string? SerialNumber { get; set; }
    public string? ModelNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? RoomOrChair { get; set; }
    public string Status { get; set; } = "Operational";
    public decimal? PurchaseCost { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public string? MaintenanceNotes { get; set; }
    public string? ServiceProvider { get; set; }
    public string? ServiceContactPhone { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsMaintenanceDue { get; set; }
    public bool IsWarrantyExpired { get; set; }
}

public class EquipmentMaintenanceLogRequest
{
    public DateTime ServiceDate { get; set; } = DateTime.UtcNow;
    public DateTime? NextDueDate { get; set; }
    public string? Notes { get; set; }
    public string? Status { get; set; } = "Operational";
    public string? ServiceProvider { get; set; }
    public string? ServiceContactPhone { get; set; }
}
