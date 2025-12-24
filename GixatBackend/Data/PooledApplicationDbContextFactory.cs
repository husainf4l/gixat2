using GixatBackend.Modules.Common.Services.Tenant;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Data;

/// <summary>
/// Factory for creating ApplicationDbContext instances for DataLoaders.
/// Uses a null tenant service since DataLoaders don't need tenant filtering.
/// </summary>
internal sealed class PooledApplicationDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public PooledApplicationDbContextFactory(DbContextOptions<ApplicationDbContext> options)
    {
        _options = options;
    }

    public ApplicationDbContext CreateDbContext()
    {
        // DataLoaders use read-only queries, so we provide a null tenant service
        // Tenant filtering is handled at the GraphQL resolver level
        return new ApplicationDbContext(_options, new NullTenantService());
    }
}

/// <summary>
/// Null implementation of ITenantService for DataLoader contexts.
/// DataLoaders don't apply tenant filtering since it's handled by the parent resolver.
/// </summary>
internal sealed class NullTenantService : ITenantService
{
    public Guid? OrganizationId => null;
}
