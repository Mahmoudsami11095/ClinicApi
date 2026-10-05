using System;
using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Clinic.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clinic.IntegrationTests;

public class CommissionIntegrationTests
{
    [Fact]
    public async Task CommissionService_GetAnalytics_ComputesAggregatesAndDoctorBreakdown()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new ClinicDbContext(options);

        var clinic = new ClinicEntity { Id = "c-1", Name = "Downtown Dental" };
        var doctor = new Doctor { Id = "doc-1", FirstName = "Hassan", LastName = "Adel", Specialization = "Endodontics" };
        var patient = new Patient { Id = "pat-1", FirstName = "Samir", LastName = "Ibrahim" };

        var appt = new Appointment
        {
            Id = "apt-1",
            ClinicId = clinic.Id,
            DoctorId = doctor.Id,
            PatientId = patient.Id,
            Doctor = doctor,
            Patient = patient
        };

        var bill = new BillingRecord
        {
            Id = "b-1",
            ClinicId = clinic.Id,
            AppointmentId = appt.Id,
            PatientId = patient.Id,
            Description = "Root Canal Treatment Single Canal",
            Amount = 1500m,
            PaidAmount = 1500m,
            Status = "paid",
            DateIssued = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Appointment = appt,
            Patient = patient
        };

        db.Clinics.Add(clinic);
        db.Doctors.Add(doctor);
        db.Patients.Add(patient);
        db.Appointments.Add(appt);
        db.BillingRecords.Add(bill);
        await db.SaveChangesAsync();

        var service = new CommissionService(db);
        var analytics = await service.GetAnalyticsAsync(clinic.Id, doctor.Id, null, null);

        Assert.NotNull(analytics);
        Assert.Equal(1500m, analytics.TotalGrossRevenue);
        Assert.True(analytics.TotalNetCommission > 0);
        Assert.Single(analytics.Doctors);
        Assert.Equal("Dr. Hassan Adel", analytics.Doctors[0].DoctorName);
        Assert.Single(analytics.EncounterItems);
    }

    [Fact]
    public async Task CommissionService_CreateAndSettlePayout_TracksStatusAndLedger()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new ClinicDbContext(options);

        var doctor = new Doctor { Id = "doc-2", FirstName = "Nour", LastName = "ElDin", Specialization = "Orthodontics" };
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        var service = new CommissionService(db);

        // 1. Upsert plan
        var plan = await service.UpsertPlanAsync(new CreateOrUpdateCommissionPlanDto
        {
            DoctorId = doctor.Id,
            DefaultCommissionRate = 35.0m,
            LabFeeDeductionType = "BeforeCommission"
        });
        Assert.Equal(35.0m, plan.DefaultCommissionRate);

        // 2. Create payout
        var payout = await service.CreatePayoutAsync(new CreateCommissionPayoutDto
        {
            DoctorId = doctor.Id,
            Notes = "Monthly settlement for September"
        });
        Assert.NotNull(payout);
        Assert.Equal("Draft", payout.Status);

        // 3. Settle payout
        var settled = await service.SettlePayoutAsync(payout.Id, new SettleCommissionPayoutDto
        {
            PaymentReference = "TRX-98234-CIB",
            Notes = "Wire transfer confirmed"
        });

        Assert.NotNull(settled);
        Assert.Equal("Paid", settled.Status);
        Assert.Equal("TRX-98234-CIB", settled.PaymentReference);
        Assert.NotNull(settled.PaidAt);
    }
}
