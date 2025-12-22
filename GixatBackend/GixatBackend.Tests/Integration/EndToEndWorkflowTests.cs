using FluentAssertions;
using GixatBackend.Modules.Sessions.Enums;
using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Tests.Helpers;
using GixatBackend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Tests.Integration;

/// <summary>
/// End-to-end workflow tests that verify complete business processes
/// with proper multi-tenancy enforcement
/// </summary>
public class EndToEndWorkflowTests : MultiTenancyTestBase
{
    [Fact]
    public async Task CompleteGarageWorkflow_FromCustomerToJobCardCompletion_ShouldWork()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateInMemoryContextWithTenant(orgId);

        // Step 1: Create Customer
        var customer = CreateTestCustomer(orgId, "John Doe");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Step 2: Register Car
        var car = CreateTestCar(customer.Id, orgId, "ABC123");
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        // Step 3: Create Garage Session
        var session = TestDataBuilder.CreateSession(customer.Id, car.Id, orgId);
        session.CustomerRequests = "Oil change and brake inspection";
        session.Status = SessionStatus.CustomerRequest;
        context.GarageSessions.Add(session);
        await context.SaveChangesAsync();

        // Step 4: Update session through workflow
        session.Status = SessionStatus.Inspection;
        session.InspectionNotes = "Brake pads worn, oil dirty";
        await context.SaveChangesAsync();

        session.Status = SessionStatus.ReportGenerated;
        await context.SaveChangesAsync();

        // Step 5: Create Job Card
        var jobCard = TestDataBuilder.CreateJobCard(session.Id, customer.Id, car.Id, orgId);
        jobCard.InternalNotes = session.InspectionNotes;
        context.JobCards.Add(jobCard);
        await context.SaveChangesAsync();

        // Step 6: Add Job Items
        var jobItem1 = TestDataBuilder.CreateJobItem(jobCard.Id, "Oil Change", 50m);
        var jobItem2 = TestDataBuilder.CreateJobItem(jobCard.Id, "Replace Brake Pads", 150m);
        context.JobItems.AddRange(jobItem1, jobItem2);
        await context.SaveChangesAsync();

        // Step 7: Update Job Card Status
        jobCard.Status = JobCardStatus.InProgress;
        await context.SaveChangesAsync();

        // Step 8: Complete Job Items
        jobItem1.Status = JobItemStatus.Completed;
        jobItem1.ActualLaborCost = 40m;
        jobItem1.ActualPartsCost = 15m;

        jobItem2.Status = JobItemStatus.Completed;
        jobItem2.ActualLaborCost = 100m;
        jobItem2.ActualPartsCost = 80m;
        await context.SaveChangesAsync();

        // Step 9: Complete Job Card
        jobCard.Status = JobCardStatus.Completed;
        jobCard.TotalActualCost = 235m;
        await context.SaveChangesAsync();

        // Assert - Verify complete workflow
        var finalCustomer = await context.Customers
            .Include(c => c.Cars)
            .FirstAsync(c => c.Id == customer.Id);
        finalCustomer.Should().NotBeNull();
        finalCustomer.Cars.Should().HaveCount(1);

        var finalSession = await context.GarageSessions
            .FirstAsync(s => s.Id == session.Id);
        finalSession.Status.Should().Be(SessionStatus.ReportGenerated);

        var finalJobCard = await context.JobCards
            .Include(j => j.Items)
            .FirstAsync(j => j.Id == jobCard.Id);
        finalJobCard.Status.Should().Be(JobCardStatus.Completed);
        finalJobCard.Items.Should().HaveCount(2);
        finalJobCard.Items.Should().OnlyContain(i => i.Status == JobItemStatus.Completed);
        finalJobCard.TotalActualCost.Should().Be(235m);

