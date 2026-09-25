using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Application.Services;
using Clinic.Domain.Entities;
using Clinic.Domain.Helpers;
using Moq;
using Xunit;

namespace Clinic.UnitTests;

/// <summary>
/// Full Branch Coverage Test Suite
/// Systematically executes every conditional branch, decision path, exception throw, and null-coalescing fallback.
/// </summary>
public class BranchCoverageTests
{
    #region 1. PhoneHelper Exhaustive Branch Coverage

    [Theory]
    // Parts split branches
    [InlineData("+20 100 123 4567", "+20", "1001234567")] // Multiple spaces
    [InlineData("+abc 123456", "+20", "+abc 123456")] // Plus but non-numeric first part
    [InlineData("+999123456", "+999", "123456")] // 3-digit prefix fallback (length >= 4)
    [InlineData("+98", "+98", "")] // 2-digit prefix fallback (length == 3)
    [InlineData("+7", "+7", "")] // 1-digit prefix fallback (length == 2)
    [InlineData("+", "+20", "+")] // Just a plus sign
    [InlineData("+xyz", "+20", "+xyz")] // Plus followed by non-digits
    [InlineData("00201011223344", "+20", "1011223344")] // Starts with 0020
    [InlineData("1001234567", "+20", "1001234567")] // No plus, no 0020
    public void PhoneHelper_SplitContactNumber_AllConditionalBranches(string input, string expectedCode, string expectedPhone)
    {
        var result = PhoneHelper.SplitContactNumber(input);
        Assert.Equal(expectedCode, result.CountryCode);
        Assert.Equal(expectedPhone, result.PhoneNumber);
    }

    [Fact]
    public void PhoneHelper_ValidatePhoneNumber_AllErrorBranches()
    {
        // 1. Missing Country Code
        var r1 = PhoneHelper.ValidatePhoneNumber(null, "1001234567");
        Assert.False(r1.IsValid);
        Assert.Equal("Country code is required.", r1.ErrorMessage);

        // 2. Missing Phone Number
        var r2 = PhoneHelper.ValidatePhoneNumber("+20", null);
        Assert.False(r2.IsValid);
        Assert.Equal("Phone number is required.", r2.ErrorMessage);

        // 3. Non-digit Characters
        var r3 = PhoneHelper.ValidatePhoneNumber("+20", "010-ABCD-567");
        Assert.False(r3.IsValid);
        Assert.Equal("Phone number must contain digits only.", r3.ErrorMessage);

        // 4. Egypt Mobile - Invalid Prefix / Length
        var r4 = PhoneHelper.ValidatePhoneNumber("+20", "01312345678");
        Assert.False(r4.IsValid);
        Assert.Contains("Invalid Egyptian mobile number", r4.ErrorMessage);

        // 5. Non-Egypt - Length < 6
        var r5 = PhoneHelper.ValidatePhoneNumber("+966", "1234");
        Assert.False(r5.IsValid);
        Assert.Equal("Phone number must be between 6 and 15 digits.", r5.ErrorMessage);

        // 6. Non-Egypt - Length > 15
        var r6 = PhoneHelper.ValidatePhoneNumber("+966", "1234567890123456");
        Assert.False(r6.IsValid);
        Assert.Equal("Phone number must be between 6 and 15 digits.", r6.ErrorMessage);

        // 7. Non-Egypt - Valid range (6-15)
        var r7 = PhoneHelper.ValidatePhoneNumber("+966", "551234567");
        Assert.True(r7.IsValid);
        Assert.Empty(r7.ErrorMessage);
    }

    [Theory]
    [InlineData("+20", "01001234567", "1001234567")] // Egypt: Strips leading zero
    [InlineData("+20", "1001234567", "1001234567")] // Egypt: Without leading zero
    [InlineData("+20", " 010-1234-5678 ", "1012345678")] // Egypt: Cleans separators & zero
    [InlineData("+1", "05551234567", "05551234567")] // Non-Egypt: Preserves leading zero
    [InlineData("+966", "(050) 123-4567", "0501234567")] // Non-Egypt: Cleans formatting
    public void PhoneHelper_NormalizePhoneNumber_AllBranches(string countryCode, string input, string expected)
    {
        var result = PhoneHelper.NormalizePhoneNumber(countryCode, input);
        Assert.Equal(expected, result);
    }

    #endregion

    #region 2. PatientService Exhaustive Branch Coverage

