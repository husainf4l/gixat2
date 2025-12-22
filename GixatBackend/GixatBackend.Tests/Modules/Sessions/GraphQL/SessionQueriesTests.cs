using FluentAssertions;
using GixatBackend.Modules.Sessions.GraphQL;
using GixatBackend.Modules.Sessions.Enums;
using GixatBackend.Tests.Helpers;
using GixatBackend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Tests.Modules.Sessions.GraphQL;

public class SessionQueriesTests : MultiTenancyTestBase
{
    [Fact]
    public async Task GetSessions_ShouldReturnOnlyOrganizationSessions()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var contextOrg1 = TestDbContextFactory.CreateInMemoryContextWithTenant(org1Id);
        var contextOrg2 = TestDbContextFactory.CreateInMemoryContextWithTenant(org2Id);

        // Setup Org1 data
        var customer1 = CreateTestCustomer(org1Id, "Test Customer");
        contextOrg1.Customers.Add(customer1);
        await contextOrg1.SaveChangesAsync();

        var car1 = CreateTestCar(customer1.Id, org1Id, "ABC123");
        contextOrg1.Cars.Add(car1);
        await contextOrg1.SaveChangesAsync();

        var session1 = TestDataBuilder.CreateSession(customer1.Id, car1.Id, org1Id);
        var session2 = TestDataBuilder.CreateSession(customer1.Id, car1.Id, org1Id);
        contextOrg1.GarageSessions.AddRange(session1, session2);
        await contextOrg1.SaveChangesAsync();

        // Setup Org2 data
        var customer2 = CreateTestCustomer(org2Id, "Test Customer");
        contextOrg2.Customers.Add(customer2);
        await contextOrg2.SaveChangesAsync();

        var car2 = CreateTestCar(customer2.Id, org2Id, "DEF456");
        contextOrg2.Cars.Add(car2);
        await contextOrg2.SaveChangesAsync();

        var session3 = TestDataBuilder.CreateSession(customer2.Id, car2.Id, org2Id);
        contextOrg2.GarageSessions.Add(session3);
        await contextOrg2.SaveChangesAsync();

        // Act
        var org1Sessions = await SessionQueries.GetSessions(contextOrg1).ToListAsync();
        var org2Sessions = await SessionQueries.GetSessions(contextOrg2).ToListAsync();

        // Assert
        org1Sessions.Should().HaveCount(2);
        org2Sessions.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSessionById_ShouldReturnNull_WhenSessionBelongsToAnotherOrganization()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var contextOrg1 = TestDbContextFactory.CreateInMemoryContextWithTenant(org1Id);
        var contextOrg2 = TestDbContextFactory.CreateInMemoryContextWithTenant(org2Id);

        var customer = CreateTestCustomer(org1Id, "Test Customer");
        contextOrg1.Customers.Add(customer);
        await contextOrg1.SaveChangesAsync();

        var car = CreateTestCar(customer.Id, org1Id, "ABC123");
        contextOrg1.Cars.Add(car);
        await contextOrg1.SaveChangesAsync();

        var session = TestDataBuilder.CreateSession(customer.Id, car.Id, org1Id);
        contextOrg1.GarageSessions.Add(session);
        await contextOrg1.SaveChangesAsync();

        // Act
        var resultOrg1 = await SessionQueries.GetSessionByIdAsync(session.Id, contextOrg1);
        var resultOrg2 = await SessionQueries.GetSessionByIdAsync(session.Id, contextOrg2);

        // Assert
        resultOrg1.Should().NotBeNull();
        resultOrg2.Should().BeNull("Session belongs to another organization");
    }
}
