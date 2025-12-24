using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.JobCards.Services;

/// <summary>
/// DataLoader to efficiently load replies for comments
/// </summary>
internal sealed class CommentRepliesDataLoader(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IBatchScheduler batchScheduler,
    DataLoaderOptions? options = null)
    : GroupedDataLoader<Guid, JobCardComment>(batchScheduler, options ?? new DataLoaderOptions())
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

    protected override async Task<ILookup<Guid, JobCardComment>> LoadGroupedBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var replies = await context.JobCardComments
            .AsNoTracking()
            .Where(c => c.ParentCommentId != null && keys.Contains(c.ParentCommentId.Value) && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return replies.ToLookup(r => r.ParentCommentId!.Value);
    }
}
