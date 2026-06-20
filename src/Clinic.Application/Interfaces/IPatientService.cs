using Clinic.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Clinic.Application.Interfaces;

public interface IPatientService
{
    Task<IEnumerable<PatientDto>> GetAllAsync(string? doctorIdClaim, string? clinicIdClaim = null);
    Task CreateAsync(PatientDto dto, string? doctorIdClaim, string? clinicIdClaim = null);
    Task UpdateAsync(string id, PatientDto dto, string? doctorIdClaim, string? clinicIdClaim = null);
    Task DeleteAsync(string id, string? doctorIdClaim, string? clinicIdClaim = null);
}
