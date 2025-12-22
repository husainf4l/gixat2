using GixatBackend.Data;
using GixatBackend.Modules.Customers.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.Customers.Services;

internal sealed class CarsByCustomerDataLoader : GroupedDataLoader<Guid, Car>
{
    private readonly IServiceProvider _serviceProvider;

    public CarsByCustomerDataLoader(
        IServiceProvider serviceProvider,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<ILookup<Guid, Car>> LoadGroupedBatchAsync(
        IReadOnlyList<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        // Create a scope to resolve scoped services (DbContext, TenantService)
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var cars = await context.Cars
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.CustomerId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        
        return cars.ToLookup(c => c.CustomerId);
    }
}
