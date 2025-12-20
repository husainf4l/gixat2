using GixatBackend.Data;
using GixatBackend.Modules.Lookup.Models;

namespace GixatBackend.Modules.Lookup.GraphQL;

public record CreateLookupItemInput(
    string Category,
    string Value,
    Guid? ParentId = null,
    string? Metadata = null,
    int SortOrder = 0);

[ExtendObjectType(OperationTypeNames.Mutation)]
public class LookupMutations
{
    public async Task<LookupItem> CreateLookupItemAsync(
        CreateLookupItemInput input,
        [Service] ApplicationDbContext context)
    {
        var item = new LookupItem
        {
            Category = input.Category,
            Value = input.Value,
            ParentId = input.ParentId,
            Metadata = input.Metadata,
            SortOrder = input.SortOrder
        };

        context.LookupItems.Add(item);
        await context.SaveChangesAsync();
        return item;
    }
}
