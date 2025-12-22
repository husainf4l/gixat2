using FluentAssertions;
using GixatBackend.Modules.JobCards.GraphQL;
using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Modules.JobCards.Services;
using GixatBackend.Modules.Sessions.Enums;
using GixatBackend.Tests.Helpers;
using GixatBackend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GixatBackend.Tests.Modules.JobCards.GraphQL;

public class JobCardMutationsMultiTenancyTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateJobCardFromSessionAsync_ShouldAssignOrganizationId_Automatically()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var context = CreateDbContext(orgId);
        var jobCardServiceMock = new Mock<IJobCardService>();

        var customer = CreateTestCustomer(orgId, "Test Customer");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var car = CreateTestCar(customer.Id, orgId, "ABC123");
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        var session = TestDataBuilder.CreateSession(customer.Id, car.Id, orgId, SessionStatus.ReportGenerated);
        session.CustomerRequests = "Oil change, brake check";
        context.GarageSessions.Add(session);
        await context.SaveChangesAsync();

        jobCardServiceMock.Setup(x => x.ValidateSessionForJobCard(It.IsAny<GixatBackend.Modules.Sessions.Models.GarageSession>()));
        jobCardServiceMock.Setup(x => x.BuildInternalNotesFromSession(It.IsAny<GixatBackend.Modules.Sessions.Models.GarageSession>()))
            .Returns("Notes");
        jobCardServiceMock.Setup(x => x.ExtractJobItemsFromSession(It.IsAny<GixatBackend.Modules.Sessions.Models.GarageSession>()))
            .Returns(new List<JobItem>());

        // Act
        var result = await JobCardMutations.CreateJobCardFromSessionAsync(
            session.Id, context, jobCardServiceMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.OrganizationId.Should().Be(orgId);
        result.CustomerId.Should().Be(customer.Id);
        result.CarId.Should().Be(car.Id);
    }

    [Fact]
    public async Task CreateJobCardFromSessionAsync_ShouldThrow_WhenSessionBelongsToDifferentOrganization()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var context1 = CreateDbContext(org1Id);
        var context2 = CreateDbContext(org2Id);
        var jobCardServiceMock = new Mock<IJobCardService>();

        var customer = CreateTestCustomer(org1Id, "Test Customer");
        context1.Customers.Add(customer);
        await context1.SaveChangesAsync();

        var car = CreateTestCar(customer.Id, org1Id, "ABC123");
        context1.Cars.Add(car);
        await context1.SaveChangesAsync();

        var session = TestDataBuilder.CreateSession(customer.Id, car.Id, org1Id);
        context1.GarageSessions.Add(session);
        await context1.SaveChangesAsync();

        // Act & Assert - Try from org2
        await Assert.ThrowsAnyAsync<Exception>(() =>
            JobCardMutations.CreateJobCardFromSessionAsync(session.Id, context2, jobCardServiceMock.Object));
    }

    [Fact]
    public async Task AddJobItemAsync_ShouldEnforceMultiTenancy()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

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

        var jobCard = TestDataBuilder.CreateJobCard(session.Id, customer.Id, car.Id, org1Id);
        contextOrg1.JobCards.Add(jobCard);
        await contextOrg1.SaveChangesAsync();

        // Act
        var resultOrg1 = await JobCardMutations.AddJobItemAsync(
            jobCard.Id, "Oil change", 50m, 30m, null, contextOrg1);

        // Try from org2
        await Assert.ThrowsAnyAsync<Exception>(() =>
            JobCardMutations.AddJobItemAsync(jobCard.Id, "Brake check", 100m, 50m, null, contextOrg2));

        // Assert
        resultOrg1.Should().NotBeNull();
        resultOrg1.Items.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task UpdateJobCardStatusAsync_ShouldOnlyUpdateOrganizationJobCard()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

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

        var jobCard = TestDataBuilder.CreateJobCard(session.Id, customer.Id, car.Id, org1Id);
        contextOrg1.JobCards.Add(jobCard);
        await contextOrg1.SaveChangesAsync();

        // Act & Assert
        var resultOrg1 = await JobCardMutations.UpdateJobCardStatusAsync(
            jobCard.Id, JobCardStatus.InProgress, contextOrg1);
        resultOrg1.Should().NotBeNull();
        resultOrg1.Status.Should().Be(JobCardStatus.InProgress);

        // Try from org2
        await Assert.ThrowsAnyAsync<Exception>(() =>
            JobCardMutations.UpdateJobCardStatusAsync(jobCard.Id, JobCardStatus.Completed, contextOrg2));
    }

    [Fact]
    public async Task ApproveJobCardAsync_ShouldEnforceMultiTenancy()
    {
        // Arrange
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

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

        var jobCard = TestDataBuilder.CreateJobCard(session.Id, customer.Id, car.Id, org1Id);
        contextOrg1.JobCards.Add(jobCard);
        await contextOrg1.SaveChangesAsync();

        // Act
        var resultOrg1 = await JobCardMutations.ApproveJobCardAsync(jobCard.Id, contextOrg1);
        resultOrg1.Should().NotBeNull();
        resultOrg1.IsApprovedByCustomer.Should().BeTrue();

        // Try from org2
        await Assert.ThrowsAnyAsync<Exception>(() =>
            JobCardMutations.ApproveJobCardAsync(jobCard.Id, contextOrg2));
    }
}