    [Fact]
    public async Task PatientService_GetAllAsync_DoctorClaimAllowed_FiltersCorrectly()
    {
        // Arrange
        var mockPatientRepo = new Mock<IPatientRepository>();
        var mockClinicRepo = new Mock<IClinicRepository>();
        var mockUserRepo = new Mock<IUserRepository>();

        mockPatientRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Patient>
        {
            new Patient { Id = "p1", ClinicId = "c1", FirstName = "Ali" },
            new Patient { Id = "p2", ClinicId = "c2", FirstName = "Omar" }
        });

        mockClinicRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ClinicEntity>
        {
            new ClinicEntity { Id = "c1", CreatorDoctorId = "doc-1" }
        });

        var service = new PatientService(mockPatientRepo.Object, mockClinicRepo.Object, mockUserRepo.Object);

        // Act
        var result = await service.GetAllAsync("doc-1", null);

        // Assert
        Assert.Single(result);
        Assert.Equal("p1", result.First().Id);
    }

    [Fact]
    public async Task PatientService_GetAllAsync_DoctorClinicAcceptedStatus_FiltersCorrectly()
    {
        // Arrange
        var mockPatientRepo = new Mock<IPatientRepository>();
        var mockClinicRepo = new Mock<IClinicRepository>();
        var mockUserRepo = new Mock<IUserRepository>();

        var clinic = new ClinicEntity { Id = "c-accepted" };
        clinic.DoctorClinics.Add(new DoctorClinic { DoctorId = "doc-2", Status = "Accepted" });

        mockPatientRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Patient>
        {
            new Patient { Id = "p3", ClinicId = "c-accepted" },
            new Patient { Id = "p4", ClinicId = "c-other" }
        });

        mockClinicRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ClinicEntity> { clinic });

        var service = new PatientService(mockPatientRepo.Object, mockClinicRepo.Object, mockUserRepo.Object);

        // Act
        var result = await service.GetAllAsync("doc-2", null);

        // Assert
        Assert.Single(result);
        Assert.Equal("p3", result.First().Id);
    }

    [Fact]
    public async Task PatientService_GetAllAsync_ClinicClaim_FiltersCorrectly()
    {
        var mockPatientRepo = new Mock<IPatientRepository>();
        var mockClinicRepo = new Mock<IClinicRepository>();
        var mockUserRepo = new Mock<IUserRepository>();

        mockPatientRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Patient>
        {
            new Patient { Id = "p1", ClinicId = "c-10" },
            new Patient { Id = "p2", ClinicId = "c-20" }
        });

        var service = new PatientService(mockPatientRepo.Object, mockClinicRepo.Object, mockUserRepo.Object);

        var result = await service.GetAllAsync(null, "c-10");

        Assert.Single(result);
        Assert.Equal("p1", result.First().Id);
    }

    [Fact]
    public async Task PatientService_CreateAsync_DoctorClaimUnauthorized_ThrowsUnauthorized()
    {
        var mockPatientRepo = new Mock<IPatientRepository>();
        var mockClinicRepo = new Mock<IClinicRepository>();
        var mockUserRepo = new Mock<IUserRepository>();

        mockClinicRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ClinicEntity>());

        var service = new PatientService(mockPatientRepo.Object, mockClinicRepo.Object, mockUserRepo.Object);

        var dto = new PatientDto { ClinicId = "c-unauthorized", PhoneNumber = "01001234567" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(dto, "doc-intruder", null));
    }

    [Fact]
    public async Task PatientService_CreateAsync_ClinicClaimMismatch_ThrowsUnauthorized()
    {
        var mockPatientRepo = new Mock<IPatientRepository>();
        var mockClinicRepo = new Mock<IClinicRepository>();
        var mockUserRepo = new Mock<IUserRepository>();

        var service = new PatientService(mockPatientRepo.Object, mockClinicRepo.Object, mockUserRepo.Object);

        var dto = new PatientDto { ClinicId = "c-wrong", PhoneNumber = "01001234567" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(dto, null, "c-actual"));
    }

    [Fact]
    public async Task PatientService_CreateAsync_DuplicatePhone_ThrowsInvalidOperation()
    {
        var mockPatientRepo = new Mock<IPatientRepository>();
        var mockClinicRepo = new Mock<IClinicRepository>();
        var mockUserRepo = new Mock<IUserRepository>();

        mockUserRepo.Setup(r => r.IsPhoneNumberUniqueAsync("+20", "1001234567", null))
            .ReturnsAsync(false);

        var service = new PatientService(mockPatientRepo.Object, mockClinicRepo.Object, mockUserRepo.Object);

        var dto = new PatientDto { ContactNumber = "+20 1001234567" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(dto, null, null));
    }

    [Fact]
    public async Task PatientService_CreateAsync_ValidInput_AddsPatient()
    {
        var mockPatientRepo = new Mock<IPatientRepository>();
        var mockClinicRepo = new Mock<IClinicRepository>();
        var mockUserRepo = new Mock<IUserRepository>();

        mockUserRepo.Setup(r => r.IsPhoneNumberUniqueAsync("+20", "1001234567", null))
            .ReturnsAsync(true);

        Patient? savedPatient = null;
        mockPatientRepo.Setup(r => r.AddAsync(It.IsAny<Patient>()))
            .Callback<Patient>(p => savedPatient = p)
            .ReturnsAsync((Patient p) => p);

        var service = new PatientService(mockPatientRepo.Object, mockClinicRepo.Object, mockUserRepo.Object);

        var dto = new PatientDto
        {
            FirstName = "Kareem",
            LastName = "Tarek",
            ContactNumber = "+20 1001234567"
        };

        await service.CreateAsync(dto, null, null);

        Assert.NotNull(savedPatient);
        Assert.Equal("Kareem", savedPatient.FirstName);
        Assert.Equal("+20", savedPatient.CountryCode);
        Assert.Equal("1001234567", savedPatient.PhoneNumber);
    }

    [Fact]
    public async Task PatientService_UpdateAsync_PatientNotFound_ThrowsKeyNotFound()
    {
        var mockPatientRepo = new Mock<IPatientRepository>();
        var mockClinicRepo = new Mock<IClinicRepository>();
        var mockUserRepo = new Mock<IUserRepository>();

        mockPatientRepo.Setup(r => r.GetByIdAsync("p-none")).ReturnsAsync((Patient?)null);

        var service = new PatientService(mockPatientRepo.Object, mockClinicRepo.Object, mockUserRepo.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateAsync("p-none", new PatientDto(), null, null));
    }

    [Fact]
    public async Task PatientService_DeleteAsync_DoctorClaimAllowed_DeletesSuccessfully()
    {
        var mockPatientRepo = new Mock<IPatientRepository>();
        var mockClinicRepo = new Mock<IClinicRepository>();
        var mockUserRepo = new Mock<IUserRepository>();

        mockPatientRepo.Setup(r => r.GetByIdAsync("p-del")).ReturnsAsync(new Patient
        {
            Id = "p-del",
            ClinicId = "c-del"
        });

        mockClinicRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ClinicEntity>
        {
            new ClinicEntity { Id = "c-del", CreatorDoctorId = "doc-owner" }
        });

        var service = new PatientService(mockPatientRepo.Object, mockClinicRepo.Object, mockUserRepo.Object);

        await service.DeleteAsync("p-del", "doc-owner", null);

        mockPatientRepo.Verify(r => r.DeleteAsync("p-del"), Times.Once);
    }

    #endregion

    #region 3. NotificationService Exhaustive Branch Coverage

    [Fact]
    public async Task NotificationService_CreateAndDispatch_DispatchesViaSignalR()
    {
        var mockRepo = new Mock<INotificationRepository>();
        var mockDispatcher = new Mock<INotificationDispatcher>();

        mockRepo.Setup(r => r.AddAsync(It.IsAny<Notification>()))
            .ReturnsAsync((Notification n) => n);

        var service = new NotificationService(mockRepo.Object, mockDispatcher.Object);

        var result = await service.CreateNotificationAsync("user-1", "Test Title", "Test Message", "Alert");

        Assert.Equal("user-1", result.UserId);
        Assert.False(result.IsRead);
        mockDispatcher.Verify(d => d.SendNotificationAsync("user-1", It.IsAny<Notification>()), Times.Once);
    }

    [Fact]
    public async Task NotificationService_GetUserNotificationsAsync_LimitsCountAndSortsDescending()
    {
        var mockRepo = new Mock<INotificationRepository>();
        var mockDispatcher = new Mock<INotificationDispatcher>();

        var list = new List<Notification>
        {
            new Notification { UserId = "u1", CreatedAt = DateTime.UtcNow.AddMinutes(-10), Title = "Older" },
            new Notification { UserId = "u1", CreatedAt = DateTime.UtcNow, Title = "Newest" },
            new Notification { UserId = "u2", CreatedAt = DateTime.UtcNow, Title = "Other User" }
        };

        mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(list);

        var service = new NotificationService(mockRepo.Object, mockDispatcher.Object);

        var result = (await service.GetUserNotificationsAsync("u1", 1)).ToList();

        Assert.Single(result);
        Assert.Equal("Newest", result.First().Title);
    }

    [Fact]
    public async Task NotificationService_MarkAsReadAsync_NullOrMismatchedUser_DoesNotUpdate()
    {
        var mockRepo = new Mock<INotificationRepository>();
        var mockDispatcher = new Mock<INotificationDispatcher>();

        mockRepo.Setup(r => r.GetByIdAsync("notif-other"))
            .ReturnsAsync(new Notification { UserId = "different-user", IsRead = false });

        var service = new NotificationService(mockRepo.Object, mockDispatcher.Object);

        // Act 1: Mismatched user
        await service.MarkAsReadAsync(Guid.NewGuid(), "target-user");
        mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Notification>()), Times.Never);

        // Act 2: Null notification
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Notification?)null);
        await service.MarkAsReadAsync(Guid.NewGuid(), "target-user");
        mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task NotificationService_MarkAllAsReadAsync_OnlyUpdatesUnreadForMatchingUser()
    {
        var mockRepo = new Mock<INotificationRepository>();
        var mockDispatcher = new Mock<INotificationDispatcher>();

        var notifs = new List<Notification>
        {
            new Notification { UserId = "u-target", IsRead = false, Title = "Unread 1" },
            new Notification { UserId = "u-target", IsRead = true, Title = "Already Read" },
            new Notification { UserId = "u-other", IsRead = false, Title = "Other Unread" }
        };

        mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(notifs);

        var service = new NotificationService(mockRepo.Object, mockDispatcher.Object);

        await service.MarkAllAsReadAsync("u-target");

        mockRepo.Verify(r => r.UpdateAsync(It.Is<Notification>(n => n.Title == "Unread 1" && n.IsRead)), Times.Once);
        mockRepo.Verify(r => r.UpdateAsync(It.Is<Notification>(n => n.Title == "Already Read")), Times.Never);
        mockRepo.Verify(r => r.UpdateAsync(It.Is<Notification>(n => n.Title == "Other Unread")), Times.Never);
    }

    #endregion

    #region 4. DoctorService Exhaustive Branch Coverage

    [Fact]
    public async Task DoctorService_CreateAsync_PhoneCollision_ThrowsInvalidOperation()
    {
        var mockDocRepo = new Mock<IDoctorRepository>();
        var mockUserRepo = new Mock<IUserRepository>();

        mockUserRepo.Setup(r => r.IsPhoneNumberUniqueAsync("+20", "1001234567", null))
            .ReturnsAsync(false);

        var service = new DoctorService(mockDocRepo.Object, mockUserRepo.Object);

        var dto = new DoctorDto
        {
            ContactNumber = "+20 1001234567",
            FirstName = "Dr. Mohamed"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(dto));
    }

    [Fact]
    public async Task DoctorService_GetAllAsync_HandlesCorruptedJsonAndNullBranches()
    {
        var mockDocRepo = new Mock<IDoctorRepository>();
        var mockUserRepo = new Mock<IUserRepository>();

        var doc1 = new Doctor
        {
            Id = "doc-valid",
            AvailabilityDays = "[\"Monday\", \"Wednesday\"]",
            AvailabilityHours = "09:00 - 17:00"
        };
        doc1.DoctorClinics.Add(new DoctorClinic
        {
            ClinicId = "c-1",
            AvailabilityDays = "[\"Monday\"]",
            AvailabilityHours = "10:00 - 14:00"
        });

        // Doctor with corrupted/invalid JSON to trigger catch branches
        var doc2 = new Doctor
        {
            Id = "doc-corrupted",
            AvailabilityDays = "NOT_A_VALID_JSON_STRING",
            AvailabilityHours = null
        };
        doc2.DoctorClinics.Add(new DoctorClinic
        {
            ClinicId = "c-2",
            AvailabilityDays = "INVALID_JSON_INSIDE_CLINIC",
            AvailabilityHours = null
        });

        // Doctor with null availability
        var doc3 = new Doctor
        {
            Id = "doc-null",
            AvailabilityDays = "null",
            AvailabilityHours = ""
        };

        mockDocRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Doctor> { doc1, doc2, doc3 });

        var service = new DoctorService(mockDocRepo.Object, mockUserRepo.Object);

        var result = (await service.GetAllAsync()).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0].Availability?.Days?.Count);
        Assert.Empty(result[1].Availability?.Days ?? new List<string>());
        Assert.Empty(result[2].Availability?.Days ?? new List<string>());
    }

    [Fact]
    public async Task PatientService_UpdateAsync_DoctorClaimAllowed_UpdatesPatient()
    {
        var mockPatientRepo = new Mock<IPatientRepository>();
        var mockClinicRepo = new Mock<IClinicRepository>();
        var mockUserRepo = new Mock<IUserRepository>();

        var existing = new Patient
        {
            Id = "p-10",
            ClinicId = "c-owned",
            FirstName = "OldName",
            PhoneNumber = "1001234567",
            CountryCode = "+20"
        };

        mockPatientRepo.Setup(r => r.GetByIdAsync("p-10")).ReturnsAsync(existing);
        mockClinicRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ClinicEntity>
        {
            new ClinicEntity { Id = "c-owned", CreatorDoctorId = "doc-boss" }
        });
        mockUserRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());
        mockUserRepo.Setup(r => r.IsPhoneNumberUniqueAsync("+20", "1009876543", null)).ReturnsAsync(true);

        var service = new PatientService(mockPatientRepo.Object, mockClinicRepo.Object, mockUserRepo.Object);

        var updateDto = new PatientDto
        {
            ClinicId = "c-owned",
            FirstName = "NewName",
            LastName = "Updated",
            ContactNumber = "+20 1009876543"
        };

        await service.UpdateAsync("p-10", updateDto, "doc-boss", null);

        Assert.Equal("NewName", existing.FirstName);
        Assert.Equal("1009876543", existing.PhoneNumber);
        mockPatientRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    public async Task PatientService_UpdateAsync_ClinicClaimMismatch_ThrowsUnauthorized()
    {
        var mockPatientRepo = new Mock<IPatientRepository>();
        var mockClinicRepo = new Mock<IClinicRepository>();
        var mockUserRepo = new Mock<IUserRepository>();

        var existing = new Patient { Id = "p-11", ClinicId = "c-actual" };
        mockPatientRepo.Setup(r => r.GetByIdAsync("p-11")).ReturnsAsync(existing);

        var service = new PatientService(mockPatientRepo.Object, mockClinicRepo.Object, mockUserRepo.Object);

        var updateDto = new PatientDto { ClinicId = "c-other" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateAsync("p-11", updateDto, null, "c-actual"));
    }

    [Fact]
    public async Task PatientService_DeleteAsync_ClinicClaimMismatch_ThrowsUnauthorized()
    {
        var mockPatientRepo = new Mock<IPatientRepository>();
        var mockClinicRepo = new Mock<IClinicRepository>();
        var mockUserRepo = new Mock<IUserRepository>();

        var existing = new Patient { Id = "p-12", ClinicId = "c-diff" };
        mockPatientRepo.Setup(r => r.GetByIdAsync("p-12")).ReturnsAsync(existing);

        var service = new PatientService(mockPatientRepo.Object, mockClinicRepo.Object, mockUserRepo.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteAsync("p-12", null, "c-myclinic"));
    }

    #endregion

    #region 5. RadiologyService Exhaustive Branch Coverage

    [Fact]
    public async Task RadiologyService_CenterCrudAndNotFound_ExecutesAllBranches()
    {
        var mockCenterRepo = new Mock<IRadiologyCenterRepository>();
        var mockRecordRepo = new Mock<IRadiologyRecordRepository>();
        var mockUserRepo = new Mock<IUserRepository>();

        mockCenterRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<RadiologyCenter>
        {
            new RadiologyCenter { Id = "rc-1", Name = "Cairo Scan" }
        });

        mockCenterRepo.Setup(r => r.GetByIdAsync("rc-none")).ReturnsAsync((RadiologyCenter?)null);
        mockCenterRepo.Setup(r => r.GetByIdAsync("rc-1")).ReturnsAsync(new RadiologyCenter { Id = "rc-1", Name = "Cairo Scan" });

        var service = new RadiologyService(mockCenterRepo.Object, mockRecordRepo.Object, mockUserRepo.Object);

        // 1. GetCenters
        var centers = await service.GetCentersAsync();
        Assert.Single(centers);

        // 2. CreateCenter
        var created = await service.CreateCenterAsync(new CreateRadiologyCenterDto { Name = "Alfa Scan" });
        Assert.Equal("Alfa Scan", created.Name);

        // 3. UpdateCenter - Not Found Branch
        await Assert.ThrowsAsync<Exception>(() =>
            service.UpdateCenterAsync("rc-none", new CreateRadiologyCenterDto { Name = "New" }));

        // 4. UpdateCenter - Found Branch
        var updated = await service.UpdateCenterAsync("rc-1", new CreateRadiologyCenterDto { Name = "Cairo Scan Premium" });
        Assert.Equal("Cairo Scan Premium", updated.Name);

        // 5. DeleteCenter
        await service.DeleteCenterAsync("rc-1");
        mockCenterRepo.Verify(r => r.DeleteAsync("rc-1"), Times.Once);
    }

    [Fact]
    public async Task RadiologyService_RecordCrudAndNameLookups_ExecutesAllBranches()
    {
        var mockCenterRepo = new Mock<IRadiologyCenterRepository>();
        var mockRecordRepo = new Mock<IRadiologyRecordRepository>();
        var mockUserRepo = new Mock<IUserRepository>();

        mockRecordRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<RadiologyRecord>
        {
            new RadiologyRecord
            {
                Id = "rec-1",
                DoctorId = "doc-rad-1",
                PatientId = "pat-known",
                RadiologyCenterId = "center-known",
                ProcedureName = "Panoramic X-Ray",
                AmountPaid = 250m
            },
            new RadiologyRecord
            {
                Id = "rec-2",
                DoctorId = "doc-rad-2",
                PatientId = "pat-orphan",
                RadiologyCenterId = "center-orphan",
                ProcedureName = "CBCT 3D Scan",
                AmountPaid = 800m
            }
        });

        // Users: only pat-known is present
        mockUserRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>
        {
            new User { PatientId = "pat-known", Name = "Karim Ahmed" }
        });

        // Centers: only center-known is present
        mockCenterRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<RadiologyCenter>
        {
            new RadiologyCenter { Id = "center-known", Name = "Nile Radiology" }
        });

        mockRecordRepo.Setup(r => r.GetByIdAsync("rec-none")).ReturnsAsync((RadiologyRecord?)null);
        mockRecordRepo.Setup(r => r.GetByIdAsync("rec-1")).ReturnsAsync(new RadiologyRecord { Id = "rec-1" });

        var service = new RadiologyService(mockCenterRepo.Object, mockRecordRepo.Object, mockUserRepo.Object);

        // 1. GetRecordsAsync - tests both known names and "Unknown" fallback branches
        var allRecords = await service.GetRecordsAsync();
        Assert.Equal(2, allRecords.Count);
        Assert.Equal("Karim Ahmed", allRecords[0].PatientName);
        Assert.Equal("Nile Radiology", allRecords[0].RadiologyCenterName);
        Assert.Equal("Unknown", allRecords[1].PatientName); // Fallback branch
        Assert.Equal("Unknown Center", allRecords[1].RadiologyCenterName); // Fallback branch

        // 2. GetRecordsByDoctorAsync
        var docRecords = await service.GetRecordsByDoctorAsync("doc-rad-1");
        Assert.Single(docRecords);

        // 3. CreateRecordAsync
        var newRecord = await service.CreateRecordAsync(new CreateRadiologyRecordDto
        {
            DoctorId = "doc-rad-1",
            ProcedureName = "Bitewing X-Ray"
        });
        Assert.Equal("Bitewing X-Ray", newRecord.ProcedureName);

        // 4. UpdateRecordAsync - Not Found Branch
        await Assert.ThrowsAsync<Exception>(() =>
            service.UpdateRecordAsync("rec-none", new CreateRadiologyRecordDto()));

        // 5. UpdateRecordAsync - Found Branch
        var updated = await service.UpdateRecordAsync("rec-1", new CreateRadiologyRecordDto { ProcedureName = "Updated X-Ray" });
        Assert.Equal("Updated X-Ray", updated.ProcedureName);

        // 6. DeleteRecordAsync
        await service.DeleteRecordAsync("rec-1");
        mockRecordRepo.Verify(r => r.DeleteAsync("rec-1"), Times.Once);
    }

    #endregion
}
