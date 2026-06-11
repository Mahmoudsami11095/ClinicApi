using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Clinic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BCrypt.Net;
using System.Text.Json;

namespace Clinic.Infrastructure.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();

        await context.Database.MigrateAsync();

        if (await context.Clinics.AnyAsync()) return; // Already seeded

        // ── Clinics ──
        var clinics = new List<ClinicEntity>
        {
            new() { Id = "clinic-1", Name = "City Health Clinic", Address = "123 Main St, Downtown", Phone = "+1 (555) 019-2834" },
            new() { Id = "clinic-2", Name = "Metro Dental & Cardiac", Address = "456 Oak Ave, Medical District", Phone = "+1 (555) 014-9988" },
            new() { Id = "clinic-3", Name = "Westside Family Care", Address = "789 Pine Rd, West End", Phone = "+1 (555) 012-3456" }
        };
        await context.Clinics.AddRangeAsync(clinics);

        // ── Doctors ──
        var doctors = new List<Doctor>
        {
            new() { Id = "101", FirstName = "Sarah", LastName = "Jenkins", Specialization = "Cardiology",
                     Email = "dr.jenkins@clinic.com", ContactNumber = "+1122334455",
                     AvailabilityDays = JsonSerializer.Serialize(new[] { "Monday", "Wednesday", "Friday" }),
                     AvailabilityHours = "09:00-17:00" },
            new() { Id = "102", FirstName = "Michael", LastName = "Chen", Specialization = "Pediatrics",
                     Email = "dr.chen@clinic.com", ContactNumber = "+5544332211",
                     AvailabilityDays = JsonSerializer.Serialize(new[] { "Tuesday", "Thursday" }),
                     AvailabilityHours = "10:00-18:00" },
            new() { Id = "103", FirstName = "Emily", LastName = "Torres", Specialization = "Dermatology",
                     Email = "dr.torres@clinic.com", ContactNumber = "+7788990011",
                     AvailabilityDays = JsonSerializer.Serialize(new[] { "Monday", "Tuesday", "Thursday" }),
                     AvailabilityHours = "08:00-16:00" },
            new() { Id = "104", FirstName = "James", LastName = "Patel", Specialization = "Neurology",
                     Email = "dr.patel@clinic.com", ContactNumber = "+3344556677",
                     AvailabilityDays = JsonSerializer.Serialize(new[] { "Wednesday", "Friday" }),
                     AvailabilityHours = "09:00-15:00" },
            new() { Id = "105", FirstName = "Zidan", LastName = "Kareem", Specialization = "Dentistry",
                     Email = "dr.zidan@clinic.com", ContactNumber = "+96655443322",
                     AvailabilityDays = JsonSerializer.Serialize(new[] { "Sunday", "Tuesday", "Thursday" }),
                     AvailabilityHours = "09:00-17:00" },
            new() { Id = "106", FirstName = "Marcus", LastName = "Vance", Specialization = "Dentistry",
                     Email = "dr.vance@clinic.com", ContactNumber = "+1234567890",
                     AvailabilityDays = JsonSerializer.Serialize(new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" }),
                     AvailabilityHours = "09:00-17:00" }
        };
        await context.Doctors.AddRangeAsync(doctors);

        // ── DoctorClinic (many-to-many) ──
        var doctorClinics = new List<DoctorClinic>
        {
            new() { DoctorId = "101", ClinicId = "clinic-1" }, new() { DoctorId = "101", ClinicId = "clinic-2" },
            new() { DoctorId = "102", ClinicId = "clinic-2" }, new() { DoctorId = "102", ClinicId = "clinic-3" },
            new() { DoctorId = "103", ClinicId = "clinic-1" },
            new() { DoctorId = "104", ClinicId = "clinic-1" }, new() { DoctorId = "104", ClinicId = "clinic-3" },
            new() { DoctorId = "105", ClinicId = "clinic-2" }, new() { DoctorId = "105", ClinicId = "clinic-3" },
            new() { DoctorId = "106", ClinicId = "clinic-1" }, new() { DoctorId = "106", ClinicId = "clinic-2" }, new() { DoctorId = "106", ClinicId = "clinic-3" }
        };
        await context.DoctorClinics.AddRangeAsync(doctorClinics);

        // ── Patients ──
        var patients = new List<Patient>
        {
            new() { Id = "1", FirstName = "John", LastName = "Doe", Gender = "male", DateOfBirth = "1980-05-15",
                     ContactNumber = "+1234567890", Email = "john.doe@example.com", BloodGroup = "O+",
                     Address = "123 Main St, Cityville", RegistrationDate = "2023-01-10T10:00:00Z", ClinicId = "clinic-1" },
            new() { Id = "2", FirstName = "Jane", LastName = "Smith", Gender = "female", DateOfBirth = "1992-08-22",
                     ContactNumber = "+0987654321", Email = "jane.smith@example.com", BloodGroup = "A-",
                     Address = "456 Oak Rd, Townsville", RegistrationDate = "2023-02-14T11:30:00Z", ClinicId = "clinic-2" },
            new() { Id = "3", FirstName = "Ahmed", LastName = "Hassan", Gender = "male", DateOfBirth = "1975-11-03",
                     ContactNumber = "+2012345678", Email = "ahmed.hassan@example.com", BloodGroup = "B+",
                     Address = "789 Nile Ave, Cairo", RegistrationDate = "2024-06-20T09:15:00Z", ClinicId = "clinic-3" },
            new() { Id = "4", FirstName = "Maria", LastName = "Garcia", Gender = "female", DateOfBirth = "1988-03-17",
                     ContactNumber = "+3498765432", Email = "maria.garcia@example.com", BloodGroup = "AB+",
                     Address = "321 Sol Blvd, Madrid", RegistrationDate = "2024-09-05T14:00:00Z", ClinicId = "clinic-1" },
            new() { Id = "5", FirstName = "Oliver", LastName = "Brown", Gender = "male", DateOfBirth = "1995-07-29",
                     ContactNumber = "+4412345678", Email = "oliver.brown@example.com", BloodGroup = "O-",
                     Address = "55 King St, London", RegistrationDate = "2025-01-12T08:45:00Z", ClinicId = "clinic-2" },
            new() { Id = "6", FirstName = "Fatima", LastName = "Ali", Gender = "female", DateOfBirth = "2000-12-01",
                     ContactNumber = "+9661234567", Email = "fatima.ali@example.com", BloodGroup = "A+",
                     Address = "101 Palm Dr, Riyadh", RegistrationDate = "2025-03-22T16:30:00Z", ClinicId = "clinic-3" },
            new() { Id = "7", FirstName = "Liam", LastName = "Wilson", Gender = "male", DateOfBirth = "1970-09-08",
                     ContactNumber = "+6112345678", Email = "liam.wilson@example.com", BloodGroup = "B-",
                     Address = "88 Harbour Rd, Sydney", RegistrationDate = "2026-01-05T07:20:00Z", ClinicId = "clinic-1" },
            new() { Id = "8", FirstName = "Sofia", LastName = "Rossi", Gender = "female", DateOfBirth = "1985-04-25",
                     ContactNumber = "+3912345678", Email = "sofia.rossi@example.com", BloodGroup = "AB-",
                     Address = "22 Via Roma, Milan", RegistrationDate = "2026-02-18T12:00:00Z", ClinicId = "clinic-2" }
        };
        await context.Patients.AddRangeAsync(patients);

        // ── Appointments ──
        var appointments = new List<Appointment>
        {
            new() { Id = "1", PatientId = "1", DoctorId = "101", Date = "2026-04-15T09:00:00Z", Status = "completed", Type = "General Checkup", Notes = "Patient complains of mild headache.", ClinicId = "clinic-1" },
            new() { Id = "2", PatientId = "2", DoctorId = "102", Date = "2026-04-15T10:30:00Z", Status = "completed", Type = "Follow-up", Notes = "Reviewing recent blood test results.", ClinicId = "clinic-2" },
            new() { Id = "3", PatientId = "3", DoctorId = "101", Date = "2026-04-15T14:00:00Z", Status = "scheduled", Type = "Cardiology Consultation", Notes = "Annual heart screening.", ClinicId = "clinic-3" },
            new() { Id = "4", PatientId = "5", DoctorId = "103", Date = "2026-04-15T15:30:00Z", Status = "scheduled", Type = "Dermatology", Notes = "Skin rash evaluation.", ClinicId = "clinic-2" },
            new() { Id = "5", PatientId = "4", DoctorId = "102", Date = "2026-04-16T09:00:00Z", Status = "scheduled", Type = "Pediatric Checkup", Notes = "Routine child wellness visit.", ClinicId = "clinic-1" },
            new() { Id = "6", PatientId = "6", DoctorId = "104", Date = "2026-04-16T11:00:00Z", Status = "scheduled", Type = "Neurology", Notes = "Persistent migraine follow-up.", ClinicId = "clinic-3" },
            new() { Id = "7", PatientId = "7", DoctorId = "101", Date = "2026-04-14T10:00:00Z", Status = "completed", Type = "General Checkup", Notes = "Blood pressure monitoring.", ClinicId = "clinic-1" },
            new() { Id = "8", PatientId = "8", DoctorId = "103", Date = "2026-04-14T14:30:00Z", Status = "cancelled", Type = "Orthopedic Consultation", Notes = "Patient rescheduled.", ClinicId = "clinic-2" },
            new() { Id = "9", PatientId = "1", DoctorId = "105", Date = "2026-05-23T10:00:00Z", Status = "scheduled", Type = "Dental Scaling", Notes = "Routine scaling and cleaning.", ClinicId = "clinic-1" },
            new() { Id = "10", PatientId = "2", DoctorId = "105", Date = "2026-05-22T09:00:00Z", Status = "completed", Type = "Root Canal", Notes = "Initial root canal session. Needs follow-up.", ClinicId = "clinic-2" }
        };
        await context.Appointments.AddRangeAsync(appointments);

        // ── Billing Records ──
        var billingRecords = new List<BillingRecord>
        {
            new() { Id = "1", PatientId = "1", AppointmentId = "1", Amount = 150.00m, Status = "paid", DateIssued = "2026-04-15T10:00:00Z", PaymentMethod = "Credit Card", ClinicId = "clinic-1" },
            new() { Id = "2", PatientId = "2", AppointmentId = "2", Amount = 200.00m, Status = "paid", DateIssued = "2026-04-15T11:30:00Z", PaymentMethod = "Cash", ClinicId = "clinic-2" },
            new() { Id = "3", PatientId = "3", AppointmentId = "3", Amount = 350.00m, Status = "pending", DateIssued = "2026-04-15T14:00:00Z", ClinicId = "clinic-3" },
            new() { Id = "4", PatientId = "7", AppointmentId = "7", Amount = 100.00m, Status = "paid", DateIssued = "2026-04-14T10:30:00Z", PaymentMethod = "Insurance", ClinicId = "clinic-1" },
            new() { Id = "5", PatientId = "5", AppointmentId = "4", Amount = 275.00m, Status = "pending", DateIssued = "2026-04-15T16:00:00Z", ClinicId = "clinic-2" },
            new() { Id = "6", PatientId = "8", AppointmentId = "8", Amount = 180.00m, Status = "overdue", DateIssued = "2026-04-10T09:00:00Z", ClinicId = "clinic-2" },
            new() { Id = "7", PatientId = "2", AppointmentId = "10", Amount = 450.00m, PaidAmount = 200.00m, Status = "partially_paid", DateIssued = "2026-05-22T10:00:00Z", PaymentMethod = "Cash", ClinicId = "clinic-2" },
            new() { Id = "8", PatientId = "1", AppointmentId = "9", Amount = 120.00m, PaidAmount = 0.00m, Status = "pending", DateIssued = "2026-05-23T11:00:00Z", ClinicId = "clinic-1" }
        };
        await context.BillingRecords.AddRangeAsync(billingRecords);

        // ── Users ──
        var defaultPassword = BCrypt.Net.BCrypt.HashPassword("password123");
        var users = new List<User>
        {
            new() { Id = "super-admin", Name = "Super Admin User", Role = UserRole.Admin, Email = "superadmin@medclinic.com", Title = "System Director", PasswordHash = defaultPassword },
            new() { Id = "admin-1", Name = "City Clinic Admin", Role = UserRole.Admin, ClinicId = "clinic-1", Email = "admin.city@clinic.com", Title = "Clinic Director", PasswordHash = defaultPassword },
            new() { Id = "admin-2", Name = "Metro Clinic Admin", Role = UserRole.Admin, ClinicId = "clinic-2", Email = "admin.metro@clinic.com", Title = "Clinic Director", PasswordHash = defaultPassword },
            new() { Id = "doc-101", Name = "Dr. Sarah Jenkins", Role = UserRole.Doctor, DoctorId = "101", Email = "dr.jenkins@clinic.com", Title = "Chief Cardiologist", PasswordHash = defaultPassword },
            new() { Id = "doc-102", Name = "Dr. Michael Chen", Role = UserRole.Doctor, DoctorId = "102", Email = "dr.chen@clinic.com", Title = "Pediatric Specialist", PasswordHash = defaultPassword },
            new() { Id = "doc-105", Name = "Dr. Zidan Kareem", Role = UserRole.Doctor, DoctorId = "105", Email = "dr.zidan@clinic.com", Title = "Senior Dentist", PasswordHash = defaultPassword },
            new() { Id = "doc-106", Name = "Dr. Marcus Vance", Role = UserRole.Doctor, DoctorId = "106", Email = "dr.vance@clinic.com", Title = "Dentist Practitioner", PasswordHash = defaultPassword },
            new() { Id = "asst-101", Name = "City Clinic Assistant", Role = UserRole.Assistant, ClinicId = "clinic-1", DoctorId = "101", Email = "asst.city@clinic.com", Title = "Clinical Assistant", PasswordHash = defaultPassword },
            new() { Id = "asst-102", Name = "Metro Clinic Assistant", Role = UserRole.Assistant, ClinicId = "clinic-2", DoctorId = "101", Email = "asst.metro@clinic.com", Title = "Clinical Assistant", PasswordHash = defaultPassword },
            new() { Id = "pat-1", Name = "John Doe", Role = UserRole.Patient, ClinicId = "clinic-1", PatientId = "1", Email = "john.doe@example.com", Title = "Registered Patient", PasswordHash = defaultPassword },
            new() { Id = "pat-2", Name = "Jane Smith", Role = UserRole.Patient, ClinicId = "clinic-2", PatientId = "2", Email = "jane.smith@example.com", Title = "Registered Patient", PasswordHash = defaultPassword }
        };
        await context.Users.AddRangeAsync(users);

        await context.SaveChangesAsync();
    }
}
