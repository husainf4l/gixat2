using FluentAssertions;
using GixatBackend.Modules.JobCards.GraphQL;
using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Tests.Helpers;
using GixatBackend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Tests.Modules.JobCards.GraphQL;

public class JobCardQueriesTests : IntegrationTestBase
{
    [Fact]
    public async Task GetJobCards_ShouldReturnOnlyOrganizationJobCards()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        var contextOrg1 = CreateDbContext(org1Id);
        var contextOrg2 = CreateDbContext(org2Id);

        // Setup Org1 data
        var customer1 = CreateTestCustomer(org1Id, "Test Customer");
        contextOrg1.Customers.Add(customer1);
        await contextOrg1.SaveChangesAsync();

        var car1 = CreateTestCar(customer1.Id, org1Id, "ABC123");
        contextOrg1.Cars.Add(car1);
        await contextOrg1.SaveChangesAsync();

        var session1 = TestDataBuilder.CreateSession(customer1.Id, car1.Id, org1Id);
        contextOrg1.GarageSessions.Add(session1);
        await contextOrg1.SaveChangesAsync();

        var jobCard1 = TestDataBuilder.CreateJobCard(session1.Id, customer1.Id, car1.Id, org1Id);
        var jobCard2 = TestDataBuilder.CreateJobCard(session1.Id, customer1.Id, car1.Id, org1Id);
        contextOrg1.JobCards.AddRange(jobCard1, jobCard2);
        await contextOrg1.SaveChangesAsync();

        // Setup Org2 data
        var customer2 = CreateTestCustomer(org2Id, "Test Customer");
        contextOrg2.Customers.Add(customer2);
        await contextOrg2.SaveChangesAsync();

        var car2 = CreateTestCar(customer2.Id, org2Id, "DEF456");
        contextOrg2.Cars.Add(car2);
        await contextOrg2.SaveChangesAsync();

        var session2 = TestDataBuilder.CreateSession(customer2.Id, car2.Id, org2Id);
        contextOrg2.GarageSessions.Add(session2);
        await contextOrg2.SaveChangesAsync();

        var jobCard3 = TestDataBuilder.CreateJobCard(session2.Id, customer2.Id, car2.Id, org2Id);
        contextOrg2.JobCards.Add(jobCard3);
        await contextOrg2.SaveChangesAsync();

        // Act
        var org1JobCards = await JobCardQueries.GetJobCards(contextOrg1).ToListAsync();
        var org2JobCards = await JobCardQueries.GetJobCards(contextOrg2).ToListAsync();

        // Assert
        org1JobCards.Should().HaveCount(2);
        org2JobCards.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetJobCardById_ShouldReturnNull_WhenJobCardBelongsToAnotherOrganization()
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

        var jobCard = TestDataBuilder.CreateJobCard(session.Id, customer.Id, car.Id, org1Id);
        contextOrg1.JobCards.Add(jobCard);
        await contextOrg1.SaveChangesAsync();

        // Act
        var resultOrg1 = await JobCardQueries.GetJobCardByIdAsync(jobCard.Id, contextOrg1);
        var resultOrg2 = await JobCardQueries.GetJobCardByIdAsync(jobCard.Id, contextOrg2);

