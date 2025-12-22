using GixatBackend.Data;
using GixatBackend.Modules.Sessions.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.Sessions.Services;

internal sealed class SessionsByCustomerDataLoader : GroupedDataLoader<Guid, GarageSession>
{
    private readonly IServiceProvider _serviceProvider;

    public SessionsByCustomerDataLoader(
        IServiceProvider serviceProvider,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<ILookup<Guid, GarageSession>> LoadGroupedBatchAsync(
        IReadOnlyList<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var sessions = await context.GarageSessions
            .AsNoTracking()
            .Where(s => customerIds.Contains(s.CustomerId))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        
        return sessions.ToLookup(s => s.CustomerId);
    }
}
