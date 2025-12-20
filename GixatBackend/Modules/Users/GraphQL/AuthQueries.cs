using GixatBackend.Modules.Users.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Users.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
[Authorize]
internal static class AuthQueries
{
    public static async Task<ApplicationUser?> GetMeAsync(
        ClaimsPrincipal claimsPrincipal,
        [Service] UserManager<ApplicationUser> userManager)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);
        ArgumentNullException.ThrowIfNull(userManager);

        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return null;
        }
        return await userManager.FindByIdAsync(userId).ConfigureAwait(false);
    }
}
