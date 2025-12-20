using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Media.Models;
using GixatBackend.Data;

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
        var address = new Address
        {
            Country = input.Country,
            City = input.City,
            Street = input.Street,
            PhoneCountryCode = input.PhoneCountryCode
        };

        Media.Models.Media? logo = null;
        if (!string.IsNullOrEmpty(input.LogoUrl))
        {
            logo = new Media.Models.Media
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
        await context.SaveChangesAsync();

        return organization;
    }

    public async Task<bool> AssignUserToOrganizationAsync(
        Guid userId,
        Guid organizationId,
        [Service] ApplicationDbContext context)
    {
        var user = await context.Users.FindAsync(userId.ToString());
        if (user == null) return false;

        user.OrganizationId = organizationId;
        await context.SaveChangesAsync();

        return true;
    }
}
