using GixatBackend.Data;
using GixatBackend.Modules.Users.Models;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.Users.Services;

internal sealed class UserByIdDataLoader : BatchDataLoader<string, ApplicationUser?>
{
    private readonly IServiceProvider _serviceProvider;

    public UserByIdDataLoader(
        IServiceProvider serviceProvider,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<IReadOnlyDictionary<string, ApplicationUser?>> LoadBatchAsync(
        IReadOnlyList<string> userIds,
        CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var users = await context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        
        return users.ToDictionary(u => u.Id, u => (ApplicationUser?)u);
    }
}
