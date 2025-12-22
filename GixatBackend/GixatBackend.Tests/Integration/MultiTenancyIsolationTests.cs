using FluentAssertions;
using GixatBackend.Data;
using GixatBackend.Tests.Helpers;
using GixatBackend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Tests.Integration;

/// <summary>
/// Integration tests to verify multi-tenancy isolation across all entities
/// </summary>
public class MultiTenancyIsolationTests : IntegrationTestBase
{
    [Fact]
    public async Task CompleteWorkflow_ShouldIsolateDataBetweenOrganizations()
    {
        // Arrange - Clean database first
        await CleanDatabaseAsync();
        
        // Create organizations
        await using var contextNoTenant = CreateDbContext(null);
        var org1 = CreateTestOrganization("Organization One");
        var org2 = CreateTestOrganization("Organization Two");
        contextNoTenant.Organizations.AddRange(org1, org2);
        await contextNoTenant.SaveChangesAsync();

        var org1Id = org1.Id;
        var org2Id = org2.Id;

        // Create complete data for Org1
        await using (var contextOrg1 = CreateContextForOrganization(org1Id))
        {
            var customer1 = CreateTestCustomer(org1Id, "Org1 Customer");
            contextOrg1.Customers.Add(customer1);
            await contextOrg1.SaveChangesAsync();

            var car1 = CreateTestCar(customer1.Id, org1Id, "ORG1-CAR");
            contextOrg1.Cars.Add(car1);
            await contextOrg1.SaveChangesAsync();

            var session1 = CreateTestSession(customer1.Id, car1.Id, org1Id);
            contextOrg1.GarageSessions.Add(session1);
            await contextOrg1.SaveChangesAsync();

            var jobCard1 = CreateTestJobCard(session1.Id, customer1.Id, car1.Id, org1Id);
            contextOrg1.JobCards.Add(jobCard1);
            await contextOrg1.SaveChangesAsync();
        }

        // Create complete data for Org2
        await using (var contextOrg2 = CreateContextForOrganization(org2Id))
        {
            var customer2 = CreateTestCustomer(org2Id, "Org2 Customer");
            contextOrg2.Customers.Add(customer2);
            await contextOrg2.SaveChangesAsync();

            var car2 = CreateTestCar(customer2.Id, org2Id, "ORG2-CAR");
            contextOrg2.Cars.Add(car2);
            await contextOrg2.SaveChangesAsync();

            var session2 = CreateTestSession(customer2.Id, car2.Id, org2Id);
            contextOrg2.GarageSessions.Add(session2);
            await contextOrg2.SaveChangesAsync();

            var jobCard2 = CreateTestJobCard(session2.Id, customer2.Id, car2.Id, org2Id);
            contextOrg2.JobCards.Add(jobCard2);
            await contextOrg2.SaveChangesAsync();
        }

        // Act & Assert - Verify complete isolation
        await using (var contextOrg1 = CreateContextForOrganization(org1Id))
        {
            var org1Customers = await contextOrg1.Customers.ToListAsync();
            org1Customers.Should().HaveCount(1);
            org1Customers[0].FirstName.Should().Be("Org1");

            var org1Cars = await contextOrg1.Cars.ToListAsync();
            org1Cars.Should().HaveCount(1);
            org1Cars[0].LicensePlate.Should().Be("ORG1-CAR");

            var org1Sessions = await contextOrg1.GarageSessions.ToListAsync();
            org1Sessions.Should().HaveCount(1);

            var org1JobCards = await contextOrg1.JobCards.ToListAsync();
            org1JobCards.Should().HaveCount(1);
        }

        await using (var contextOrg2 = CreateContextForOrganization(org2Id))
        {
            var org2Customers = await contextOrg2.Customers.ToListAsync();
            org2Customers.Should().HaveCount(1);
            org2Customers[0].FirstName.Should().Be("Org2");

            var org2Cars = await contextOrg2.Cars.ToListAsync();
            org2Cars.Should().HaveCount(1);
            org2Cars[0].LicensePlate.Should().Be("ORG2-CAR");

            var org2Sessions = await contextOrg2.GarageSessions.ToListAsync();
            org2Sessions.Should().HaveCount(1);

            var org2JobCards = await contextOrg2.JobCards.ToListAsync();
            org2JobCards.Should().HaveCount(1);
        }

        // Verify AssertTenantIsolation helper works
        await AssertTenantIsolationAsync(org1Id, org2Id);
    }

