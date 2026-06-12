using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repositories;

public class ClinicRepository : GenericRepository<ClinicEntity>, IClinicRepository
{
    public ClinicRepository(ClinicDbContext context) : base(context) { }

    public override async Task<List<ClinicEntity>> GetAllAsync()
        => await _dbSet.Include(c => c.DoctorClinics).AsNoTracking().ToListAsync();
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

    public async Task AssignToClinicAsync(string doctorId, string clinicId)
    {
        var assignment = await _context.DoctorClinics.FirstOrDefaultAsync(dc => dc.DoctorId == doctorId && dc.ClinicId == clinicId);
        if (assignment == null)
        {
            await _context.DoctorClinics.AddAsync(new DoctorClinic
            {
                DoctorId = doctorId,
                ClinicId = clinicId,
                Status = "Accepted"
            });
            await _context.SaveChangesAsync();
        }
        else
        {
            assignment.Status = "Accepted";
            await _context.SaveChangesAsync();
        }
    }

    public async Task AssignDoctorsToClinicAsync(string clinicId, List<string> doctorIds)
    {
        // For editing assignments, we remove doctors that are no longer selected
        // unless they are the clinic's creator/owner or accepted.
        // Wait, the owner can manage all assignments. Let's see: we should only keep the doctors in the new list,
        // but if they are the owner we don't delete them.
        var clinic = await _context.Clinics.FirstOrDefaultAsync(c => c.Id == clinicId);
        var creatorId = clinic?.CreatorDoctorId;

        var existingAssignments = await _context.DoctorClinics.Where(dc => dc.ClinicId == clinicId).ToListAsync();
        foreach (var assignment in existingAssignments)
        {
            if (assignment.DoctorId != creatorId && !doctorIds.Contains(assignment.DoctorId))
            {
                _context.DoctorClinics.Remove(assignment);
            }
        }

        foreach (var doctorId in doctorIds)
        {
            var exists = existingAssignments.Any(dc => dc.DoctorId == doctorId);
            if (!exists && doctorId != creatorId)
            {
                await _context.DoctorClinics.AddAsync(new DoctorClinic
                {
                    DoctorId = doctorId,
                    ClinicId = clinicId,
                    Status = "Pending"
                });
            }
        }
        await _context.SaveChangesAsync();
    }

    public async Task RespondToAssignmentAsync(string doctorId, string clinicId, string status)
    {
        var assignment = await _context.DoctorClinics.FirstOrDefaultAsync(dc => dc.DoctorId == doctorId && dc.ClinicId == clinicId);
        if (assignment != null)
        {
            if (string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                _context.DoctorClinics.Remove(assignment);
            }
            else
            {
                assignment.Status = "Accepted";
            }
            await _context.SaveChangesAsync();
        }
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
