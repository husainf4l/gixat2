using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.JobCards.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
[Authorize]
internal static class JobCardQueries
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<JobCard> GetJobCards(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.JobCards;
    }

    public static async Task<JobCard?> GetJobCardByIdAsync(Guid id, ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await context.JobCards
            .Include(j => j.Car)
            .Include(j => j.Customer)
            .Include(j => j.Items)
            .Include(j => j.Session)
            .FirstOrDefaultAsync(j => j.Id == id).ConfigureAwait(false);
    }
}
