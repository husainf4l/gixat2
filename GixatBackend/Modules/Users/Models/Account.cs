using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Users.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for Identity")]
public sealed class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    /// <summary>
    /// The provider for this account (e.g., "Google", "Microsoft", "GitHub")
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// The provider's unique identifier for this account
    /// </summary>
    public string ProviderAccountId { get; set; } = string.Empty;
    
    /// <summary>
    /// OAuth token type (e.g., "Bearer")
    /// </summary>
    public string? TokenType { get; set; }
    
    /// <summary>
    /// OAuth access token
    /// </summary>
    public string? AccessToken { get; set; }
    
    /// <summary>
    /// When the access token expires
    /// </summary>
    public DateTime? AccessTokenExpiresAt { get; set; }
    
    /// <summary>
    /// OAuth refresh token for obtaining new access tokens
    /// </summary>
    public string? RefreshToken { get; set; }
    
    /// <summary>
    /// When the refresh token expires
    /// </summary>
    public DateTime? RefreshTokenExpiresAt { get; set; }
    
    /// <summary>
    /// OAuth scopes granted
    /// </summary>
    public string? Scope { get; set; }
    
    /// <summary>
    /// OAuth id_token (JWT) from the provider
    /// </summary>
    public string? IdToken { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
