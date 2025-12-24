using GixatBackend.Data;
using GixatBackend.Modules.Sessions.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.Sessions.Services;

/// <summary>
/// DataLoader for efficiently loading sessions grouped by car ID.
/// Prevents N+1 query problems when loading sessions for multiple cars.
/// </summary>
public sealed class SessionsByCarDataLoader : GroupedDataLoader<Guid, GarageSession>
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public SessionsByCarDataLoader(
        IBatchScheduler batchScheduler,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    protected override async Task<ILookup<Guid, GarageSession>> LoadGroupedBatchAsync(
        IReadOnlyList<Guid> carIds,
        CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        
        var sessions = await context.GarageSessions
            .AsNoTracking()
            .Where(s => carIds.Contains(s.CarId))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return sessions.ToLookup(s => s.CarId);
    }
}
