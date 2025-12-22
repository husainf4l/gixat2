using GixatBackend.Modules.Users.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using HotChocolate.Authorization;
using GixatBackend.Data;
using Microsoft.EntityFrameworkCore;
using GixatBackend.Modules.Common.Services;

namespace GixatBackend.Modules.Users.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
[Authorize]
internal static class AuthQueries
{
    public static async Task<ApplicationUser?> GetMeAsync(
        ClaimsPrincipal claimsPrincipal,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);
        ArgumentNullException.ThrowIfNull(context);

        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return null;
        }

        // Query is fast with indexed userId, no need for caching
        var user = await context.Users
            .AsNoTracking()
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId)
            .ConfigureAwait(false);

        return user;
    }
}
