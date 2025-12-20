using GixatBackend.Data;
using GixatBackend.Modules.Lookup.Models;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Lookup.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
[Authorize]
internal static class LookupQueries
{
    public static async Task<IEnumerable<LookupItem>> GetAutocompleteItemsAsync(
        string category,
        string? query,
        Guid? parentId,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var dbQuery = context.LookupItems
            .Where(l => l.Category == category && l.IsActive);

        if (parentId.HasValue)
        {
            dbQuery = dbQuery.Where(l => l.ParentId == parentId);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            dbQuery = dbQuery.Where(l => l.Value.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return await dbQuery
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Value)
            .Take(20)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public static async Task<IEnumerable<string>> GetCategoriesAsync(
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await context.LookupItems
            .Select(l => l.Category)
            .Distinct()
            .ToListAsync()
            .ConfigureAwait(false);
    }
}