        // Assert
        resultOrg1.Should().NotBeNull();
        resultOrg2.Should().BeNull("JobCard belongs to another organization");
    }

    [Fact]
    public async Task SearchJobCards_ShouldFilterByOrganization()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        var contextOrg1 = CreateDbContext(org1Id);
        var contextOrg2 = CreateDbContext(org2Id);

        // Org1 setup
        var customer1 = CreateTestCustomer(org1Id, "John Doe");
        contextOrg1.Customers.Add(customer1);
        await contextOrg1.SaveChangesAsync();

        var car1 = CreateTestCar(customer1.Id, org1Id, "ABC123");
        contextOrg1.Cars.Add(car1);
        await contextOrg1.SaveChangesAsync();

        var session1 = TestDataBuilder.CreateSession(customer1.Id, car1.Id, org1Id);
        contextOrg1.GarageSessions.Add(session1);
        await contextOrg1.SaveChangesAsync();

        var jobCard1 = TestDataBuilder.CreateJobCard(session1.Id, customer1.Id, car1.Id, org1Id);
        contextOrg1.JobCards.Add(jobCard1);
        await contextOrg1.SaveChangesAsync();

        // Org2 setup
        var customer2 = CreateTestCustomer(org2Id, "John Smith");
        contextOrg2.Customers.Add(customer2);
        await contextOrg2.SaveChangesAsync();

        var car2 = CreateTestCar(customer2.Id, org2Id, "XYZ789");
        contextOrg2.Cars.Add(car2);
        await contextOrg2.SaveChangesAsync();

        var session2 = TestDataBuilder.CreateSession(customer2.Id, car2.Id, org2Id);
        contextOrg2.GarageSessions.Add(session2);
        await contextOrg2.SaveChangesAsync();

        var jobCard2 = TestDataBuilder.CreateJobCard(session2.Id, customer2.Id, car2.Id, org2Id);
        contextOrg2.JobCards.Add(jobCard2);
        await contextOrg2.SaveChangesAsync();

        // Act
        var org1Results = await JobCardQueries.SearchJobCards("John", null, contextOrg1).ToListAsync();
        var org2Results = await JobCardQueries.SearchJobCards("John", null, contextOrg2).ToListAsync();

        // Assert
        org1Results.Should().HaveCount(1);
        org2Results.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetJobCardsByCustomer_ShouldFilterByOrganization()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        var contextOrg1 = CreateDbContext(org1Id);
        var contextOrg2 = CreateDbContext(org2Id);

        var customer1 = CreateTestCustomer(org1Id, "Test Customer");
        contextOrg1.Customers.Add(customer1);
        await contextOrg1.SaveChangesAsync();

        var car1 = CreateTestCar(customer1.Id, org1Id, "ABC123");
        contextOrg1.Cars.Add(car1);
        await contextOrg1.SaveChangesAsync();

        var session1 = TestDataBuilder.CreateSession(customer1.Id, car1.Id, org1Id);
        contextOrg1.GarageSessions.Add(session1);
        await contextOrg1.SaveChangesAsync();

        var jobCard = TestDataBuilder.CreateJobCard(session1.Id, customer1.Id, car1.Id, org1Id);
        contextOrg1.JobCards.Add(jobCard);
        await contextOrg1.SaveChangesAsync();

        // Act
        var org1Results = await JobCardQueries.GetJobCardsByCustomer(customer1.Id, contextOrg1).ToListAsync();
        var org2Results = await JobCardQueries.GetJobCardsByCustomer(customer1.Id, contextOrg2).ToListAsync();

        // Assert
        org1Results.Should().HaveCount(1);
        org2Results.Should().HaveCount(0, "Customer belongs to another organization");
    }

    [Fact]
    public async Task GetJobCardsByStatus_ShouldReturnOnlyOrganizationJobCards()
    {
        // Arrange
        await CleanDatabaseAsync();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        // Create organizations first (required for foreign key constraint)
        await CreateOrganizationsAsync(org1Id, org2Id);

        var contextOrg1 = CreateDbContext(org1Id);
        var contextOrg2 = CreateDbContext(org2Id);

        // Setup Org1
        var customer1 = CreateTestCustomer(org1Id, "Test Customer");
        contextOrg1.Customers.Add(customer1);
        await contextOrg1.SaveChangesAsync();

        var car1 = CreateTestCar(customer1.Id, org1Id, "ABC123");
        contextOrg1.Cars.Add(car1);
        await contextOrg1.SaveChangesAsync();

        var session1 = TestDataBuilder.CreateSession(customer1.Id, car1.Id, org1Id);
        contextOrg1.GarageSessions.Add(session1);
        await contextOrg1.SaveChangesAsync();

        var jobCard1 = TestDataBuilder.CreateJobCard(session1.Id, customer1.Id, car1.Id, org1Id);
        jobCard1.Status = JobCardStatus.Pending;
        contextOrg1.JobCards.Add(jobCard1);
        await contextOrg1.SaveChangesAsync();

        // Setup Org2
        var customer2 = CreateTestCustomer(org2Id, "Test Customer");
        contextOrg2.Customers.Add(customer2);
        await contextOrg2.SaveChangesAsync();

        var car2 = CreateTestCar(customer2.Id, org2Id, "DEF456");
        contextOrg2.Cars.Add(car2);
        await contextOrg2.SaveChangesAsync();

        var session2 = TestDataBuilder.CreateSession(customer2.Id, car2.Id, org2Id);
        contextOrg2.GarageSessions.Add(session2);
        await contextOrg2.SaveChangesAsync();

        var jobCard2 = TestDataBuilder.CreateJobCard(session2.Id, customer2.Id, car2.Id, org2Id);
        jobCard2.Status = JobCardStatus.Pending;
        contextOrg2.JobCards.Add(jobCard2);
        await contextOrg2.SaveChangesAsync();

        // Act
        var org1Results = await JobCardQueries.GetJobCardsByStatus(JobCardStatus.Pending, contextOrg1).ToListAsync();
        var org2Results = await JobCardQueries.GetJobCardsByStatus(JobCardStatus.Pending, contextOrg2).ToListAsync();

        // Assert
        org1Results.Should().HaveCount(1);
        org2Results.Should().HaveCount(1);
    }
}
