using Clinic.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Clinic.Infrastructure.Data;

public class ClinicDbContext : DbContext
{
    public ClinicDbContext(DbContextOptions<ClinicDbContext> options) : base(options) { }

    public DbSet<ClinicEntity> Clinics => Set<ClinicEntity>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<BillingRecord> BillingRecords => Set<BillingRecord>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<DentalLog> DentalLogs => Set<DentalLog>();
    public DbSet<DoctorClinic> DoctorClinics => Set<DoctorClinic>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Clinic ──
        modelBuilder.Entity<ClinicEntity>(entity =>
        {
            entity.ToTable("Clinics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(50);
        });

        // ── User ──
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(e => e.Clinic)
                  .WithMany(c => c.Users)
                  .HasForeignKey(e => e.ClinicId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Doctor)
                  .WithMany()
                  .HasForeignKey(e => e.DoctorId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Patient)
                  .WithMany()
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Doctor ──
        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.ToTable("Doctors");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Specialization).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.ContactNumber).HasMaxLength(50);
            entity.Property(e => e.Avatar).HasMaxLength(500);
            entity.Property(e => e.AvailabilityDays).HasMaxLength(500);
            entity.Property(e => e.AvailabilityHours).HasMaxLength(50);
        });

        // ── DoctorClinic (Many-to-Many join) ──
        modelBuilder.Entity<DoctorClinic>(entity =>
        {
            entity.ToTable("DoctorClinics");
            entity.HasKey(dc => new { dc.DoctorId, dc.ClinicId });

            entity.Property(dc => dc.Status)
                  .HasMaxLength(50)
                  .HasDefaultValue("Accepted");

            entity.Property(dc => dc.AvailabilityHours).HasMaxLength(50);
            entity.Property(dc => dc.AvailabilityDays).HasMaxLength(500);

            entity.HasOne(dc => dc.Doctor)
                  .WithMany(d => d.DoctorClinics)
                  .HasForeignKey(dc => dc.DoctorId);

            entity.HasOne(dc => dc.Clinic)
                  .WithMany(c => c.DoctorClinics)
                  .HasForeignKey(dc => dc.ClinicId);
        });

        // ── Patient ──
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.DateOfBirth).HasMaxLength(50);
            entity.Property(e => e.ContactNumber).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.BloodGroup).HasMaxLength(10);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.RegistrationDate).HasMaxLength(50);

            entity.HasOne(e => e.Clinic)
                  .WithMany(c => c.Patients)
                  .HasForeignKey(e => e.ClinicId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Appointment ──
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.Appointments)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Doctor)
                  .WithMany(d => d.Appointments)
                  .HasForeignKey(e => e.DoctorId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Clinic)
                  .WithMany(c => c.Appointments)
                  .HasForeignKey(e => e.ClinicId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── BillingRecord ──
        modelBuilder.Entity<BillingRecord>(entity =>
        {
            entity.ToTable("BillingRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PaidAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.DateIssued).HasMaxLength(50);
            entity.Property(e => e.PaymentMethod).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.BillingRecords)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Appointment)
                  .WithOne(a => a.BillingRecord)
                  .HasForeignKey<BillingRecord>(e => e.AppointmentId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Clinic)
                  .WithMany(c => c.BillingRecords)
                  .HasForeignKey(e => e.ClinicId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Owned collection – PaymentLog
            entity.OwnsMany(e => e.Payments, payment =>
            {
                payment.ToTable("PaymentLogs");
                payment.Property(p => p.Amount).HasColumnType("decimal(18,2)");
                payment.Property(p => p.Date).HasMaxLength(50);
                payment.Property(p => p.PaymentMethod).HasMaxLength(100);
            });
        });

        // ── Prescription ──
        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.ToTable("Prescriptions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.Appointment)
                  .WithOne(a => a.Prescription)
                  .HasForeignKey<Prescription>(e => e.AppointmentId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.Prescriptions)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Doctor)
                  .WithMany(d => d.Prescriptions)
                  .HasForeignKey(e => e.DoctorId)
                  .OnDelete(DeleteBehavior.NoAction);

            // Owned collection – MedicationItem
            entity.OwnsMany(e => e.Medications, med =>
            {
                med.ToTable("MedicationItems");
                med.Property(m => m.Name).HasMaxLength(200);
                med.Property(m => m.Dosage).HasMaxLength(100);
                med.Property(m => m.Frequency).HasMaxLength(100);
                med.Property(m => m.Duration).HasMaxLength(100);
            });
        });

        // ── DentalLog ──
        modelBuilder.Entity<DentalLog>(entity =>
        {
            entity.ToTable("DentalLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ToothNumber).HasMaxLength(10);
            entity.Property(e => e.DoctorId);
            entity.Property(e => e.DoctorName).HasMaxLength(200);
            entity.Property(e => e.Date).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(500); // JSON array
            entity.Property(e => e.PainDetails).HasMaxLength(1000);
            entity.Property(e => e.Treatment).HasMaxLength(500);
            entity.Property(e => e.Medication).HasMaxLength(500);

            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.DentalLogs)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
