using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.JobCards.Services;

internal sealed class JobCardCommentByIdDataLoader(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IBatchScheduler batchScheduler,
    DataLoaderOptions? options = null)
    : BatchDataLoader<Guid, JobCardComment?>(batchScheduler, options ?? new DataLoaderOptions())
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

    protected override async Task<IReadOnlyDictionary<Guid, JobCardComment?>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var comments = await context.JobCardComments
            .AsNoTracking()
            .Where(c => keys.Contains(c.Id) && !c.IsDeleted)
            .ToDictionaryAsync(c => c.Id, cancellationToken)
            .ConfigureAwait(false);

        return comments!;
    }
}
