using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.JobCards.Services;

internal sealed class JobItemsByJobCardDataLoader : GroupedDataLoader<Guid, JobItem>
{
    private readonly IServiceProvider _serviceProvider;

    public JobItemsByJobCardDataLoader(
        IServiceProvider serviceProvider,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options!)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<ILookup<Guid, JobItem>> LoadGroupedBatchAsync(
        IReadOnlyList<Guid> jobCardIds,
        CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var jobItems = await context.JobItems
            .AsNoTracking()
            .Where(i => jobCardIds.Contains(i.JobCardId))
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        
        return jobItems.ToLookup(i => i.JobCardId);
    }
}
