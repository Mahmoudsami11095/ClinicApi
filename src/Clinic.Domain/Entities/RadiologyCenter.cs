namespace Clinic.Domain.Entities;

public class RadiologyCenter
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? Address { get; set; }
    
    // Navigation
    public ICollection<RadiologyRecord> RadiologyRecords { get; set; } = new List<RadiologyRecord>();
}
