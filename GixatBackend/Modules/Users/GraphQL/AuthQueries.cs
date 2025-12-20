using GixatBackend.Modules.Users.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Users.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
public class AuthQueries
{
    public async Task<ApplicationUser?> GetMeAsync(
        ClaimsPrincipal claimsPrincipal,
        [Service] UserManager<ApplicationUser> userManager)
    {
        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return null;
        return await userManager.FindByIdAsync(userId);
    }
}
