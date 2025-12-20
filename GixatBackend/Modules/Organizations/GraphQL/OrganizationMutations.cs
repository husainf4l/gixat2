using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Common.Services;
using GixatBackend.Data;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Authorization;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Services;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace GixatBackend.Modules.Organizations.GraphQL;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required by HotChocolate for schema discovery")]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by HotChocolate")]
public sealed record AddressInput(
    string Country, 
    string City, 
    string Street, 
    string PhoneCountryCode);

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required by HotChocolate for schema discovery")]
public sealed record CreateOrganizationInput(
    string Name, 
    AddressInput Address,
    Uri? LogoUrl,
    string? LogoAlt);

[ExtendObjectType(OperationTypeNames.Mutation)]
[Authorize]
internal static class OrganizationMutations
{
    public static async Task<AuthPayload> CreateOrganizationAsync(
        CreateOrganizationInput input,
        ClaimsPrincipal claimsPrincipal,
        [Service] ApplicationDbContext context,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IAuthService authService,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(claimsPrincipal);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(authService);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("Not authorized");
        }

        await using var transaction = await context.Database.BeginTransactionAsync().ConfigureAwait(false);

        var address = new Address
        {
            Country = input.Address.Country,
            City = input.Address.City,
            Street = input.Address.Street,
            PhoneCountryCode = input.Address.PhoneCountryCode
        };

        AppMedia? logo = null;
        if (input.LogoUrl != null)
        {
            logo = new AppMedia
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

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId).ConfigureAwait(false) ?? throw new InvalidOperationException("User not found");

        if (user.OrganizationId.HasValue)
        {
            throw new InvalidOperationException("User already belongs to an organization");
        }

        user.OrganizationId = organization.Id;
        await context.SaveChangesAsync().ConfigureAwait(false);

        await transaction.CommitAsync().ConfigureAwait(false);

        // Generate fresh auth payload with updated JWT token containing OrganizationId
        var authPayload = await authService.RefreshTokenForUserAsync(user).ConfigureAwait(false);
        
        // Automatically update the cookie with the new token
        if (authPayload.Token != null && httpContextAccessor.HttpContext != null)
        {
            httpContextAccessor.HttpContext.Response.Cookies.Append("access_token", authPayload.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)
            });
        }

        return authPayload;
    }

    public static async Task<bool> AssignUserToOrganizationAsync(
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

    public static async Task<Organization> UpdateMyOrganizationAsync(
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
