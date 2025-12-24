using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.JobCards.Services;

internal sealed class JobCardsByCustomerDataLoader : GroupedDataLoader<Guid, JobCard>
{
    private readonly IServiceProvider _serviceProvider;

    public JobCardsByCustomerDataLoader(
        IServiceProvider serviceProvider,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options!)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<ILookup<Guid, JobCard>> LoadGroupedBatchAsync(
        IReadOnlyList<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var jobCards = await context.JobCards
            .AsNoTracking()
            .Where(j => customerIds.Contains(j.CustomerId))
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        
        return jobCards.ToLookup(j => j.CustomerId);
    }
}
