namespace Clinic.Domain.Entities;

public class Specialization
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string TranslationKey { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    // Navigation property
    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
