using FluentAssertions;
using GixatBackend.Modules.Sessions.GraphQL;
using GixatBackend.Modules.Sessions.Enums;
using GixatBackend.Modules.Sessions.Services;
using GixatBackend.Tests.Helpers;
using GixatBackend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GixatBackend.Tests.Modules.Sessions.GraphQL;

public class SessionMutationsMultiTenancyTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateSessionAsync_ShouldAssignOrganizationId_Automatically()
    {
        // Arrange
        await CleanDatabaseAsync();
        var orgId = Guid.NewGuid();
        
        // Create organization first (required for foreign key constraint)
        await CreateOrganizationsAsync(orgId);
        
        var context = CreateDbContext(orgId);
        var sessionServiceMock = new Mock<ISessionService>();
        sessionServiceMock.Setup(x => x.ValidateNoActiveSession(It.IsAny<GixatBackend.Modules.Sessions.Models.GarageSession?>()));

        var customer = CreateTestCustomer(orgId, "Test Customer");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var car = CreateTestCar(customer.Id, orgId, "ABC123");
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        // Act
        var result = await SessionMutations.CreateSessionAsync(
            car.Id,
            customer.Id,
            context,
            sessionServiceMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.OrganizationId.Should().Be(orgId);
        result.CustomerId.Should().Be(customer.Id);
        result.CarId.Should().Be(car.Id);
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldThrow_WhenCustomerBelongsToDifferentOrganization()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        var context1 = CreateDbContext(org1Id);
        var context2 = CreateDbContext(org2Id);
        var sessionServiceMock = new Mock<ISessionService>();
        sessionServiceMock.Setup(x => x.ValidateNoActiveSession(It.IsAny<GixatBackend.Modules.Sessions.Models.GarageSession?>()));

        // Create customer in org1
        var customer = CreateTestCustomer(org1Id, "Test Customer");
        context1.Customers.Add(customer);
        await context1.SaveChangesAsync();

        var car = CreateTestCar(customer.Id, org1Id, "ABC123");
        context1.Cars.Add(car);
        await context1.SaveChangesAsync();

        // Act & Assert - Try to create session in org2 using org1's customer
        await Assert.ThrowsAnyAsync<Exception>(() =>
            SessionMutations.CreateSessionAsync(car.Id, customer.Id, context2, sessionServiceMock.Object));
    }

    [Fact]
    public async Task UpdateSessionStatusAsync_ShouldOnlyUpdateOrganizationSession()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        var contextOrg1 = CreateDbContext(org1Id);
        var contextOrg2 = CreateDbContext(org2Id);
        var sessionServiceMock = new Mock<ISessionService>();

        var customer = CreateTestCustomer(org1Id, "Test Customer");
        contextOrg1.Customers.Add(customer);
        await contextOrg1.SaveChangesAsync();

        var car = CreateTestCar(customer.Id, org1Id, "ABC123");
        contextOrg1.Cars.Add(car);
        await contextOrg1.SaveChangesAsync();

        var session = TestDataBuilder.CreateSession(customer.Id, car.Id, org1Id);
        contextOrg1.GarageSessions.Add(session);
        await contextOrg1.SaveChangesAsync();

        // Act & Assert - Try to update from org2 context
        await Assert.ThrowsAnyAsync<Exception>(() =>
            SessionMutations.UpdateSessionStatusAsync(session.Id, SessionStatus.Inspection, null, contextOrg2, sessionServiceMock.Object));
    }

    [Fact]
    public async Task UpdateCustomerRequestsAsync_ShouldEnforceMultiTenancy()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        var contextOrg1 = CreateDbContext(org1Id);
        var contextOrg2 = CreateDbContext(org2Id);

        var customer = CreateTestCustomer(org1Id, "Test Customer");
        contextOrg1.Customers.Add(customer);
        await contextOrg1.SaveChangesAsync();

        var car = CreateTestCar(customer.Id, org1Id, "ABC123");
        contextOrg1.Cars.Add(car);
        await contextOrg1.SaveChangesAsync();

        var session = TestDataBuilder.CreateSession(customer.Id, car.Id, org1Id);
        contextOrg1.GarageSessions.Add(session);
        await contextOrg1.SaveChangesAsync();
        var sessionId = session.Id;

        // Act - Try to update from org2 (should fail because session not found due to tenant filter)
        var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
            SessionMutations.UpdateCustomerRequestsAsync(sessionId, "Unauthorized", contextOrg2));

        // Assert
        exception.Message.Should().Contain("not found"); // Verify it's filtered by tenant, not a different error
    }
}
