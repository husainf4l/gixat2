using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.Organizations.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
public class OrganizationQueries
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Organization> GetOrganizations([Service] ApplicationDbContext context)
        => context.Organizations;

    public async Task<Organization?> GetOrganizationByIdAsync(Guid id, [Service] ApplicationDbContext context)
        => await context.Organizations
            .Include(o => o.Address)
            .Include(o => o.Logo)
            .FirstOrDefaultAsync(o => o.Id == id);
}
