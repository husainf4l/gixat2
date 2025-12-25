using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.JobCards.Services;

/// <summary>
/// DataLoader for efficiently loading parts for job items
/// </summary>
public sealed class JobItemPartsDataLoader : GroupedDataLoader<Guid, JobItemPart>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public JobItemPartsDataLoader(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    protected override async Task<ILookup<Guid, JobItemPart>> LoadGroupedBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var parts = await context.JobItemParts
            .Include(jip => jip.InventoryItem)
            .Where(jip => keys.Contains(jip.JobItemId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return parts.ToLookup(jip => jip.JobItemId);
    }
}
