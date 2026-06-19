using Clinic.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Clinic.Application.Interfaces;

public interface IPatientService
{
    Task<IEnumerable<PatientDto>> GetAllAsync(string? doctorIdClaim);
    Task CreateAsync(PatientDto dto, string? doctorIdClaim);
    Task UpdateAsync(string id, PatientDto dto, string? doctorIdClaim);
    Task DeleteAsync(string id, string? doctorIdClaim);
}
