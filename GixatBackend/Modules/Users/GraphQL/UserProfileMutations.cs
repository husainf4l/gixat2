using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Services;
using HotChocolate.Authorization;
using System.Security.Claims;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Users.GraphQL;

/// <summary>
/// GraphQL mutations for user profile management
/// </summary>
[ExtendObjectType(OperationTypeNames.Mutation)]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "HotChocolate resolvers cannot be static")]
internal static class UserProfileMutations
{
    /// <summary>
    /// Update current user's profile
    /// </summary>
    [Authorize]
    public static async Task<UserProfile> UpdateMyProfileAsync(
        UpdateProfileInput input,
        ClaimsPrincipal claimsPrincipal,
        [Service] UserProfileService profileService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profileService);
        ArgumentNullException.ThrowIfNull(input);
        
        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        return await profileService.UpdateProfileAsync(userId, input, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Upload avatar for current user
    /// Note: This requires using multipart/form-data with file upload
    /// </summary>
    [Authorize]
    public static async Task<AvatarUploadResult> UploadMyAvatarAsync(
        IFile file,
        ClaimsPrincipal claimsPrincipal,
        [Service] UserProfileService profileService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profileService);
        ArgumentNullException.ThrowIfNull(file);
        
        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        await using var stream = file.OpenReadStream();
        
        return await profileService.UploadAvatarAsync(
            userId,
            stream,
            file.Name,
            file.ContentType ?? "image/jpeg",
            file.Length ?? 0,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Delete current user's avatar
    /// </summary>
    [Authorize]
    public static async Task<bool> DeleteMyAvatarAsync(
        ClaimsPrincipal claimsPrincipal,
        [Service] UserProfileService profileService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profileService);
        
        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        return await profileService.DeleteAvatarAsync(userId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Generate presigned URL for direct avatar upload to S3 (client-side upload)
    /// This is the recommended approach for larger files or mobile apps
    /// </summary>
    [Authorize]
    public static async Task<string> GenerateAvatarUploadUrlAsync(
        string fileName,
        string contentType,
        ClaimsPrincipal claimsPrincipal,
        [Service] UserProfileService profileService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profileService);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        
        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        return await profileService.GenerateAvatarUploadUrlAsync(userId, fileName, contentType, cancellationToken)
            .ConfigureAwait(false);
    }
}
