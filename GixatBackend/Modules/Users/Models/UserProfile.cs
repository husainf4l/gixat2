using System.Diagnostics.CodeAnalysis;
using GixatBackend.Modules.Organizations.Models;

namespace GixatBackend.Modules.Users.Models;

/// <summary>
/// User profile information including avatar and bio
/// </summary>
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not used for equality comparisons")]
public record UserProfile
{
    public required string Id { get; init; }
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public required string UserType { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required List<string> Roles { get; init; }
    public Guid? OrganizationId { get; init; }
    public Organization? Organization { get; init; }
}

/// <summary>
/// Input for updating user profile
/// </summary>
public record UpdateProfileInput
{
    public string? FullName { get; init; }
    public string? Bio { get; init; }
    public string? PhoneNumber { get; init; }
}

/// <summary>
/// Result of avatar upload operation
/// </summary>
public record AvatarUploadResult
{
    public required string AvatarUrl { get; init; }
    public required string Message { get; init; }
}
