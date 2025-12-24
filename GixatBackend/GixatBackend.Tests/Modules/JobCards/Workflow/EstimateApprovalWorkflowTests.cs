using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Data;
using GixatBackend.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Tests.Modules.JobCards.Workflow;

public sealed class EstimateApprovalWorkflowTests : IntegrationTestBase
{
    [Fact]
    public async Task JobCard_InitialState_ShouldNotBeApproved()
    {
        // Arrange
        var org = CreateTestOrganization();
        using var context = CreateDbContext(org.Id);
        var customer = CreateTestCustomer(org.Id, "John Doe");
        var car = CreateTestCar(customer.Id, org.Id, "ABC123");
        var session = CreateTestSession(car.Id, customer.Id, org.Id);

        await context.Organizations.AddAsync(org);
        await context.Customers.AddAsync(customer);
        await context.Cars.AddAsync(car);
        await context.GarageSessions.AddAsync(session);
        await context.SaveChangesAsync();

        // Act
        var jobCard = new JobCard
        {
            SessionId = session.Id,
            CarId = car.Id,
            CustomerId = customer.Id,
            OrganizationId = org.Id,
            Status = JobCardStatus.Pending
        };

        context.JobCards.Add(jobCard);
        await context.SaveChangesAsync();

        // Assert
        var savedJobCard = await context.JobCards
            .FirstOrDefaultAsync(j => j.Id == jobCard.Id);

        Assert.NotNull(savedJobCard);
        Assert.False(savedJobCard.IsApprovedByCustomer);
        Assert.Null(savedJobCard.ApprovedAt);
    }

    [Fact]
    public async Task ApproveJobCard_ShouldSetApprovalFlagsCorrectly()
    {
        // Arrange
        var org = CreateTestOrganization();
        using var context = CreateDbContext(org.Id);
        var customer = CreateTestCustomer(org.Id, "Jane Smith");
        var car = CreateTestCar(customer.Id, org.Id, "XYZ789");
        var session = CreateTestSession(car.Id, customer.Id, org.Id);

        await context.Organizations.AddAsync(org);
        await context.Customers.AddAsync(customer);
        await context.Cars.AddAsync(car);
        await context.GarageSessions.AddAsync(session);

        var jobCard = new JobCard
        {
            SessionId = session.Id,
            CarId = car.Id,
            CustomerId = customer.Id,
            OrganizationId = org.Id,
            Status = JobCardStatus.Pending,
            TotalEstimatedCost = 500m
        };

        context.JobCards.Add(jobCard);
        await context.SaveChangesAsync();

        // Act
        jobCard.IsApprovedByCustomer = true;
        jobCard.ApprovedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var approvedJobCard = await context.JobCards
            .FirstOrDefaultAsync(j => j.Id == jobCard.Id);

        Assert.NotNull(approvedJobCard);
        Assert.True(approvedJobCard.IsApprovedByCustomer);
        Assert.NotNull(approvedJobCard.ApprovedAt);
        Assert.True((DateTime.UtcNow - approvedJobCard.ApprovedAt.Value).TotalSeconds < 2);
    }

