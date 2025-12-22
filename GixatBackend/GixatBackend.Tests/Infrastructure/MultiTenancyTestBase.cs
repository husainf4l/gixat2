using GixatBackend.Data;
using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Tests.Infrastructure;

/// <summary>
/// Base class for multi-tenancy tests that provides common setup and utilities
/// </summary>
public abstract class MultiTenancyTestBase
{
    protected Organization CreateTestOrganization(string name = "Test Org")
    {
        return TestDataBuilder.CreateOrganization(name);
    }

    protected ApplicationUser CreateTestUser(Guid organizationId, string email = "user@test.com")
    {
        return TestDataBuilder.CreateUser(email, organizationId);
    }

    protected Customer CreateTestCustomer(Guid organizationId, string name = "Test Customer")
    {
        return TestDataBuilder.CreateCustomer(organizationId, name);
    }

    protected Car CreateTestCar(Guid customerId, Guid organizationId, string plateNumber = "ABC123")
    {
        return TestDataBuilder.CreateCar(customerId, organizationId, plateNumber: plateNumber);
    }

    protected async Task<ApplicationDbContext> SetupTwoOrganizationsWithDataAsync()
    {
        var org1 = CreateTestOrganization("Organization 1");
        var org2 = CreateTestOrganization("Organization 2");

        var context = TestDbContextFactory.CreateInMemoryContext();
        
        context.Organizations.AddRange(org1, org2);
        await context.SaveChangesAsync();

        return context;
    }

    protected async Task AssertTenantIsolationAsync(
        ApplicationDbContext contextOrg1,
        ApplicationDbContext contextOrg2,
        Func<ApplicationDbContext, Task<int>> countFunc)
    {
        var countOrg1 = await countFunc(contextOrg1);
        var countOrg2 = await countFunc(contextOrg2);

        // Org1 should see its data, Org2 should not see Org1's data
        countOrg1.Should().BeGreaterThan(0, "Organization 1 should see its own data");
        countOrg2.Should().Be(0, "Organization 2 should not see Organization 1's data");
    }
}
