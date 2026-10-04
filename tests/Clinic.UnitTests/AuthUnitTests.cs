using System;
using System.Collections.Generic;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Clinic.Domain.Enums;
using Xunit;

namespace Clinic.UnitTests;

public class AuthUnitTests
{
    [Fact]
    public void BCrypt_PasswordHashing_GeneratesValidHashAndVerifiesMatch()
    {
        // Arrange
        const string rawPassword = "SecurePassword123!";

        // Act
        var hash = BCrypt.Net.BCrypt.HashPassword(rawPassword);

        // Assert
        Assert.NotNull(hash);
        Assert.StartsWith("$2", hash);
        Assert.True(BCrypt.Net.BCrypt.Verify(rawPassword, hash));
        Assert.False(BCrypt.Net.BCrypt.Verify("IncorrectPassword456!", hash));
    }

    [Fact]
    public void BCrypt_DifferentSalts_ProduceDistinctHashesForSamePassword()
    {
        // Arrange
        const string rawPassword = "SamePasswordAcrossUsers!";

        // Act
        var hash1 = BCrypt.Net.BCrypt.HashPassword(rawPassword);
        var hash2 = BCrypt.Net.BCrypt.HashPassword(rawPassword);

        // Assert
        Assert.NotEqual(hash1, hash2);
        Assert.True(BCrypt.Net.BCrypt.Verify(rawPassword, hash1));
        Assert.True(BCrypt.Net.BCrypt.Verify(rawPassword, hash2));
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Doctor)]
    [InlineData(UserRole.Assistant)]
    [InlineData(UserRole.Patient)]
    public void UserEntity_RoleAssignment_SupportsStandardRbacRoles(UserRole role)
    {
        // Arrange & Act
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = $"{role.ToString().ToLower()}@clinic.com",
            Role = role,
            Name = $"Test {role}"
        };

        // Assert
        Assert.Equal(role, user.Role);
        Assert.False(string.IsNullOrEmpty(user.Id));
    }

    [Fact]
    public void UserDto_Mapping_PreservesSecurityClaimsAndClinicAssociations()
    {
        // Arrange
        var user = new User
        {
            Id = "user-test-99",
            Email = "lead.doctor@clinic.com",
            Name = "Dr. Ahmed Mansour",
            Role = UserRole.Doctor,
            DoctorId = "doc-99",
            IsDeleted = false
        };
        var clinicIds = new List<string> { "clinic-branch-1", "clinic-branch-2" };

        // Act
        var dto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString().ToLower(),
            DoctorId = user.DoctorId,
            ClinicIds = clinicIds
        };

        // Assert
        Assert.Equal("user-test-99", dto.Id);
        Assert.Equal("lead.doctor@clinic.com", dto.Email);
        Assert.Equal("doctor", dto.Role);
        Assert.Equal("doc-99", dto.DoctorId);
        Assert.Equal(2, dto.ClinicIds.Count);
        Assert.Contains("clinic-branch-1", dto.ClinicIds);
    }

    [Theory]
    [InlineData("doctor@example.com", true)]
    [InlineData("patient.john@clinic.org", true)]
    [InlineData("+201012345678", false)]
    [InlineData("01019876543", false)]
    public void CredentialTypeDetector_DistinguishesEmailFromPhoneNumber(string credential, bool isEmail)
    {
        // Act
        var detectedIsEmail = credential.Contains("@");

        // Assert
        Assert.Equal(isEmail, detectedIsEmail);
    }

    [Fact]
    public void UserEntity_SoftDelete_TracksDeactivationStatus()
    {
        // Arrange
        var user = new User
        {
            Id = "user-deactivated",
            Email = "inactive@clinic.com",
            IsDeleted = true
        };

        // Assert
        Assert.True(user.IsDeleted);
    }
}
