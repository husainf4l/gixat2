using GixatBackend.Data;
using GixatBackend.Modules.Lookup.Models;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Lookup.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
[Authorize]
internal static class LookupQueries
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<LookupItem> GetLookupItems(
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.LookupItems.Where(l => l.IsActive);
    }
}
