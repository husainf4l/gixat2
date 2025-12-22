using GixatBackend.Data;
using GixatBackend.Modules.Common.Services.Tenant;
using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Sessions.Models;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Enums;
using GixatBackend.Modules.Sessions.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using Testcontainers.PostgreSql;

namespace GixatBackend.Tests.Infrastructure;

/// <summary>
/// Base class for integration tests that use a real PostgreSQL database with TestContainers.
/// This ensures global query filters work exactly as in production.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private PostgreSqlContainer? _postgreSqlContainer;
    private string? _connectionString;

    /// <summary>
    /// Initializes the PostgreSQL container before tests run
    /// </summary>
    public async Task InitializeAsync()
    {
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("gixat_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithCleanUp(true)
            .Build();

        await _postgreSqlContainer.StartAsync();
        _connectionString = _postgreSqlContainer.GetConnectionString();

        // Run migrations to set up the database schema
        await using var context = CreateDbContext(null);
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Cleans up the PostgreSQL container after tests complete
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_postgreSqlContainer != null)
        {
            await _postgreSqlContainer.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a DbContext instance configured for the specified organization.
    /// This context will use real PostgreSQL and global query filters will work!
    /// </summary>
    protected ApplicationDbContext CreateDbContext(Guid? organizationId)
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            throw new InvalidOperationException("Database connection not initialized. Ensure InitializeAsync was called.");
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        var tenantServiceMock = new Mock<ITenantService>();
        tenantServiceMock.Setup(x => x.OrganizationId).Returns(organizationId);

        return new ApplicationDbContext(options, tenantServiceMock.Object);
    }

    /// <summary>
    /// Creates a DbContext with the specified organization ID for tenant-scoped operations
    /// </summary>
    protected ApplicationDbContext CreateContextForOrganization(Guid organizationId)
    {
        return CreateDbContext(organizationId);
    }

    /// <summary>
    /// Cleans all data from multi-tenant tables to ensure test isolation
    /// </summary>
    protected async Task CleanDatabaseAsync()
    {
        await using var context = CreateDbContext(null);
        
        // Delete in reverse order of dependencies
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"JobItems\"");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"JobCardMedias\"");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"JobCards\"");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"SessionLogs\"");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"SessionMedias\"");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"GarageSessions\"");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Cars\"");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Customers\"");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"UserInvites\"");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"AspNetUsers\" WHERE \"OrganizationId\" IS NOT NULL");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"Organizations\"");
    }

    #region Helper Methods for Creating Test Data

    /// <summary>
    /// Creates organizations with specific IDs in the database (required for foreign key constraints)
    /// </summary>
    protected async Task CreateOrganizationsAsync(params Guid[] organizationIds)
    {
        await using var contextNoTenant = CreateDbContext(null);
        
        foreach (var orgId in organizationIds)
        {
            var org = new Organization
            {
                Id = orgId,
                Name = $"Test Organization {orgId}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            contextNoTenant.Organizations.Add(org);
        }
        
        await contextNoTenant.SaveChangesAsync();
    }

    protected Organization CreateTestOrganization(string name = "Test Organization")
    {
        return new Organization
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    protected ApplicationUser CreateTestUser(Guid organizationId, string email = "user@test.com", string role = "OrgUser")
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            OrganizationId = organizationId,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Test User",
            UserType = UserType.Organizational,
            IsActive = true
        };
    }

    protected Customer CreateTestCustomer(Guid organizationId, string name)
    {
        var nameParts = name.Split(' ', 2);
        return new Customer
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            FirstName = nameParts[0],
            LastName = nameParts.Length > 1 ? nameParts[1] : "Customer",
            PhoneNumber = $"+{Random.Shared.Next(1000000000, int.MaxValue)}",
            Email = $"{name.ToLower().Replace(" ", ".")}@test.com",
            CreatedAt = DateTime.UtcNow
        };
    }

    protected Car CreateTestCar(Guid customerId, Guid organizationId, string licensePlate)
    {
        return new Car
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            OrganizationId = organizationId,
            LicensePlate = licensePlate,
            Make = "Toyota",
            Model = "Camry",
            Year = 2023,
            Color = "Silver",
            CreatedAt = DateTime.UtcNow
        };
    }

    protected GarageSession CreateTestSession(Guid customerId, Guid carId, Guid organizationId)
    {
        return new GarageSession
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CarId = carId,
            OrganizationId = organizationId,
            Status = SessionStatus.CustomerRequest,
            CustomerRequests = "Test request",
            CreatedAt = DateTime.UtcNow
        };
    }

    protected JobCard CreateTestJobCard(Guid sessionId, Guid customerId, Guid carId, Guid organizationId, string? jobNumber = null)
    {
        return new JobCard
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            CustomerId = customerId,
            CarId = carId,
            OrganizationId = organizationId,
            Status = JobCardStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    protected JobItem CreateTestJobItem(Guid jobCardId, string description, decimal estimatedLaborCost = 100m, decimal estimatedPartsCost = 50m)
    {
        return new JobItem
        {
            Id = Guid.NewGuid(),
            JobCardId = jobCardId,
            Description = description,
            EstimatedLaborCost = estimatedLaborCost,
            EstimatedPartsCost = estimatedPartsCost,
            Status = JobItemStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates two organizations with sample data for multi-tenancy testing
    /// </summary>
    protected async Task<(Guid org1Id, Guid org2Id)> SetupTwoOrganizationsWithDataAsync()
    {
        // Create organizations without tenant context
        await using var contextNoTenant = CreateDbContext(null);
        
        var org1 = CreateTestOrganization("Organization One");
        var org2 = CreateTestOrganization("Organization Two");
        
        contextNoTenant.Organizations.AddRange(org1, org2);
        await contextNoTenant.SaveChangesAsync();

        var org1Id = org1.Id;
        var org2Id = org2.Id;

        // Create data for org1
        await using (var contextOrg1 = CreateContextForOrganization(org1Id))
        {
            var customer1 = CreateTestCustomer(org1Id, "Customer One");
            var customer2 = CreateTestCustomer(org1Id, "Customer Two");
            contextOrg1.Customers.AddRange(customer1, customer2);
            await contextOrg1.SaveChangesAsync();

            var car1 = CreateTestCar(customer1.Id, org1Id, "ORG1-CAR1");
            var car2 = CreateTestCar(customer2.Id, org1Id, "ORG1-CAR2");
            contextOrg1.Cars.AddRange(car1, car2);
            await contextOrg1.SaveChangesAsync();
        }

        // Create data for org2
        await using (var contextOrg2 = CreateContextForOrganization(org2Id))
        {
            var customer3 = CreateTestCustomer(org2Id, "Customer Three");
            contextOrg2.Customers.Add(customer3);
            await contextOrg2.SaveChangesAsync();

            var car3 = CreateTestCar(customer3.Id, org2Id, "ORG2-CAR1");
            contextOrg2.Cars.Add(car3);
            await contextOrg2.SaveChangesAsync();
        }

        return (org1Id, org2Id);
    }

    /// <summary>
    /// Verifies that queries from one organization cannot access another organization's data
    /// </summary>
    protected async Task AssertTenantIsolationAsync(Guid org1Id, Guid org2Id)
    {
        await using var contextOrg1 = CreateContextForOrganization(org1Id);
        await using var contextOrg2 = CreateContextForOrganization(org2Id);

        // Each org should only see their own customers
        var org1Customers = await contextOrg1.Customers.CountAsync();
        var org2Customers = await contextOrg2.Customers.CountAsync();

        if (org1Customers == 0 || org2Customers == 0)
        {
            throw new InvalidOperationException("Tenant isolation check failed: One or both organizations have no customers.");
        }

        // Query all customers without filters - should still be filtered by global query filter
        var allCustomersFromOrg1Context = await contextOrg1.Customers.ToListAsync();
        var allCustomersFromOrg2Context = await contextOrg2.Customers.ToListAsync();

        // Verify no overlap
        var org1CustomerIds = allCustomersFromOrg1Context.Select(c => c.Id).ToHashSet();
        var org2CustomerIds = allCustomersFromOrg2Context.Select(c => c.Id).ToHashSet();

        if (org1CustomerIds.Overlaps(org2CustomerIds))
        {
            throw new InvalidOperationException("Tenant isolation violated: Organizations can see each other's customers!");
        }
    }

    #endregion
}