    [Fact]
    public async Task CrossOrganizationQuery_ShouldAlwaysReturnEmpty()
    {
        // Arrange - Clean database
        await CleanDatabaseAsync();
        
        await using var contextNoTenant = CreateDbContext(null);
        var org1 = CreateTestOrganization("Org One");
        var org2 = CreateTestOrganization("Org Two");
        contextNoTenant.Organizations.AddRange(org1, org2);
        await contextNoTenant.SaveChangesAsync();

        Guid customerId, carId;
        
        // Add data to org1
        await using (var contextOrg1 = CreateContextForOrganization(org1.Id))
        {
            var customer = CreateTestCustomer(org1.Id, "Test Customer");
            contextOrg1.Customers.Add(customer);
            await contextOrg1.SaveChangesAsync();
            customerId = customer.Id;

            var car = CreateTestCar(customer.Id, org1.Id, "TEST-123");
            contextOrg1.Cars.Add(car);
            await contextOrg1.SaveChangesAsync();
            carId = car.Id;
        }

        // Act - Try to query org1's data from org2 context using explicit IDs
        await using (var contextOrg2 = CreateContextForOrganization(org2.Id))
        {
            var customerFromOrg2 = await contextOrg2.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId);
            var carFromOrg2 = await contextOrg2.Cars
                .FirstOrDefaultAsync(c => c.Id == carId);

            // Assert
            customerFromOrg2.Should().BeNull("Global query filter should block cross-org access");
            carFromOrg2.Should().BeNull("Global query filter should block cross-org access");
        }
    }

    [Fact]
    public async Task GlobalQueryFilter_ShouldApplyToAllTenantEntities()
    {
        // Arrange - Clean database
        await CleanDatabaseAsync();
        
        await using var contextNoTenant = CreateDbContext(null);
        var org1 = CreateTestOrganization("Org One");
        var org2 = CreateTestOrganization("Org Two");
        contextNoTenant.Organizations.AddRange(org1, org2);
        await contextNoTenant.SaveChangesAsync();

        // Create users in both orgs
        await using (var contextOrg1 = CreateContextForOrganization(org1.Id))
        {
            var user1 = CreateTestUser(org1.Id, "user1@org1.com");
            contextOrg1.Users.Add(user1);
            await contextOrg1.SaveChangesAsync();
        }

        await using (var contextOrg2 = CreateContextForOrganization(org2.Id))
        {
            var user2 = CreateTestUser(org2.Id, "user2@org2.com");
            contextOrg2.Users.Add(user2);
            await contextOrg2.SaveChangesAsync();
        }

        // Act & Assert
        await using (var contextOrg1 = CreateContextForOrganization(org1.Id))
        {
            var org1Users = await contextOrg1.Users.ToListAsync();
            org1Users.Should().HaveCount(1);
            org1Users[0].Email.Should().Be("user1@org1.com");
        }

        await using (var contextOrg2 = CreateContextForOrganization(org2.Id))
        {
            var org2Users = await contextOrg2.Users.ToListAsync();
            org2Users.Should().HaveCount(1);
            org2Users[0].Email.Should().Be("user2@org2.com");
        }
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldAutoAssignOrganizationId()
    {
        // Arrange - Clean database
        await CleanDatabaseAsync();
        
        await using var contextNoTenant = CreateDbContext(null);
        var org = CreateTestOrganization("Test Org");
        contextNoTenant.Organizations.Add(org);
        await contextNoTenant.SaveChangesAsync();

        // Act - Create customer without explicitly setting OrganizationId
        await using var context = CreateContextForOrganization(org.Id);
        var customer = new GixatBackend.Modules.Customers.Models.Customer
        {
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+1234567890",
            Email = "test@example.com"
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Assert
        customer.OrganizationId.Should().Be(org.Id, "SaveChangesAsync should auto-assign OrganizationId");
    }

    [Fact]
    public async Task MultipleEntities_ShouldAllHaveSameOrganizationId()
    {
        // Arrange - Clean database
        await CleanDatabaseAsync();
        
        await using var contextNoTenant = CreateDbContext(null);
        var org = CreateTestOrganization("Test Org");
        contextNoTenant.Organizations.Add(org);
        await contextNoTenant.SaveChangesAsync();

        // Act
        await using var context = CreateContextForOrganization(org.Id);
        var customer = CreateTestCustomer(org.Id, "Test Customer");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var car = CreateTestCar(customer.Id, org.Id, "ABC-123");
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        var session = CreateTestSession(customer.Id, car.Id, org.Id);
        context.GarageSessions.Add(session);
        await context.SaveChangesAsync();

        var jobCard = CreateTestJobCard(session.Id, customer.Id, car.Id, org.Id);
        context.JobCards.Add(jobCard);
        await context.SaveChangesAsync();

        // Assert - All entities should have the same organization ID
        customer.OrganizationId.Should().Be(org.Id);
        car.OrganizationId.Should().Be(org.Id);
        session.OrganizationId.Should().Be(org.Id);
        jobCard.OrganizationId.Should().Be(org.Id);
    }
}
