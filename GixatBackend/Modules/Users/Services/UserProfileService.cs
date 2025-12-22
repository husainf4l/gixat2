using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Common.Services.AWS;
using GixatBackend.Modules.Common.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Users.Services;

/// <summary>
/// Service for managing user profile operations including avatar uploads
/// </summary>
[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required for GraphQL resolvers")]
public sealed class UserProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IS3Service _s3Service;
    private const int MaxAvatarSizeMB = 5;
    private const int MaxAvatarSizeBytes = MaxAvatarSizeMB * 1024 * 1024;
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/jpg", "image/png", "image/webp"];
    private const string AvatarS3Prefix = "avatars/";

    public UserProfileService(UserManager<ApplicationUser> userManager, IS3Service s3Service)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _s3Service = s3Service ?? throw new ArgumentNullException(nameof(s3Service));
    }

    /// <summary>
    /// Get user profile by user ID
    /// </summary>
    public async Task<UserProfile?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .Include(u => u.Organization)
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);

        return new UserProfile
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            UserType = user.UserType.ToString(),
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList(),
            OrganizationId = user.OrganizationId,
            Organization = user.Organization
        };
    }

    /// <summary>
    /// Update user profile information
    /// </summary>
    public async Task<UserProfile> UpdateProfileAsync(string userId, UpdateProfileInput input, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException("User", userId);

        if (input.FullName != null)
        {
            user.FullName = input.FullName.Trim();
        }

        if (input.Bio != null)
        {
            user.Bio = input.Bio.Trim();
        }

        if (input.PhoneNumber != null)
        {
            user.PhoneNumber = input.PhoneNumber.Trim();
        }

        var result = await _userManager.UpdateAsync(user).ConfigureAwait(false);
        
        if (!result.Succeeded)
        {
            throw new BusinessRuleViolationException(
                "ProfileUpdateFailed", 
                $"Failed to update profile: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var updatedUser = await _userManager.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        return new UserProfile
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            UserType = user.UserType.ToString(),
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList(),
            OrganizationId = updatedUser?.OrganizationId,
            Organization = updatedUser?.Organization
        };
    }

    /// <summary>
    /// Upload user avatar to S3 with validation
    /// </summary>
    public async Task<AvatarUploadResult> UploadAvatarAsync(
        string userId, 
        Stream fileStream, 
        string fileName, 
        string contentType, 
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        // Validate file size
        if (fileSize > MaxAvatarSizeBytes)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["file"] = [$"File size must not exceed {MaxAvatarSizeMB}MB. Current size: {fileSize / 1024.0 / 1024.0:F2}MB"]
                });
        }

        // Validate content type
        if (!AllowedImageTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["file"] = [$"Invalid file type. Allowed types: {string.Join(", ", AllowedImageTypes)}"]
                });
        }

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException("User", userId);

        // Delete old avatar if exists
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            try
            {
                // Extract S3 key from URL (assuming format: https://bucket.s3.region.amazonaws.com/key)
                var oldKey = ExtractS3KeyFromUrl(user.AvatarUrl);
                if (!string.IsNullOrEmpty(oldKey))
                {
                    await _s3Service.DeleteFileAsync(oldKey).ConfigureAwait(false);
                }
            }
            catch
            {
                // Log error but continue - don't fail upload if old file deletion fails
            }
        }

        // Upload new avatar with user-specific path
        var sanitizedFileName = SanitizeFileName(fileName);
        var s3Key = $"{AvatarS3Prefix}{userId}/{Guid.NewGuid()}-{sanitizedFileName}";
        
        var uploadedKey = await _s3Service.UploadFileAsync(fileStream, s3Key, contentType)
            .ConfigureAwait(false);

        // Get presigned URL for the avatar (24 hours expiry)
        var avatarUrl = _s3Service.GetFileUrl(uploadedKey).ToString();

        // Update user avatar URL
        user.AvatarUrl = avatarUrl;
        var result = await _userManager.UpdateAsync(user).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            // Cleanup uploaded file if user update fails
            try
            {
                await _s3Service.DeleteFileAsync(uploadedKey).ConfigureAwait(false);
            }
            catch
            {
                // Log error
            }

            throw new BusinessRuleViolationException(
                "AvatarUploadFailed",
                $"Failed to save avatar: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return new AvatarUploadResult
        {
            AvatarUrl = avatarUrl,
            Message = "Avatar uploaded successfully"
        };
    }

    /// <summary>
    /// Delete user avatar
    /// </summary>
    public async Task<bool> DeleteAvatarAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException("User", userId);

        if (string.IsNullOrEmpty(user.AvatarUrl))
        {
            return false; // No avatar to delete
        }

        // Extract S3 key from URL
        var s3Key = ExtractS3KeyFromUrl(user.AvatarUrl);
        if (!string.IsNullOrEmpty(s3Key))
        {
            await _s3Service.DeleteFileAsync(s3Key).ConfigureAwait(false);
        }

        user.AvatarUrl = null;
        var result = await _userManager.UpdateAsync(user).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            throw new BusinessRuleViolationException(
                "AvatarDeletionFailed",
                $"Failed to delete avatar: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return true;
    }

    /// <summary>
    /// Generate presigned URL for direct avatar upload to S3
    /// </summary>
    public async Task<string> GenerateAvatarUploadUrlAsync(
        string userId, 
        string fileName, 
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // Validate content type
        if (!AllowedImageTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["contentType"] = [$"Invalid file type. Allowed types: {string.Join(", ", AllowedImageTypes)}"]
                });
        }

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException("User", userId);

        var sanitizedFileName = SanitizeFileName(fileName);
        var s3Key = $"{AvatarS3Prefix}{userId}/{Guid.NewGuid()}-{sanitizedFileName}";

        // Generate presigned URL valid for 15 minutes
        var presignedUrl = await _s3Service.GeneratePresignedUploadUrlAsync(s3Key, contentType, 15)
            .ConfigureAwait(false);

        return presignedUrl;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 100 ? sanitized[..100] : sanitized;
    }

    private static string? ExtractS3KeyFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            // Remove leading slash
            return uri.AbsolutePath.TrimStart('/');
        }
        catch
        {
            return null;
        }
    }
}
