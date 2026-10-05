namespace Clinic.Application.DTOs;

public class SearchResultDto
{
    public List<SearchPatientItemDto> Patients { get; set; } = new();
    public List<SearchDoctorItemDto> Doctors { get; set; } = new();
    public List<SearchChairItemDto> Chairs { get; set; } = new();
    public List<SearchMaterialItemDto> Materials { get; set; } = new();
}

public class SearchPatientItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public string? ClinicId { get; set; }
}

public class SearchDoctorItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public string? Email { get; set; }
}

public class SearchChairItemDto
{
    public string Id { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string ChairName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class SearchMaterialItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Unit { get; set; }
}
