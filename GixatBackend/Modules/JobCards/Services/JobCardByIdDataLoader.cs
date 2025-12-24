using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.JobCards.Services;

internal sealed class JobCardByIdDataLoader(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IBatchScheduler batchScheduler,
    DataLoaderOptions? options = null)
    : BatchDataLoader<Guid, JobCard?>(batchScheduler, options ?? new DataLoaderOptions())
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

    protected override async Task<IReadOnlyDictionary<Guid, JobCard?>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var jobCards = await context.JobCards
            .AsNoTracking()
            .Where(jc => keys.Contains(jc.Id))
            .ToDictionaryAsync(jc => jc.Id, cancellationToken)
            .ConfigureAwait(false);

        return jobCards!;
    }
}
