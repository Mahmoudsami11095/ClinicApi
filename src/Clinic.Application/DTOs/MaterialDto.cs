namespace Clinic.Application.DTOs;

public class MaterialDto
{
    public string Id { get; set; } = string.Empty;
    public string ClinicId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Unit { get; set; }
}

public class ConsumedMaterialDto
{
    public string MaterialId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
