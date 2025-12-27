using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.JobCards.Services;

/// <summary>
/// DataLoader for efficiently loading labor entries for job items
/// </summary>
internal sealed class JobItemLaborEntriesDataLoader : GroupedDataLoader<Guid, LaborEntry>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public JobItemLaborEntriesDataLoader(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options!)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    }

    protected override async Task<ILookup<Guid, LaborEntry>> LoadGroupedBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var laborEntries = await context.LaborEntries
            .Include(le => le.Technician)
            .Where(le => keys.Contains(le.JobItemId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return laborEntries.ToLookup(le => le.JobItemId);
    }
}
