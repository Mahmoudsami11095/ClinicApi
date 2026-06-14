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

public interface IClinicRepository : IGenericRepository<ClinicEntity> { }

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
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);
}
