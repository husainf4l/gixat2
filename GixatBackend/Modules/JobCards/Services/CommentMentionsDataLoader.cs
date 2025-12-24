using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.JobCards.Services;

/// <summary>
/// DataLoader to efficiently load mentions for comments
/// </summary>
internal sealed class CommentMentionsDataLoader(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IBatchScheduler batchScheduler,
    DataLoaderOptions? options = null)
    : GroupedDataLoader<Guid, JobCardCommentMention>(batchScheduler, options ?? new DataLoaderOptions())
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

    protected override async Task<ILookup<Guid, JobCardCommentMention>> LoadGroupedBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var mentions = await context.JobCardCommentMentions
            .AsNoTracking()
            .Where(m => keys.Contains(m.CommentId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return mentions.ToLookup(m => m.CommentId);
    }
}
