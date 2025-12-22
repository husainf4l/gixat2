using GixatBackend.Data;
using GixatBackend.Modules.Common.Services.Tenant;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GixatBackend.Tests.Helpers;

/// <summary>
/// Factory for creating in-memory database contexts for testing
/// </summary>
public static class TestDbContextFactory
{
    public static ApplicationDbContext CreateInMemoryContext(Guid? organizationId = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantServiceMock = new Mock<ITenantService>();
        tenantServiceMock.Setup(x => x.OrganizationId).Returns(organizationId);

        return new ApplicationDbContext(options, tenantServiceMock.Object);
    }

    public static ApplicationDbContext CreateInMemoryContextWithTenant(Guid organizationId)
    {
        return CreateInMemoryContext(organizationId);
    }
}
