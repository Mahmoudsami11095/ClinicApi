using Clinic.Domain.Entities;

namespace Clinic.Application.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(string id);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(string id);
}

public interface IClinicRepository : IGenericRepository<ClinicEntity>
{
    async Task<List<string>> GetAllowedClinicIdsForDoctorAsync(string doctorId)
    {
        var clinics = await GetAllAsync();
        return clinics
            .Where(c => c.CreatorDoctorId == doctorId ||
                        c.DoctorClinics.Any(dc => dc.DoctorId == doctorId && dc.Status == "Accepted"))
            .Select(c => c.Id)
            .ToList();
    }

    async Task<bool> IsDoctorAuthorizedForClinicAsync(string doctorId, string clinicId)
    {
        var clinics = await GetAllAsync();
        return clinics.Any(c => c.Id == clinicId &&
                                (c.CreatorDoctorId == doctorId ||
                                 c.DoctorClinics.Any(dc => dc.DoctorId == doctorId && dc.Status == "Accepted")));
    }
}

public interface IPatientRepository : IGenericRepository<Patient> { }

public interface IDoctorRepository : IGenericRepository<Doctor>
{
    Task<Doctor> AddWithClinicsAsync(Doctor doctor, List<string> clinicIds);
    Task AssignToClinicAsync(string doctorId, string clinicId);
    Task AssignDoctorsToClinicAsync(string clinicId, List<string> doctorIds);
    Task RespondToAssignmentAsync(string doctorId, string clinicId, string status);
}

public interface IAppointmentRepository : IGenericRepository<Appointment> { }

public interface IBillingRepository : IGenericRepository<BillingRecord> { }

public interface IPrescriptionRepository : IGenericRepository<Prescription> { }

public interface IDentalLogRepository : IGenericRepository<DentalLog> { }

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByEmailIncludeDeletedAsync(string email);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    Task<bool> IsPhoneNumberUniqueAsync(string countryCode, string phoneNumber, string? excludeUserId = null);
    Task DeleteUserWithRelatedDataAsync(string userId, string contentRootPath, string webRootPath);
}

public interface INotificationRepository : IGenericRepository<Notification>
{
    Task<List<Notification>> GetByUserIdAsync(string userId, int count);
}

public interface IRadiologyCenterRepository : IGenericRepository<RadiologyCenter> { }

public interface IRadiologyRecordRepository : IGenericRepository<RadiologyRecord> { }
