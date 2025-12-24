using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.JobCards.Services;

internal sealed class JobItemByIdDataLoader(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IBatchScheduler batchScheduler,
    DataLoaderOptions? options = null)
    : BatchDataLoader<Guid, JobItem?>(batchScheduler, options ?? new DataLoaderOptions())
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

    protected override async Task<IReadOnlyDictionary<Guid, JobItem?>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var jobItems = await context.JobItems
            .AsNoTracking()
            .Where(ji => keys.Contains(ji.Id))
            .ToDictionaryAsync(ji => ji.Id, cancellationToken)
            .ConfigureAwait(false);

        return jobItems!;
    }
}
