using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Data;
using GixatBackend.Modules.Common.Services;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Organizations.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
[Authorize]
internal static class OrganizationQueries
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Organization> GetOrganizations([Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Organizations;
    }

    public static async Task<Organization?> GetMyOrganizationAsync(
        [Service] ApplicationDbContext context,
        [Service] ITenantService tenantService)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tenantService);

        var orgId = tenantService.OrganizationId;
        if (!orgId.HasValue)
        {
            return null;
        }

        return await context.Organizations
            .Include(o => o.Address)
            .Include(o => o.Logo)
            .FirstOrDefaultAsync(o => o.Id == orgId.Value).ConfigureAwait(false);
    }

    public static async Task<Organization?> GetOrganizationByIdAsync(Guid id, [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await context.Organizations
            .Include(o => o.Address)
            .Include(o => o.Logo)
            .FirstOrDefaultAsync(o => o.Id == id).ConfigureAwait(false);
    }
}
