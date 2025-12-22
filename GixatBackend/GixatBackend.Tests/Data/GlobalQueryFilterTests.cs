using FluentAssertions;
using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Sessions.Models;
using GixatBackend.Modules.Users.Enums;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Tests.Helpers;
using GixatBackend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Tests.Data;

/// <summary>
/// Tests to verify that global query filters properly isolate tenant data
/// </summary>
public class GlobalQueryFilterTests : IntegrationTestBase
{
    [Fact]
    public async Task Customers_ShouldBeFilteredByOrganization_Automatically()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        var context1 = CreateDbContext(org1Id);
        var context2 = CreateDbContext(org2Id);

        var customer1 = TestDataBuilder.CreateCustomer(org1Id, "Customer Org1");
        var customer2 = TestDataBuilder.CreateCustomer(org2Id, "Customer Org2");

        context1.Customers.Add(customer1);
        context1.Customers.Add(customer2);
        await context1.SaveChangesAsync();

        // Act
        var org1Customers = await context1.Customers.ToListAsync();
        var org2Customers = await context2.Customers.ToListAsync();

        // Assert
        org1Customers.Should().ContainSingle()
            .Which.FirstName.Should().Be("Customer");
        org2Customers.Should().ContainSingle()
            .Which.FirstName.Should().Be("Customer");
    }

    [Fact]
    public async Task Cars_ShouldBeFilteredByOrganization_Automatically()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var context1 = CreateDbContext(org1Id);
        var context2 = CreateDbContext(org2Id);

        var customer1 = TestDataBuilder.CreateCustomer(org1Id);
        var customer2 = TestDataBuilder.CreateCustomer(org2Id);
        context1.Customers.AddRange(customer1, customer2);
        await context1.SaveChangesAsync();

        var car1 = TestDataBuilder.CreateCar(customer1.Id, org1Id, "Toyota");
        var car2 = TestDataBuilder.CreateCar(customer2.Id, org2Id, "Honda");
        context1.Cars.AddRange(car1, car2);
        await context1.SaveChangesAsync();

        // Act
        var org1Cars = await context1.Cars.ToListAsync();
        var org2Cars = await context2.Cars.ToListAsync();

        // Assert
        org1Cars.Should().ContainSingle()
            .Which.Make.Should().Be("Toyota");
        org2Cars.Should().ContainSingle()
            .Which.Make.Should().Be("Honda");
    }

    [Fact]
    public async Task Sessions_ShouldBeFilteredByOrganization_Automatically()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var context1 = CreateDbContext(org1Id);
        var context2 = CreateDbContext(org2Id);

        var customer1 = TestDataBuilder.CreateCustomer(org1Id);
        var customer2 = TestDataBuilder.CreateCustomer(org2Id);
        context1.Customers.AddRange(customer1, customer2);

        var car1 = TestDataBuilder.CreateCar(customer1.Id, org1Id);
        var car2 = TestDataBuilder.CreateCar(customer2.Id, org2Id);
        context1.Cars.AddRange(car1, car2);
        await context1.SaveChangesAsync();

        var session1 = TestDataBuilder.CreateSession(customer1.Id, car1.Id, org1Id);
        var session2 = TestDataBuilder.CreateSession(customer2.Id, car2.Id, org2Id);
        context1.GarageSessions.AddRange(session1, session2);
        await context1.SaveChangesAsync();

        // Act
        var org1Sessions = await context1.GarageSessions.ToListAsync();
        var org2Sessions = await context2.GarageSessions.ToListAsync();

        // Assert
        org1Sessions.Should().ContainSingle()
            .Which.OrganizationId.Should().Be(org1Id);
        org2Sessions.Should().ContainSingle()
            .Which.OrganizationId.Should().Be(org2Id);
    }

    [Fact]
    public async Task JobCards_ShouldBeFilteredByOrganization_Automatically()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var context1 = CreateDbContext(org1Id);
        var context2 = CreateDbContext(org2Id);

        var customer1 = TestDataBuilder.CreateCustomer(org1Id);
        var customer2 = TestDataBuilder.CreateCustomer(org2Id);
        var car1 = TestDataBuilder.CreateCar(customer1.Id, org1Id);
        var car2 = TestDataBuilder.CreateCar(customer2.Id, org2Id);
        var session1 = TestDataBuilder.CreateSession(customer1.Id, car1.Id, org1Id);
        var session2 = TestDataBuilder.CreateSession(customer2.Id, car2.Id, org2Id);

        context1.Customers.AddRange(customer1, customer2);
        context1.Cars.AddRange(car1, car2);
        context1.GarageSessions.AddRange(session1, session2);
        await context1.SaveChangesAsync();

        var jobCard1 = TestDataBuilder.CreateJobCard(session1.Id, customer1.Id, car1.Id, org1Id, "JOB001");
        var jobCard2 = TestDataBuilder.CreateJobCard(session2.Id, customer2.Id, car2.Id, org2Id, "JOB002");
        context1.JobCards.AddRange(jobCard1, jobCard2);
        await context1.SaveChangesAsync();

        // Act
        var org1JobCards = await context1.JobCards.ToListAsync();
        var org2JobCards = await context2.JobCards.ToListAsync();

        // Assert
        org1JobCards.Should().ContainSingle()
            .Which.SessionId.Should().Be(session1.Id);
        org2JobCards.Should().ContainSingle()
            .Which.SessionId.Should().Be(session2.Id);
    }

    [Fact]
    public async Task Users_ShouldBeFilteredByOrganization_WhenOrganizationIdIsSet()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var context1 = CreateDbContext(org1Id);
        var context2 = CreateDbContext(org2Id);

        var user1 = TestDataBuilder.CreateUser("user1@org1.com", org1Id);
        var user2 = TestDataBuilder.CreateUser("user2@org2.com", org2Id);
        var adminUser = TestDataBuilder.CreateUser("admin@system.com", null, UserType.System);

        context1.Users.AddRange(user1, user2, adminUser);
        await context1.SaveChangesAsync();

        // Act
        var org1Users = await context1.Users.ToListAsync();
        var org2Users = await context2.Users.ToListAsync();

        // Assert
        org1Users.Should().ContainSingle()
            .Which.Email.Should().Be("user1@org1.com");
        org2Users.Should().ContainSingle()
            .Which.Email.Should().Be("user2@org2.com");
    }

    [Fact]
    public async Task IgnoreQueryFilters_ShouldReturnAllData_WhenUsed()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var context = CreateDbContext(org1Id);

        var customer1 = TestDataBuilder.CreateCustomer(org1Id, "Customer Org1");
        var customer2 = TestDataBuilder.CreateCustomer(org2Id, "Customer Org2");

        context.Customers.AddRange(customer1, customer2);
        await context.SaveChangesAsync();

        // Act
        var filteredCustomers = await context.Customers.ToListAsync();
        var allCustomers = await context.Customers.IgnoreQueryFilters().ToListAsync();

        // Assert
        filteredCustomers.Should().ContainSingle();
        allCustomers.Should().HaveCount(2);
    }

    [Fact]
    public async Task NavigationProperties_ShouldRespectQueryFilters()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var context1 = CreateDbContext(org1Id);

        var customer1 = TestDataBuilder.CreateCustomer(org1Id);
        var customer2 = TestDataBuilder.CreateCustomer(org2Id);
        context1.Customers.AddRange(customer1, customer2);

        var car1 = TestDataBuilder.CreateCar(customer1.Id, org1Id);
        var car2 = TestDataBuilder.CreateCar(customer2.Id, org2Id);
        context1.Cars.AddRange(car1, car2);
        await context1.SaveChangesAsync();

        // Act
        var customersWithCars = await context1.Customers
            .Include(c => c.Cars)
            .ToListAsync();

        // Assert
        customersWithCars.Should().ContainSingle();
        customersWithCars.First().Cars.Should().ContainSingle()
            .Which.OrganizationId.Should().Be(org1Id);
    }

    [Fact]
    public async Task OrganizationEntity_ShouldNotBeFiltered()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var context = CreateDbContext(org1Id);

        var org1 = new Organization { Id = org1Id, Name = "Org 1", CreatedAt = DateTime.UtcNow };
        var org2 = new Organization { Id = Guid.NewGuid(), Name = "Org 2", CreatedAt = DateTime.UtcNow };

        context.Organizations.AddRange(org1, org2);
        await context.SaveChangesAsync();

        // Act
        var organizations = await context.Organizations.ToListAsync();

        // Assert
        // Organizations should NOT be filtered - they're tenant-agnostic
        organizations.Should().HaveCount(2);
    }
}
