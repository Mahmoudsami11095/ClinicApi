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
    public DbSet<UserClinic> UserClinics => Set<UserClinic>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<RadiologyCenter> RadiologyCenters => Set<RadiologyCenter>();
    public DbSet<RadiologyRecord> RadiologyRecords => Set<RadiologyRecord>();
    public DbSet<Specialization> Specializations => Set<Specialization>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<SubscriptionSetting> SubscriptionSettings => Set<SubscriptionSetting>();
    public DbSet<SubscriptionReceipt> SubscriptionReceipts => Set<SubscriptionReceipt>();
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
            entity.Property(e => e.AvailabilityHours).HasMaxLength(50);
            entity.Property(e => e.AvailabilityDays).HasMaxLength(500);
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
            entity.Property(e => e.CountryCode).HasMaxLength(10).HasDefaultValue("+20");
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Ignore(e => e.ContactNumber);
            entity.Property(e => e.Avatar).HasMaxLength(500);
            entity.Property(e => e.AvailabilityDays).HasMaxLength(500);
            entity.Property(e => e.AvailabilityHours).HasMaxLength(50);
            entity.Property(e => e.ReceiptUrl).HasMaxLength(1000);

            entity.HasOne(e => e.SpecializationReference)
                  .WithMany(s => s.Doctors)
                  .HasForeignKey(e => e.SpecializationId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Specialization ──
        modelBuilder.Entity<Specialization>(entity =>
        {
            entity.ToTable("Specializations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.TranslationKey).HasMaxLength(100);
            entity.Property(e => e.Category).HasMaxLength(100);

            entity.HasData(
                new Specialization { Id = "s1", Name = "General Dentistry", TranslationKey = "auth.spec_general_dentistry", Category = "Dentistry" },
                new Specialization { Id = "s2", Name = "Orthodontics", TranslationKey = "auth.spec_orthodontics", Category = "Dentistry" },
                new Specialization { Id = "s3", Name = "Oral Surgery", TranslationKey = "auth.spec_oral_surgery", Category = "Dentistry" },
                new Specialization { Id = "s4", Name = "Endodontics", TranslationKey = "auth.spec_endodontics", Category = "Dentistry" },
                new Specialization { Id = "s5", Name = "Periodontics", TranslationKey = "auth.spec_periodontics", Category = "Dentistry" },
                new Specialization { Id = "s6", Name = "Pediatric Dentistry", TranslationKey = "auth.spec_pediatric_dentistry", Category = "Dentistry" },
                new Specialization { Id = "s7", Name = "Prosthodontics", TranslationKey = "auth.spec_prosthodontics", Category = "Dentistry" },
                new Specialization { Id = "s8", Name = "Cardiology", TranslationKey = "auth.spec_cardiology", Category = "Medicine" },
                new Specialization { Id = "s9", Name = "Dermatology", TranslationKey = "auth.spec_dermatology", Category = "Medicine" },
                new Specialization { Id = "s10", Name = "Endocrinology", TranslationKey = "auth.spec_endocrinology", Category = "Medicine" },
                new Specialization { Id = "s11", Name = "Gastroenterology", TranslationKey = "auth.spec_gastroenterology", Category = "Medicine" },
                new Specialization { Id = "s12", Name = "Neurology", TranslationKey = "auth.spec_neurology", Category = "Medicine" },
                new Specialization { Id = "s13", Name = "Obstetrics and Gynecology", TranslationKey = "auth.spec_obgyn", Category = "Medicine" },
                new Specialization { Id = "s14", Name = "Oncology", TranslationKey = "auth.spec_oncology", Category = "Medicine" },
                new Specialization { Id = "s15", Name = "Ophthalmology", TranslationKey = "auth.spec_ophthalmology", Category = "Medicine" },
                new Specialization { Id = "s16", Name = "Orthopedics", TranslationKey = "auth.spec_orthopedics", Category = "Medicine" },
                new Specialization { Id = "s17", Name = "Pediatrics", TranslationKey = "auth.spec_pediatrics", Category = "Medicine" },
                new Specialization { Id = "s18", Name = "Psychiatry", TranslationKey = "auth.spec_psychiatry", Category = "Medicine" },
                new Specialization { Id = "s19", Name = "Radiology", TranslationKey = "auth.spec_radiology", Category = "Medicine" },
                new Specialization { Id = "s20", Name = "Urology", TranslationKey = "auth.spec_urology", Category = "Medicine" },
                new Specialization { Id = "s21", Name = "General Practice", TranslationKey = "auth.spec_general_practice", Category = "Medicine" }
            );
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

        // ── UserClinic (Many-to-Many join) ──
        modelBuilder.Entity<UserClinic>(entity =>
        {
            entity.ToTable("UserClinics");
            entity.HasKey(uc => new { uc.UserId, uc.ClinicId });

            entity.HasOne(uc => uc.User)
                  .WithMany(u => u.UserClinics)
                  .HasForeignKey(uc => uc.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(uc => uc.Clinic)
                  .WithMany(c => c.UserClinics)
                  .HasForeignKey(uc => uc.ClinicId)
                  .OnDelete(DeleteBehavior.Cascade);
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
            entity.Property(e => e.CountryCode).HasMaxLength(10).HasDefaultValue("+20");
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Ignore(e => e.ContactNumber);
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
                  .WithMany(a => a.BillingRecords)
                  .HasForeignKey(e => e.AppointmentId)
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
            entity.Property(e => e.ConsumedMaterials).HasMaxLength(2000); // JSON array

            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.DentalLogs)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Notification ──
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(450).IsRequired(); // Matches Identity User Id length
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(50);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Material ──
        modelBuilder.Entity<Material>(entity =>
        {
            entity.ToTable("Materials");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DoctorId).IsRequired();
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.HasOne(e => e.Doctor)
                  .WithMany()
                  .HasForeignKey(e => e.DoctorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── RadiologyCenter ──
        modelBuilder.Entity<RadiologyCenter>(entity =>
        {
            entity.ToTable("RadiologyCenters");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ContactNumber).HasMaxLength(50);
            entity.Property(e => e.Address).HasMaxLength(500);
        });

        // ── RadiologyRecord ──
        modelBuilder.Entity<RadiologyRecord>(entity =>
        {
            entity.ToTable("RadiologyRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProcedureName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Date).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(e => e.RadiologyCenter)
                  .WithMany(c => c.RadiologyRecords)
                  .HasForeignKey(e => e.RadiologyCenterId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Doctor)
                  .WithMany(d => d.RadiologyRecords)
                  .HasForeignKey(e => e.DoctorId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.RadiologyRecords)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PromoCode ──
        modelBuilder.Entity<PromoCode>(entity =>
        {
            entity.ToTable("PromoCodes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.DiscountType).HasMaxLength(50);

            entity.HasData(
                new PromoCode { Id = "p1", Code = "FREE3MONTHS", DiscountType = "FreeMonths", Value = 3, ExpiryDate = new DateTime(2028, 1, 1, 0, 0, 0, DateTimeKind.Utc), MaxUses = 100, CurrentUses = 0, IsActive = true },
                new PromoCode { Id = "p2", Code = "SAVE50", DiscountType = "Flat", Value = 50, ExpiryDate = new DateTime(2028, 1, 1, 0, 0, 0, DateTimeKind.Utc), MaxUses = 100, CurrentUses = 0, IsActive = true },
                new PromoCode { Id = "p3", Code = "HALFPRICE", DiscountType = "Percent", Value = 50, ExpiryDate = new DateTime(2028, 1, 1, 0, 0, 0, DateTimeKind.Utc), MaxUses = 100, CurrentUses = 0, IsActive = true }
            );
        });

        // ── SubscriptionSetting ──
        modelBuilder.Entity<SubscriptionSetting>(entity =>
        {
            entity.ToTable("SubscriptionSettings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InitialSetupFee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AnnualSubscriptionFee).HasColumnType("decimal(18,2)");

            entity.HasData(
                new SubscriptionSetting { Id = "s_default", InitialSetupFee = 100.00m, AnnualSubscriptionFee = 300.00m, TrialDurationMonths = 6 }
            );
        });

        // ── SubscriptionReceipt ──
        modelBuilder.Entity<SubscriptionReceipt>(entity =>
        {
            entity.ToTable("SubscriptionReceipts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DoctorId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ReceiptUrl).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        });
    }
}
