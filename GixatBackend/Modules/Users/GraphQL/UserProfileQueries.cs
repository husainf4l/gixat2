using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Services;
using HotChocolate.Authorization;
using System.Security.Claims;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Users.GraphQL;

/// <summary>
/// GraphQL queries for user profile
/// </summary>
[ExtendObjectType(OperationTypeNames.Query)]
[Authorize]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "HotChocolate resolvers cannot be static")]
internal sealed class UserProfileQueries
{
    /// <summary>
    /// Get current authenticated user's profile
    /// </summary>
    [Authorize]
    public static async Task<UserProfile?> MeAsync(
        ClaimsPrincipal claimsPrincipal,
        [Service] UserProfileService profileService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profileService);
        
        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        return await profileService.GetUserProfileAsync(userId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get user profile by ID (admin only)
    /// </summary>
    [Authorize(Roles = ["Admin"])]
    public static async Task<UserProfile?> UserProfileByIdAsync(
        string userId,
        [Service] UserProfileService profileService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profileService);
        return await profileService.GetUserProfileAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
