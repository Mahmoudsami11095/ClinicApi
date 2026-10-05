using Clinic.API.Controllers;
using Clinic.Application.DTOs;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Clinic.IntegrationTests;

public class SearchControllerIntegrationTests
{
    private ClinicDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ClinicDbContext(options);
    }

    [Fact]
    public async Task QuickSearch_WithEmptyQuery_ReturnsEmptyLists()
    {
        using var context = CreateInMemoryDbContext();
        var controller = new SearchController(context);

        var result = await controller.QuickSearch("", 10);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var searchResult = Assert.IsType<SearchResultDto>(okResult.Value);
        Assert.Empty(searchResult.Patients);
        Assert.Empty(searchResult.Doctors);
        Assert.Empty(searchResult.Chairs);
        Assert.Empty(searchResult.Materials);
    }

    [Fact]
    public async Task QuickSearch_WithMatchingPatient_ReturnsPatientDetails()
    {
        using var context = CreateInMemoryDbContext();
        context.Patients.AddRange(
            new Patient { Id = "p-1", FirstName = "Sarah", LastName = "Connor", PhoneNumber = "01011112222", Gender = "Female", ClinicId = "c-1" },
            new Patient { Id = "p-2", FirstName = "John", LastName = "Doe", PhoneNumber = "01099998888", Gender = "Male", ClinicId = "c-1" }
        );
        await context.SaveChangesAsync();

        var controller = new SearchController(context);
        var result = await controller.QuickSearch("connor", 5);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var searchResult = Assert.IsType<SearchResultDto>(okResult.Value);
        Assert.Single(searchResult.Patients);
        Assert.Equal("Sarah Connor", searchResult.Patients[0].Name);
        Assert.Equal("01011112222", searchResult.Patients[0].Phone);
    }

    [Fact]
    public async Task QuickSearch_WithMatchingDoctor_ReturnsDoctorAndSpecialty()
    {
        using var context = CreateInMemoryDbContext();
        context.Doctors.AddRange(
            new Doctor { Id = "d-1", FirstName = "Sherif", LastName = "Hassan", Specialization = "Orthodontics", Email = "sherif@clinic.com" },
            new Doctor { Id = "d-2", FirstName = "Mona", LastName = "Zaki", Specialization = "Endodontics", Email = "mona@clinic.com" }
        );
        await context.SaveChangesAsync();

        var controller = new SearchController(context);
        var result = await controller.QuickSearch("Ortho", 5);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var searchResult = Assert.IsType<SearchResultDto>(okResult.Value);
        Assert.Single(searchResult.Doctors);
        Assert.Equal("Sherif Hassan", searchResult.Doctors[0].Name);
        Assert.Equal("Orthodontics", searchResult.Doctors[0].Specialization);
    }

    [Fact]
    public async Task QuickSearch_WithMatchingChairOrRoom_ReturnsOperatory()
    {
        using var context = CreateInMemoryDbContext();
        context.ClinicChairs.AddRange(
            new ClinicChair { Id = "c-101", ClinicId = "c1", RoomNumber = "101", ChairName = "Operatory 1 - Restorative", Status = "available" },
            new ClinicChair { Id = "c-102", ClinicId = "c1", RoomNumber = "102", ChairName = "Surgical Suite", Status = "occupied" }
        );
        await context.SaveChangesAsync();

        var controller = new SearchController(context);
        var result = await controller.QuickSearch("Surgical", 5);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var searchResult = Assert.IsType<SearchResultDto>(okResult.Value);
        Assert.Single(searchResult.Chairs);
        Assert.Equal("102", searchResult.Chairs[0].RoomNumber);
        Assert.Equal("Surgical Suite", searchResult.Chairs[0].ChairName);
        Assert.Equal("occupied", searchResult.Chairs[0].Status);
    }

    [Fact]
    public async Task QuickSearch_WithMatchingMaterial_ReturnsInventoryDetails()
    {
        using var context = CreateInMemoryDbContext();
        context.Materials.AddRange(
            new Material { Id = "m-1", Name = "Composite A2 Filtek", Category = "Restorative", Quantity = 25, Unit = "Syringes" },
            new Material { Id = "m-2", Name = "Dental Anesthetic Articaine 4%", Category = "Anesthesia", Quantity = 10, Unit = "Carpules" }
        );
        await context.SaveChangesAsync();

        var controller = new SearchController(context);
        var result = await controller.QuickSearch("Articaine", 5);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var searchResult = Assert.IsType<SearchResultDto>(okResult.Value);
        Assert.Single(searchResult.Materials);
        Assert.Equal("Dental Anesthetic Articaine 4%", searchResult.Materials[0].Name);
        Assert.Equal(10, searchResult.Materials[0].Quantity);
    }

    [Fact]
    public async Task QuickSearch_RespectsLimitConstraint()
    {
        using var context = CreateInMemoryDbContext();
        for (int i = 1; i <= 15; i++)
        {
            context.Patients.Add(new Patient
            {
                Id = $"p-{i}",
                FirstName = $"Patient{i:D2}",
                LastName = "TestGroup",
                PhoneNumber = $"010000000{i:D2}"
            });
        }
        await context.SaveChangesAsync();

        var controller = new SearchController(context);
        var result = await controller.QuickSearch("TestGroup", 4);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var searchResult = Assert.IsType<SearchResultDto>(okResult.Value);
        Assert.Equal(4, searchResult.Patients.Count);
    }
}
