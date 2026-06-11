using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repositories;

public class ClinicRepository : GenericRepository<ClinicEntity>, IClinicRepository
{
    public ClinicRepository(ClinicDbContext context) : base(context) { }
}

public class PatientRepository : GenericRepository<Patient>, IPatientRepository
{
    public PatientRepository(ClinicDbContext context) : base(context) { }
}

public class DoctorRepository : GenericRepository<Doctor>, IDoctorRepository
{
    public DoctorRepository(ClinicDbContext context) : base(context) { }

    public override async Task<List<Doctor>> GetAllAsync()
        => await _dbSet.Include(d => d.DoctorClinics).AsNoTracking().ToListAsync();

    public async Task<Doctor> AddWithClinicsAsync(Doctor doctor, List<string> clinicIds)
    {
        foreach (var clinicId in clinicIds)
        {
            doctor.DoctorClinics.Add(new DoctorClinic
            {
                DoctorId = doctor.Id,
                ClinicId = clinicId
            });
        }
        await _dbSet.AddAsync(doctor);
        await _context.SaveChangesAsync();
        return doctor;
    }
}

public class AppointmentRepository : GenericRepository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(ClinicDbContext context) : base(context) { }
}

public class BillingRepository : GenericRepository<BillingRecord>, IBillingRepository
{
    public BillingRepository(ClinicDbContext context) : base(context) { }

    public override async Task<List<BillingRecord>> GetAllAsync()
        => await _dbSet.Include(b => b.Payments).AsNoTracking().ToListAsync();
}

public class PrescriptionRepository : GenericRepository<Prescription>, IPrescriptionRepository
{
    public PrescriptionRepository(ClinicDbContext context) : base(context) { }

    public override async Task<List<Prescription>> GetAllAsync()
        => await _dbSet.Include(p => p.Medications).AsNoTracking().ToListAsync();
}

public class DentalLogRepository : GenericRepository<DentalLog>, IDentalLogRepository
{
    public DentalLogRepository(ClinicDbContext context) : base(context) { }
}

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ClinicDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
        => await _dbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
}
