namespace Clinic.Application.DTOs;

public class RadiologyCenterDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? Address { get; set; }
}

public class CreateRadiologyCenterDto
{
    public string Name { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? Address { get; set; }
}
