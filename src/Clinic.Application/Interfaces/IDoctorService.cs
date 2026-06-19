using Clinic.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Clinic.Application.Interfaces;

public interface IDoctorService
{
    Task<IEnumerable<DoctorDto>> GetAllAsync();
    Task CreateAsync(DoctorDto dto);
}