    [Fact]
    public async Task ApproveJobCard_ShouldApproveAllJobItems()
    {
        // Arrange
        var org = CreateTestOrganization();
        using var context = CreateDbContext(org.Id);
        var customer = CreateTestCustomer(org.Id, "Bob Johnson");
        var car = CreateTestCar(customer.Id, org.Id, "DEF456");
        var session = CreateTestSession(car.Id, customer.Id, org.Id);

        await context.Organizations.AddAsync(org);
        await context.Customers.AddAsync(customer);
        await context.Cars.AddAsync(car);
        await context.GarageSessions.AddAsync(session);

        var jobCard = new JobCard
        {
            SessionId = session.Id,
            CarId = car.Id,
            CustomerId = customer.Id,
            OrganizationId = org.Id,
            Status = JobCardStatus.Pending
        };

        var item1 = new JobItem
        {
            JobCardId = jobCard.Id,
            Description = "Oil Change",
            EstimatedLaborCost = 50m,
            EstimatedPartsCost = 30m
        };

        var item2 = new JobItem
        {
            JobCardId = jobCard.Id,
            Description = "Brake Pads Replacement",
            EstimatedLaborCost = 100m,
            EstimatedPartsCost = 150m
        };

        jobCard.Items.Add(item1);
        jobCard.Items.Add(item2);

        context.JobCards.Add(jobCard);
        await context.SaveChangesAsync();

        // Act - Approve JobCard and all items
        jobCard.IsApprovedByCustomer = true;
        jobCard.ApprovedAt = DateTime.UtcNow;

        foreach (var item in jobCard.Items)
        {
            item.IsApprovedByCustomer = true;
            item.ApprovedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        // Assert
        var approvedJobCard = await context.JobCards
            .Include(j => j.Items)
            .FirstOrDefaultAsync(j => j.Id == jobCard.Id);

        Assert.NotNull(approvedJobCard);
        Assert.True(approvedJobCard.IsApprovedByCustomer);
        Assert.All(approvedJobCard.Items, item =>
        {
            Assert.True(item.IsApprovedByCustomer);
            Assert.NotNull(item.ApprovedAt);
        });
    }

    [Fact]
    public async Task ApproveIndividualJobItem_ShouldNotAffectOtherItems()
    {
        // Arrange
        var org = CreateTestOrganization();
        using var context = CreateDbContext(org.Id);
        var customer = CreateTestCustomer(org.Id, "Alice Brown");
        var car = CreateTestCar(customer.Id, org.Id, "GHI789");
        var session = CreateTestSession(car.Id, customer.Id, org.Id);

        await context.Organizations.AddAsync(org);
        await context.Customers.AddAsync(customer);
        await context.Cars.AddAsync(car);
        await context.GarageSessions.AddAsync(session);

        var jobCard = new JobCard
        {
            SessionId = session.Id,
            CarId = car.Id,
            CustomerId = customer.Id,
            OrganizationId = org.Id,
            Status = JobCardStatus.Pending
        };

        var item1 = new JobItem
        {
            JobCardId = jobCard.Id,
            Description = "Essential Repair",
            EstimatedLaborCost = 200m,
            EstimatedPartsCost = 100m
        };

        var item2 = new JobItem
        {
            JobCardId = jobCard.Id,
            Description = "Optional Service",
            EstimatedLaborCost = 50m,
            EstimatedPartsCost = 20m
        };

        jobCard.Items.Add(item1);
        jobCard.Items.Add(item2);

        context.JobCards.Add(jobCard);
        await context.SaveChangesAsync();

        // Act - Approve only the first item
        item1.IsApprovedByCustomer = true;
        item1.ApprovedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Assert
        var reloadedJobCard = await context.JobCards
            .Include(j => j.Items)
            .FirstOrDefaultAsync(j => j.Id == jobCard.Id);

        Assert.NotNull(reloadedJobCard);
        Assert.False(reloadedJobCard.IsApprovedByCustomer); // JobCard itself not approved

        var approvedItem = reloadedJobCard.Items.First(i => i.Description == "Essential Repair");
        var unapprovedItem = reloadedJobCard.Items.First(i => i.Description == "Optional Service");

        Assert.True(approvedItem.IsApprovedByCustomer);
        Assert.NotNull(approvedItem.ApprovedAt);

        Assert.False(unapprovedItem.IsApprovedByCustomer);
        Assert.Null(unapprovedItem.ApprovedAt);
    }

    [Fact]
    public async Task EstimateCostCalculation_ShouldBeAccurate()
    {
        // Arrange
        var org = CreateTestOrganization();
        using var context = CreateDbContext(org.Id);
        var customer = CreateTestCustomer(org.Id, "Charlie Davis");
        var car = CreateTestCar(customer.Id, org.Id, "JKL012");
        var session = CreateTestSession(car.Id, customer.Id, org.Id);

        await context.Organizations.AddAsync(org);
        await context.Customers.AddAsync(customer);
        await context.Cars.AddAsync(car);
        await context.GarageSessions.AddAsync(session);

        var jobCard = new JobCard
        {
            SessionId = session.Id,
            CarId = car.Id,
            CustomerId = customer.Id,
            OrganizationId = org.Id,
            Status = JobCardStatus.Pending
        };

        var item1 = new JobItem
        {
            JobCardId = jobCard.Id,
            Description = "Service 1",
            EstimatedLaborCost = 100m,
            EstimatedPartsCost = 50m
        };

        var item2 = new JobItem
        {
            JobCardId = jobCard.Id,
            Description = "Service 2",
            EstimatedLaborCost = 150m,
            EstimatedPartsCost = 200m
        };

        jobCard.Items.Add(item1);
        jobCard.Items.Add(item2);

        // Act
        jobCard.TotalEstimatedLabor = jobCard.Items.Sum(i => i.EstimatedLaborCost);
        jobCard.TotalEstimatedParts = jobCard.Items.Sum(i => i.EstimatedPartsCost);
        jobCard.TotalEstimatedCost = jobCard.TotalEstimatedLabor + jobCard.TotalEstimatedParts;

        context.JobCards.Add(jobCard);
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(250m, jobCard.TotalEstimatedLabor); // 100 + 150
        Assert.Equal(250m, jobCard.TotalEstimatedParts); // 50 + 200
        Assert.Equal(500m, jobCard.TotalEstimatedCost); // 250 + 250
    }

    [Fact]
    public async Task PartialApproval_Scenario_ShouldCalculateCorrectly()
    {
        // Arrange
        var org = CreateTestOrganization();
        using var context = CreateDbContext(org.Id);
        var customer = CreateTestCustomer(org.Id, "David Wilson");
        var car = CreateTestCar(customer.Id, org.Id, "MNO345");
        var session = CreateTestSession(car.Id, customer.Id, org.Id);

        await context.Organizations.AddAsync(org);
        await context.Customers.AddAsync(customer);
        await context.Cars.AddAsync(car);
        await context.GarageSessions.AddAsync(session);

        var jobCard = new JobCard
        {
            SessionId = session.Id,
            CarId = car.Id,
            CustomerId = customer.Id,
            OrganizationId = org.Id,
            Status = JobCardStatus.Pending
        };

        var item1 = new JobItem
        {
            JobCardId = jobCard.Id,
            Description = "Critical Repair",
            EstimatedLaborCost = 300m,
            EstimatedPartsCost = 200m
        };

        var item2 = new JobItem
        {
            JobCardId = jobCard.Id,
            Description = "Optional Upgrade",
            EstimatedLaborCost = 100m,
            EstimatedPartsCost = 500m
        };

        var item3 = new JobItem
        {
            JobCardId = jobCard.Id,
            Description = "Recommended Service",
            EstimatedLaborCost = 50m,
            EstimatedPartsCost = 30m
        };

        jobCard.Items.Add(item1);
        jobCard.Items.Add(item2);
        jobCard.Items.Add(item3);

        context.JobCards.Add(jobCard);
        await context.SaveChangesAsync();

        // Act - Customer approves only critical repair and recommended service
        item1.IsApprovedByCustomer = true;
        item1.ApprovedAt = DateTime.UtcNow;

        item3.IsApprovedByCustomer = true;
        item3.ApprovedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        // Assert
        var reloadedJobCard = await context.JobCards
            .Include(j => j.Items)
            .FirstOrDefaultAsync(j => j.Id == jobCard.Id);

        Assert.NotNull(reloadedJobCard);

        var approvedItems = reloadedJobCard.Items.Where(i => i.IsApprovedByCustomer).ToList();
        var approvedTotal = approvedItems.Sum(i => i.EstimatedCost);

        Assert.Equal(2, approvedItems.Count);
        Assert.Equal(580m, approvedTotal); // (300 + 200) + (50 + 30) = 580
    }

    [Fact]
    public async Task ApprovalAuditTrail_ShouldCaptureTimestamp()
    {
        // Arrange
        var org = CreateTestOrganization();
        using var context = CreateDbContext(org.Id);
        var customer = CreateTestCustomer(org.Id, "Eve Martinez");
        var car = CreateTestCar(customer.Id, org.Id, "PQR678");
        var session = CreateTestSession(car.Id, customer.Id, org.Id);

        await context.Organizations.AddAsync(org);
        await context.Customers.AddAsync(customer);
        await context.Cars.AddAsync(car);
        await context.GarageSessions.AddAsync(session);

        var jobCard = new JobCard
        {
            SessionId = session.Id,
            CarId = car.Id,
            CustomerId = customer.Id,
            OrganizationId = org.Id,
            Status = JobCardStatus.Pending
        };

        context.JobCards.Add(jobCard);
        await context.SaveChangesAsync();

        var beforeApproval = DateTime.UtcNow;

        // Simulate small delay
        await Task.Delay(100);

        // Act
        jobCard.IsApprovedByCustomer = true;
        jobCard.ApprovedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var afterApproval = DateTime.UtcNow;

        // Assert
        var approvedJobCard = await context.JobCards
            .FirstOrDefaultAsync(j => j.Id == jobCard.Id);

        Assert.NotNull(approvedJobCard);
        Assert.NotNull(approvedJobCard.ApprovedAt);
        Assert.True(approvedJobCard.ApprovedAt >= beforeApproval);
        Assert.True(approvedJobCard.ApprovedAt <= afterApproval);
    }

    [Fact]
    public async Task MultiTenancy_ApprovalWorkflow_ShouldRespectOrganizationIsolation()
    {
        // Arrange
        var org1 = CreateTestOrganization();
        var org2 = CreateTestOrganization();
        
        using var context1 = CreateDbContext(org1.Id);
        using var context2 = CreateDbContext(org2.Id);

        var customer1 = CreateTestCustomer(org1.Id, "Customer Org1");
        var customer2 = CreateTestCustomer(org2.Id, "Customer Org2");

        var car1 = CreateTestCar(customer1.Id, org1.Id, "STU901");
        var car2 = CreateTestCar(customer2.Id, org2.Id, "VWX234");

        var session1 = CreateTestSession(car1.Id, customer1.Id, org1.Id);
        var session2 = CreateTestSession(car2.Id, customer2.Id, org2.Id);

        await context1.Organizations.AddRangeAsync(org1, org2);
        await context1.Customers.AddRangeAsync(customer1, customer2);
        await context1.Cars.AddRangeAsync(car1, car2);
        await context1.GarageSessions.AddRangeAsync(session1, session2);

        var jobCard1 = new JobCard
        {
            SessionId = session1.Id,
            CarId = car1.Id,
            CustomerId = customer1.Id,
            OrganizationId = org1.Id,
            Status = JobCardStatus.Pending
        };

        var jobCard2 = new JobCard
        {
            SessionId = session2.Id,
            CarId = car2.Id,
            CustomerId = customer2.Id,
            OrganizationId = org2.Id,
            Status = JobCardStatus.Pending
        };

        context1.JobCards.AddRange(jobCard1, jobCard2);
        await context1.SaveChangesAsync();

        // Act - Approve jobCard1 in org1
        jobCard1.IsApprovedByCustomer = true;
        jobCard1.ApprovedAt = DateTime.UtcNow;
        await context1.SaveChangesAsync();

        // Assert - Query with org1 context should see only approved jobCard1
        var org1JobCards = await context1.JobCards.ToListAsync();

        Assert.Single(org1JobCards);
        Assert.True(org1JobCards[0].IsApprovedByCustomer);

        // Query with org2 context should see only unapproved jobCard2
        var org2JobCards = await context2.JobCards.ToListAsync();

        Assert.Single(org2JobCards);
        Assert.False(org2JobCards[0].IsApprovedByCustomer);
    }
}
