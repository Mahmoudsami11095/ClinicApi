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

    Task<ClinicEntity?> GetBySlugAsync(string slug);
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

public interface IBillingRepository : IGenericRepository<BillingRecord>
{
    // BR-FIN-03: Generate sequential gapless invoice number per clinic
    Task<string> GetNextInvoiceNumberAsync(string? clinicId);
}

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

public interface IClinicalNoteRepository : IGenericRepository<ClinicalNote>
{
    Task<List<ClinicalNote>> GetByPatientIdAsync(string patientId);
}

public interface IDiagnosticRequisitionRepository : IGenericRepository<DiagnosticRequisitionOrder>
{
    Task<DiagnosticRequisitionOrder?> GetByTokenAsync(string token);
    Task<List<DiagnosticRequisitionOrder>> GetByClinicIdAsync(string clinicId);
    Task<List<DiagnosticRequisitionOrder>> GetByPatientIdAsync(string patientId);
    Task<List<DiagnosticRequisitionOrder>> GetByDoctorIdAsync(string doctorId);
}

public interface IStockTransferRequisitionRepository : IGenericRepository<StockTransferRequisition>
{
    Task<StockTransferRequisition?> GetByRequisitionNumberAsync(string requisitionNumber);
    Task<List<StockTransferRequisition>> GetByClinicAsync(string clinicId, string? status = null, string? direction = "all");
    Task<string> GetNextRequisitionNumberAsync();
}

public interface IInsuranceClaimRepository : IGenericRepository<InsuranceClaim>
{
    Task<InsuranceClaim?> GetByClaimNumberAsync(string claimNumber);
    Task<List<InsuranceClaim>> GetByClinicAsync(string clinicId, string? status = null);
    Task<List<InsuranceClaim>> GetByPatientAsync(string patientId);
    Task<string> GetNextClaimNumberAsync();
}

public interface IInformedConsentRepository : IGenericRepository<InformedConsentDocument>
{
    Task<InformedConsentDocument?> GetByDocumentNumberAsync(string documentNumber);
    Task<List<InformedConsentDocument>> GetByPatientIdAsync(string patientId);
    Task<List<InformedConsentDocument>> GetByClinicIdAsync(string clinicId, string? status = null);
    Task<string> GetNextDocumentNumberAsync();
}

public interface IPatientRecallRepository : IGenericRepository<PatientRecall>
{
    Task<PatientRecall?> GetByRecallNumberAsync(string recallNumber);
    Task<List<PatientRecall>> GetByClinicAsync(string clinicId, string? status = null);
    Task<List<PatientRecall>> GetDueRecallsAsync(string clinicId, DateTime asOfDate);
    Task<string> GetNextRecallNumberAsync();
}

