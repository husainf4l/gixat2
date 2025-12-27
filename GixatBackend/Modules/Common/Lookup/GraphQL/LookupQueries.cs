using GixatBackend.Data;
using GixatBackend.Modules.Common.Lookup.Models;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Common.Lookup.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
[Authorize]
internal sealed class LookupQueries
{
    [UseSorting]
    public static IQueryable<LookupItem> GetLookupItems(
        [Service] ApplicationDbContext context,
        string? category = null,
        string? parentId = null,
        bool includeChildren = true)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var query = context.LookupItems
            .Where(l => l.IsActive);

        // Apply category filter first (uses index)
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(l => l.Category == category);
        }

        // Apply parent filter (uses index)
        if (!string.IsNullOrEmpty(parentId))
        {
            if (Guid.TryParse(parentId, out var parentGuid))
            {
                query = query.Where(l => l.ParentId == parentGuid);
            }
        }
        else if (includeChildren)
        {
            // Only return root items when includeChildren is true and no parentId specified
            query = query.Where(l => l.ParentId == null);
        }

        // Include children with split query for better performance
        if (includeChildren)
        {
            query = query
                .Include(l => l.Children.Where(c => c.IsActive))
                .AsSplitQuery();
        }

        return query;
    }

    [UseSorting]
    public static IQueryable<LookupItem> GetLookupItemsByCategory(
        [Service] ApplicationDbContext context,
        string category,
        bool includeChildren = true)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var query = context.LookupItems
            .Where(l => l.IsActive && l.Category == category && l.ParentId == null);

        if (includeChildren)
        {
            query = query
                .Include(l => l.Children.Where(c => c.IsActive))
                .AsSplitQuery();
        }

        return query;
    }
}
