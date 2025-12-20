using GixatBackend.Data;
using GixatBackend.Modules.Lookup.Models;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Lookup.GraphQL;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed record CreateLookupItemInput(
    string Category,
    string Value,
    Guid? ParentId = null,
    string? Metadata = null,
    int SortOrder = 0);

[ExtendObjectType(OperationTypeNames.Mutation)]
[Authorize]
internal static class LookupMutations
{
    public static async Task<LookupItem> CreateLookupItemAsync(
        CreateLookupItemInput input,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        var item = new LookupItem
        {
            Category = input.Category,
            Value = input.Value,
            ParentId = input.ParentId,
            Metadata = input.Metadata,
            SortOrder = input.SortOrder
        };

        context.LookupItems.Add(item);
        await context.SaveChangesAsync().ConfigureAwait(false);
        return item;
    }
}
