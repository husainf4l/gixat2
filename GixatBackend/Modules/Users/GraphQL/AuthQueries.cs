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
        [Service] ApplicationDbContext context,
        [Service] IRedisCacheService cache)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(cache);

        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return null;
        }

        // Try to get from cache first
        var cacheKey = $"user:{userId}";
        var cachedUser = await cache.GetAsync<ApplicationUser>(cacheKey).ConfigureAwait(false);
        
        if (cachedUser != null)
        {
            return cachedUser;
        }

        // If not in cache, get from database
        var user = await context.Users
            .AsNoTracking()
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId)
            .ConfigureAwait(false);

        // Cache for 5 minutes
        if (user != null)
        {
            await cache.SetAsync(cacheKey, user, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
        }

        return user;
    }
}
