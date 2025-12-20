using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Common.Services;
using GixatBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.Organizations.GraphQL;

public record CreateOrganizationInput(
    string Name, 
    string Country, 
    string City, 
    string Street, 
    string PhoneCountryCode,
    string? LogoUrl,
    string? LogoAlt);

[ExtendObjectType(OperationTypeNames.Mutation)]
public class OrganizationMutations
{
    public async Task<Organization> CreateOrganizationAsync(
        CreateOrganizationInput input,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        var address = new Address
        {
            Country = input.Country,
            City = input.City,
            Street = input.Street,
            PhoneCountryCode = input.PhoneCountryCode
        };

        Media? logo = null;
        if (!string.IsNullOrEmpty(input.LogoUrl))
        {
            logo = new Media
            {
                Url = input.LogoUrl,
                Alt = input.LogoAlt,
                Type = MediaType.Image
            };
        }

        var organization = new Organization
        {
            Name = input.Name,
            Address = address,
            Logo = logo
        };

        context.Organizations.Add(organization);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return organization;
    }

    public async Task<bool> AssignUserToOrganizationAsync(
        Guid userId,
        Guid organizationId,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var user = await context.Users.FindAsync(userId.ToString()).ConfigureAwait(false);
        if (user == null)
        {
            return false;
        }

        user.OrganizationId = organizationId;
        await context.SaveChangesAsync().ConfigureAwait(false);

        return true;
    }

    public async Task<Organization> UpdateMyOrganizationAsync(
        string name,
        [Service] ApplicationDbContext context,
        [Service] ITenantService tenantService)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tenantService);

        var orgId = tenantService.OrganizationId;
        if (!orgId.HasValue)
        {
            throw new InvalidOperationException("Not authorized");
        }

        var organization = await context.Organizations.FindAsync(orgId.Value).ConfigureAwait(false);
        if (organization == null)
        {
            throw new InvalidOperationException("Organization not found");
        }

        organization.Name = name;
        await context.SaveChangesAsync().ConfigureAwait(false);
        return organization;
    }
}
