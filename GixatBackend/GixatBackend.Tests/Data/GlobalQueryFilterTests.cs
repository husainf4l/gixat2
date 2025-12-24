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
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        // Create customer for org1
        await using (var context1 = CreateDbContext(org1Id))
        {
            var customer1 = TestDataBuilder.CreateCustomer(org1Id, "Customer Org1");
            context1.Customers.Add(customer1);
            await context1.SaveChangesAsync();
        }

        // Create customer for org2
        await using (var context2 = CreateDbContext(org2Id))
        {
            var customer2 = TestDataBuilder.CreateCustomer(org2Id, "Customer Org2");
            context2.Customers.Add(customer2);
            await context2.SaveChangesAsync();
        }

        // Act
        await using (var context1 = CreateDbContext(org1Id))
        await using (var context2 = CreateDbContext(org2Id))
        {
            var org1Customers = await context1.Customers.ToListAsync();
            var org2Customers = await context2.Customers.ToListAsync();

            // Assert
            org1Customers.Should().ContainSingle()
                .Which.FirstName.Should().Be("Customer");
            org2Customers.Should().ContainSingle()
                .Which.FirstName.Should().Be("Customer");
        }
    }

    [Fact]
    public async Task Cars_ShouldBeFilteredByOrganization_Automatically()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        // Create data for org1
        await using (var context1 = CreateDbContext(org1Id))
        {
            var customer1 = TestDataBuilder.CreateCustomer(org1Id);
            context1.Customers.Add(customer1);
            await context1.SaveChangesAsync();

            var car1 = TestDataBuilder.CreateCar(customer1.Id, org1Id, "Toyota");
            context1.Cars.Add(car1);
            await context1.SaveChangesAsync();
        }

        // Create data for org2
        await using (var context2 = CreateDbContext(org2Id))
        {
            var customer2 = TestDataBuilder.CreateCustomer(org2Id);
            context2.Customers.Add(customer2);
            await context2.SaveChangesAsync();

            var car2 = TestDataBuilder.CreateCar(customer2.Id, org2Id, "Honda");
            context2.Cars.Add(car2);
            await context2.SaveChangesAsync();
        }

        // Act & Assert
        await using (var context1 = CreateDbContext(org1Id))
        await using (var context2 = CreateDbContext(org2Id))
        {
            var org1Cars = await context1.Cars.ToListAsync();
            var org2Cars = await context2.Cars.ToListAsync();

            org1Cars.Should().ContainSingle()
                .Which.Make.Should().Be("Toyota");
            org2Cars.Should().ContainSingle()
                .Which.Make.Should().Be("Honda");
        }
    }

    [Fact]
    public async Task Sessions_ShouldBeFilteredByOrganization_Automatically()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        // Create data for org1
        await using (var context1 = CreateDbContext(org1Id))
        {
            var customer1 = TestDataBuilder.CreateCustomer(org1Id);
            context1.Customers.Add(customer1);
            var car1 = TestDataBuilder.CreateCar(customer1.Id, org1Id);
            context1.Cars.Add(car1);
            await context1.SaveChangesAsync();

            var session1 = TestDataBuilder.CreateSession(customer1.Id, car1.Id, org1Id);
            context1.GarageSessions.Add(session1);
            await context1.SaveChangesAsync();
        }

        // Create data for org2
        await using (var context2 = CreateDbContext(org2Id))
        {
            var customer2 = TestDataBuilder.CreateCustomer(org2Id);
            context2.Customers.Add(customer2);
            var car2 = TestDataBuilder.CreateCar(customer2.Id, org2Id);
            context2.Cars.Add(car2);
            await context2.SaveChangesAsync();

            var session2 = TestDataBuilder.CreateSession(customer2.Id, car2.Id, org2Id);
            context2.GarageSessions.Add(session2);
            await context2.SaveChangesAsync();
        }

        // Act & Assert
        await using (var context1 = CreateDbContext(org1Id))
        await using (var context2 = CreateDbContext(org2Id))
        {
            var org1Sessions = await context1.GarageSessions.ToListAsync();
            var org2Sessions = await context2.GarageSessions.ToListAsync();

            org1Sessions.Should().ContainSingle()
                .Which.OrganizationId.Should().Be(org1Id);
            org2Sessions.Should().ContainSingle()
                .Which.OrganizationId.Should().Be(org2Id);
        }
    }

    [Fact]
    public async Task JobCards_ShouldBeFilteredByOrganization_Automatically()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        Guid session1Id, session2Id;

        // Create data for org1
        await using (var context1 = CreateDbContext(org1Id))
        {
            var customer1 = TestDataBuilder.CreateCustomer(org1Id);
            var car1 = TestDataBuilder.CreateCar(customer1.Id, org1Id);
            var session1 = TestDataBuilder.CreateSession(customer1.Id, car1.Id, org1Id);
            session1Id = session1.Id;

            context1.Customers.Add(customer1);
            context1.Cars.Add(car1);
            context1.GarageSessions.Add(session1);
            await context1.SaveChangesAsync();

            var jobCard1 = TestDataBuilder.CreateJobCard(session1.Id, customer1.Id, car1.Id, org1Id, "JOB001");
            context1.JobCards.Add(jobCard1);
            await context1.SaveChangesAsync();
        }

        // Create data for org2
        await using (var context2 = CreateDbContext(org2Id))
        {
            var customer2 = TestDataBuilder.CreateCustomer(org2Id);
            var car2 = TestDataBuilder.CreateCar(customer2.Id, org2Id);
            var session2 = TestDataBuilder.CreateSession(customer2.Id, car2.Id, org2Id);
            session2Id = session2.Id;

            context2.Customers.Add(customer2);
            context2.Cars.Add(car2);
            context2.GarageSessions.Add(session2);
            await context2.SaveChangesAsync();

            var jobCard2 = TestDataBuilder.CreateJobCard(session2.Id, customer2.Id, car2.Id, org2Id, "JOB002");
            context2.JobCards.Add(jobCard2);
            await context2.SaveChangesAsync();
        }

        // Act & Assert
        await using (var context1 = CreateDbContext(org1Id))
        await using (var context2 = CreateDbContext(org2Id))
        {
            var org1JobCards = await context1.JobCards.ToListAsync();
            var org2JobCards = await context2.JobCards.ToListAsync();

            org1JobCards.Should().ContainSingle()
                .Which.SessionId.Should().Be(session1Id);
            org2JobCards.Should().ContainSingle()
                .Which.SessionId.Should().Be(session2Id);
        }
    }

    [Fact]
    public async Task Users_ShouldBeFilteredByOrganization_WhenOrganizationIdIsSet()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

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
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        // Create customer for org1
        await using (var context1 = CreateDbContext(org1Id))
        {
            var customer1 = TestDataBuilder.CreateCustomer(org1Id, "Customer Org1");
            context1.Customers.Add(customer1);
            await context1.SaveChangesAsync();
        }

        // Create customer for org2
        await using (var context2 = CreateDbContext(org2Id))
        {
            var customer2 = TestDataBuilder.CreateCustomer(org2Id, "Customer Org2");
            context2.Customers.Add(customer2);
            await context2.SaveChangesAsync();
        }

        // Act & Assert
        await using (var context = CreateDbContext(org1Id))
        {
            var filteredCustomers = await context.Customers.ToListAsync();
            var allCustomers = await context.Customers.IgnoreQueryFilters().ToListAsync();

            filteredCustomers.Should().ContainSingle();
            allCustomers.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task NavigationProperties_ShouldRespectQueryFilters()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        // Create data for org1
        await using (var context1 = CreateDbContext(org1Id))
        {
            var customer1 = TestDataBuilder.CreateCustomer(org1Id);
            context1.Customers.Add(customer1);
            await context1.SaveChangesAsync();

            var car1 = TestDataBuilder.CreateCar(customer1.Id, org1Id);
            context1.Cars.Add(car1);
            await context1.SaveChangesAsync();
        }

        // Create data for org2
        await using (var context2 = CreateDbContext(org2Id))
        {
            var customer2 = TestDataBuilder.CreateCustomer(org2Id);
            context2.Customers.Add(customer2);
            await context2.SaveChangesAsync();

            var car2 = TestDataBuilder.CreateCar(customer2.Id, org2Id);
            context2.Cars.Add(car2);
            await context2.SaveChangesAsync();
        }

        // Act & Assert
        await using (var context1 = CreateDbContext(org1Id))
        {
            var customersWithCars = await context1.Customers
                .Include(c => c.Cars)
                .ToListAsync();

            customersWithCars.Should().ContainSingle();
            customersWithCars.First().Cars.Should().ContainSingle()
                .Which.OrganizationId.Should().Be(org1Id);
        }
    }

    [Fact]
    public async Task OrganizationEntity_ShouldNotBeFiltered()
    {
        // Arrange
        await CleanDatabaseAsync();
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