        // Verify organization consistency
        finalCustomer.OrganizationId.Should().Be(orgId);
        finalSession.OrganizationId.Should().Be(orgId);
        finalJobCard.OrganizationId.Should().Be(orgId);
    }

    [Fact]
    public async Task MultipleCustomers_WithMultipleCars_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateInMemoryContextWithTenant(orgId);

        // Create first customer with 2 cars
        var customer1 = CreateTestCustomer(orgId, "Customer One");
        context.Customers.Add(customer1);
        await context.SaveChangesAsync();

        var car1 = CreateTestCar(customer1.Id, orgId, "CAR1");
        var car2 = CreateTestCar(customer1.Id, orgId, "CAR2");
        context.Cars.AddRange(car1, car2);
        await context.SaveChangesAsync();

        // Create second customer with 1 car
        var customer2 = CreateTestCustomer(orgId, "Customer Two");
        context.Customers.Add(customer2);
        await context.SaveChangesAsync();

        var car3 = CreateTestCar(customer2.Id, orgId, "CAR3");
        context.Cars.Add(car3);
        await context.SaveChangesAsync();

        // Create sessions for each car
        var session1 = TestDataBuilder.CreateSession(customer1.Id, car1.Id, orgId);
        var session2 = TestDataBuilder.CreateSession(customer1.Id, car2.Id, orgId);
        var session3 = TestDataBuilder.CreateSession(customer2.Id, car3.Id, orgId);
        context.GarageSessions.AddRange(session1, session2, session3);
        await context.SaveChangesAsync();

        // Act - Query and verify
        var customers = await context.Customers.Include(c => c.Cars).ToListAsync();
        var sessions = await context.GarageSessions.ToListAsync();

        // Assert
        customers.Should().HaveCount(2);
        
        var c1 = customers.First(c => c.FirstName == "Customer" && c.LastName == "One");
        c1.Cars.Should().HaveCount(2);
        
        var c2 = customers.First(c => c.FirstName == "Customer" && c.LastName == "Two");
        c2.Cars.Should().HaveCount(1);

        sessions.Should().HaveCount(3);
        sessions.Should().OnlyContain(s => s.OrganizationId == orgId);
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldMaintainConsistency()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateInMemoryContextWithTenant(orgId);

        // Create base data
        var customers = new List<GixatBackend.Modules.Customers.Models.Customer>();
        for (int i = 0; i < 5; i++)
        {
            var customer = CreateTestCustomer(orgId, $"Customer {i}");
            customers.Add(customer);
            context.Customers.Add(customer);
        }
        await context.SaveChangesAsync();

        // Create cars for each customer
        var cars = new List<GixatBackend.Modules.Customers.Models.Car>();
        foreach (var customer in customers)
        {
            var car = CreateTestCar(customer.Id, orgId, $"CAR{customer.FirstName}");
            cars.Add(car);
            context.Cars.Add(car);
        }
        await context.SaveChangesAsync();

        // Create sessions
        var sessions = new List<GixatBackend.Modules.Sessions.Models.GarageSession>();
        for (int i = 0; i < cars.Count; i++)
        {
            var session = TestDataBuilder.CreateSession(customers[i].Id, cars[i].Id, orgId);
            sessions.Add(session);
            context.GarageSessions.Add(session);
        }
        await context.SaveChangesAsync();

        // Act - Verify all data is properly linked
        var allCustomers = await context.Customers.Include(c => c.Cars).ToListAsync();
        var allSessions = await context.GarageSessions.ToListAsync();

        // Assert
        allCustomers.Should().HaveCount(5);
        allCustomers.Should().OnlyContain(c => c.OrganizationId == orgId);
        allCustomers.SelectMany(c => c.Cars).Should().HaveCount(5);
        
        allSessions.Should().HaveCount(5);
        allSessions.Should().OnlyContain(s => s.OrganizationId == orgId);
    }

    [Fact]
    public async Task CustomerWithMultipleVisits_ShouldTrackHistory()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var context = TestDbContextFactory.CreateInMemoryContextWithTenant(orgId);

        var customer = CreateTestCustomer(orgId, "Loyal Customer");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var car = CreateTestCar(customer.Id, orgId, "LOYAL-CAR");
        context.Cars.Add(car);
        await context.SaveChangesAsync();

        // Create 3 visits (sessions) over time
        var session1 = TestDataBuilder.CreateSession(customer.Id, car.Id, orgId);
        session1.CreatedAt = DateTime.UtcNow.AddMonths(-2);
        session1.Status = SessionStatus.JobCardCreated;

        var session2 = TestDataBuilder.CreateSession(customer.Id, car.Id, orgId);
        session2.CreatedAt = DateTime.UtcNow.AddMonths(-1);
        session2.Status = SessionStatus.JobCardCreated;

        var session3 = TestDataBuilder.CreateSession(customer.Id, car.Id, orgId);
        session3.CreatedAt = DateTime.UtcNow;
        session3.Status = SessionStatus.CustomerRequest;

        context.GarageSessions.AddRange(session1, session2, session3);
        await context.SaveChangesAsync();

        // Create job cards for completed sessions
        var jobCard1 = TestDataBuilder.CreateJobCard(session1.Id, customer.Id, car.Id, orgId);
        jobCard1.Status = JobCardStatus.Completed;
        jobCard1.TotalActualCost = 100m;

        var jobCard2 = TestDataBuilder.CreateJobCard(session2.Id, customer.Id, car.Id, orgId);
        jobCard2.Status = JobCardStatus.Completed;
        jobCard2.TotalActualCost = 150m;

        context.JobCards.AddRange(jobCard1, jobCard2);
        await context.SaveChangesAsync();

        // Act - Query customer history
        var customerSessions = await context.GarageSessions
            .Where(s => s.CustomerId == customer.Id)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        var customerJobCards = await context.JobCards
            .Where(j => j.CustomerId == customer.Id)
            .ToListAsync();

        // Assert
        customerSessions.Should().HaveCount(3);
        customerSessions.Should().OnlyContain(s => s.OrganizationId == orgId);
        customerSessions.Should().OnlyContain(s => s.CarId == car.Id);

        customerJobCards.Should().HaveCount(2);
        customerJobCards.Should().OnlyContain(j => j.Status == JobCardStatus.Completed);
        customerJobCards.Sum(j => j.TotalActualCost).Should().Be(250m);
    }
}
