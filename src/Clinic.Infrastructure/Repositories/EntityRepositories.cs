using Clinic.Application.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Repositories;

public class ClinicRepository : GenericRepository<ClinicEntity>, IClinicRepository
{
    public ClinicRepository(ClinicDbContext context) : base(context) { }

    public override async Task<List<ClinicEntity>> GetAllAsync()
        => await _dbSet
            .Include(c => c.DoctorClinics)
            .Include(c => c.UserClinics)
                .ThenInclude(uc => uc.User)
            .AsNoTracking()
            .ToListAsync();
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

    public override async Task<List<User>> GetAllAsync()
        => await _dbSet.Include(u => u.UserClinics).AsNoTracking().ToListAsync();

    public override async Task<User?> GetByIdAsync(string id)
        => await _dbSet.Include(u => u.UserClinics).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByEmailAsync(string email)
        => await _dbSet.Include(u => u.UserClinics).FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    public async Task<User?> GetByEmailIncludeDeletedAsync(string email)
        => await _dbSet.IgnoreQueryFilters().Include(u => u.UserClinics).FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber)
    {
        var split = Clinic.Domain.Helpers.PhoneHelper.SplitContactNumber(phoneNumber);
        var normPhone = Clinic.Domain.Helpers.PhoneHelper.NormalizePhoneNumber(split.CountryCode, split.PhoneNumber);

        return await _dbSet
            .Include(u => u.Patient)
            .Include(u => u.Doctor)
            .FirstOrDefaultAsync(u =>
                (u.Patient != null && u.Patient.CountryCode == split.CountryCode && u.Patient.PhoneNumber == normPhone) ||
                (u.Doctor != null && u.Doctor.CountryCode == split.CountryCode && u.Doctor.PhoneNumber == normPhone)
            );
    }

    public async Task<bool> IsPhoneNumberUniqueAsync(string countryCode, string phoneNumber, string? excludeUserId = null)
    {
        var normPhone = Clinic.Domain.Helpers.PhoneHelper.NormalizePhoneNumber(countryCode, phoneNumber);

        var patientExists = await _context.Patients
            .AnyAsync(p => p.CountryCode == countryCode && p.PhoneNumber == normPhone && 
                (excludeUserId == null || !_dbSet.Any(u => u.Id == excludeUserId && u.PatientId == p.Id)));

        if (patientExists) return false;

        var doctorExists = await _context.Doctors
            .AnyAsync(d => d.CountryCode == countryCode && d.PhoneNumber == normPhone && 
                (excludeUserId == null || !_dbSet.Any(u => u.Id == excludeUserId && u.DoctorId == d.Id)));

        if (doctorExists) return false;

        return true;
    }

    public async Task DeleteUserWithRelatedDataAsync(string userId, string contentRootPath, string webRootPath)
    {
        var user = await _dbSet.FindAsync(userId);
        if (user == null) return;

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
            // 1. Notifications cleanup
            var notifications = await _context.Notifications.Where(n => n.UserId == userId).ToListAsync();
            _context.Notifications.RemoveRange(notifications);

            // 2. Doctor cleanups
            if (!string.IsNullOrEmpty(user.DoctorId))
            {
                var doctorId = user.DoctorId;

                // Get all appointment IDs for this doctor (needed for dependent cleanup)
                var doctorAppointmentIds = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId)
                    .Select(a => a.Id)
                    .ToListAsync();

                // Delete Prescriptions FIRST (FK to Appointment with NoAction, FK to Patient/Doctor with NoAction)
                var doctorPrescriptions = await _context.Prescriptions
                    .Where(p => p.DoctorId == doctorId)
                    .ToListAsync();
                _context.Prescriptions.RemoveRange(doctorPrescriptions);

                // Delete BillingRecords linked to doctor's appointments (FK to Patient with NoAction)
                var doctorBillingRecords = await _context.BillingRecords
                    .Where(b => doctorAppointmentIds.Contains(b.AppointmentId!))
                    .ToListAsync();
                _context.BillingRecords.RemoveRange(doctorBillingRecords);

                // Now safe to delete Appointments
                var doctorAppointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId)
                    .ToListAsync();
                _context.Appointments.RemoveRange(doctorAppointments);

                var doctorDentalLogs = await _context.DentalLogs.Where(d => d.DoctorId == doctorId).ToListAsync();
                _context.DentalLogs.RemoveRange(doctorDentalLogs);

                var doctorRadiologyRecords = await _context.RadiologyRecords.Where(r => r.DoctorId == doctorId).ToListAsync();
                _context.RadiologyRecords.RemoveRange(doctorRadiologyRecords);

                var doctorReceipts = await _context.SubscriptionReceipts.Where(r => r.DoctorId == doctorId).ToListAsync();
                _context.SubscriptionReceipts.RemoveRange(doctorReceipts);

                var doctorClinics = await _context.DoctorClinics.Where(dc => dc.DoctorId == doctorId).ToListAsync();
                _context.DoctorClinics.RemoveRange(doctorClinics);

                var doctorMaterials = await _context.Materials.Where(m => m.DoctorId == doctorId).ToListAsync();
                _context.Materials.RemoveRange(doctorMaterials);

                var clinicsCreatedByDoctor = await _context.Clinics.Where(c => c.CreatorDoctorId == doctorId).ToListAsync();
                foreach (var clinic in clinicsCreatedByDoctor)
                {
                    clinic.CreatorDoctorId = null;
                }

                var doctor = await _context.Doctors.FindAsync(doctorId);
                if (doctor != null)
                {
                    _context.Doctors.Remove(doctor);
                }

                try
                {
                    var receiptsFolder = Path.Combine(webRootPath, "receipts");
                    if (Directory.Exists(receiptsFolder))
                    {
                        var files = Directory.GetFiles(receiptsFolder, $"{doctorId}_*");
                        foreach (var file in files)
                        {
                            System.IO.File.Delete(file);
                        }
                    }
                }
                catch
                {
                    // Suppress IO errors so DB transaction isn't broken
                }
            }

            // 3. Patient cleanups
            if (!string.IsNullOrEmpty(user.PatientId))
            {
                var patientId = user.PatientId;

                // Get all appointment IDs for this patient (needed for dependent cleanup)
                var patientAppointmentIds = await _context.Appointments
                    .Where(a => a.PatientId == patientId)
                    .Select(a => a.Id)
                    .ToListAsync();

                // Delete Prescriptions FIRST (FK to Appointment with NoAction)
                var patientPrescriptions = await _context.Prescriptions
                    .Where(p => p.PatientId == patientId)
                    .ToListAsync();
                _context.Prescriptions.RemoveRange(patientPrescriptions);

                // Delete BillingRecords BEFORE Patient (FK to Patient with NoAction)
                var patientBilling = await _context.BillingRecords
                    .Where(b => b.PatientId == patientId)
                    .ToListAsync();
                _context.BillingRecords.RemoveRange(patientBilling);

                // Now safe to delete Appointments
                var patientAppointments = await _context.Appointments
                    .Where(a => a.PatientId == patientId)
                    .ToListAsync();
                _context.Appointments.RemoveRange(patientAppointments);

                var patientDentalLogs = await _context.DentalLogs.Where(d => d.PatientId == patientId).ToListAsync();
                _context.DentalLogs.RemoveRange(patientDentalLogs);

                var patientRadiologyRecords = await _context.RadiologyRecords.Where(r => r.PatientId == patientId).ToListAsync();
                _context.RadiologyRecords.RemoveRange(patientRadiologyRecords);

                var patient = await _context.Patients.FindAsync(patientId);
                if (patient != null)
                {
                    _context.Patients.Remove(patient);
                }

                try
                {
                    var uploadsFolder = Path.Combine(contentRootPath, "uploads", patientId);
                    if (Directory.Exists(uploadsFolder))
                    {
                        Directory.Delete(uploadsFolder, recursive: true);
                    }
                }
                catch
                {
                    // Suppress
                }
            }

            // 4. UserClinics join cleanup
            var userClinics = await _context.UserClinics.Where(uc => uc.UserId == userId).ToListAsync();
            _context.UserClinics.RemoveRange(userClinics);

            // 5. Final User delete
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
}

public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(ClinicDbContext context) : base(context) { }

    public override async Task<Notification?> GetByIdAsync(string id)
    {
        if (Guid.TryParse(id, out var guidId))
        {
            return await _dbSet.FindAsync(guidId);
        }
        return null;
    }

    public override async Task DeleteAsync(string id)
    {
        if (Guid.TryParse(id, out var guidId))
        {
            var entity = await _dbSet.FindAsync(guidId);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}

public class RadiologyCenterRepository : GenericRepository<RadiologyCenter>, IRadiologyCenterRepository
{
    public RadiologyCenterRepository(ClinicDbContext context) : base(context) { }
}

public class RadiologyRecordRepository : GenericRepository<RadiologyRecord>, IRadiologyRecordRepository
{
    public RadiologyRecordRepository(ClinicDbContext context) : base(context) { }
}
