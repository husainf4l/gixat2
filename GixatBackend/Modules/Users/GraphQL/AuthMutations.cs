using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Services;
using GixatBackend.Modules.Users.Enums;
using GixatBackend.Data;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Google.Apis.Auth.OAuth2;

namespace GixatBackend.Modules.Users.GraphQL;

[ExtendObjectType(OperationTypeNames.Mutation)]
internal static class AuthMutations
{
    [AllowAnonymous]
    public static async Task<AuthPayload> RegisterAsync(
        RegisterInput input,
        [Service] IAuthService authService,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var payload = await authService.RegisterAsync(input).ConfigureAwait(false);

        if (payload.Token != null)
        {
            SetAuthCookie(httpContextAccessor, payload.Token);
        }

        return payload;
    }

    [AllowAnonymous]
    public static async Task<AuthPayload> LoginAsync(
        LoginInput input,
        [Service] IAuthService authService,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var payload = await authService.LoginAsync(input).ConfigureAwait(false);

        if (payload.Token != null)
        {
            SetAuthCookie(httpContextAccessor, payload.Token);
        }

        return payload;
    }

    [AllowAnonymous]
    public static async Task<GoogleAuthResponse> LoginWithGoogleAsync(
        string idToken,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuthService authService,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(authService);

#pragma warning disable CA1031
        try
        {
            // Verify the Google ID token with validation settings
            var validationSettings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") 
                    ?? throw new InvalidOperationException("GOOGLE_CLIENT_ID is not configured.") }
            };

            var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings).ConfigureAwait(false);

            if (payload == null)
            {
                return new GoogleAuthResponse 
                { 
                    Success = false, 
                    Message = "Invalid Google token" 
                };
            }

            var email = payload.Email;
            var googleId = payload.Subject;
            var firstName = payload.GivenName ?? "";
            var lastName = payload.FamilyName ?? "";
            var name = payload.Name ?? email;

            // Check if account exists (bypass tenant filter for Google auth)
            var account = await context.Accounts
                .IgnoreQueryFilters()
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Provider == "Google" && a.ProviderAccountId == googleId).ConfigureAwait(false);

            ApplicationUser user;

            if (account != null && account.User != null)
            {
                // Existing user with Google account
                user = account.User;
                
                // Update account info
                account.IdToken = idToken;
                account.UpdatedAt = DateTime.UtcNow;
                
                try
                {
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    return new GoogleAuthResponse 
                    { 
                        Success = false, 
                        Message = $"Failed to update account: {ex.Message}" 
                    };
                }
            }
            else
            {
                // Check if user exists by email (bypass tenant filter for Google auth)
                var existingUser = await context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Email == email)
                    .ConfigureAwait(false);

                if (existingUser == null)
                {
                    // Create new user
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        FullName = name
                    };

                    var createResult = await userManager.CreateAsync(user).ConfigureAwait(false);
                    if (!createResult.Succeeded)
                    {
                        return new GoogleAuthResponse 
                        { 
                            Success = false, 
                            Message = $"Failed to create user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}" 
                        };
                    }
                }
                else
                {
                    user = existingUser;
                }

                // Check if Google account link already exists to prevent duplicates (bypass tenant filter)
                var existingGoogleAccount = await context.Accounts
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(a => a.UserId == user.Id && a.Provider == "Google" && a.ProviderAccountId == googleId)
                    .ConfigureAwait(false);

                if (existingGoogleAccount == null)
                {
                    // Create account link
                    var newAccount = new Account
                    {
                        UserId = user.Id,
                        Provider = "Google",
                        ProviderAccountId = googleId,
                        IdToken = idToken,
                        TokenType = "Bearer",
                        Scope = "profile email openid"
                    };

                    context.Accounts.Add(newAccount);
                    
                    try
                    {
                        await context.SaveChangesAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        return new GoogleAuthResponse 
                        { 
                            Success = false, 
                            Message = $"Failed to create Google account link: {ex.Message}" 
                        };
                    }
                }
                else
                {
                    // Update existing account info
                    existingGoogleAccount.IdToken = idToken;
                    existingGoogleAccount.UpdatedAt = DateTime.UtcNow;
                    
                    try
                    {
                        await context.SaveChangesAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        return new GoogleAuthResponse 
                        { 
                            Success = false, 
                            Message = $"Failed to update Google account: {ex.Message}" 
                        };
                    }
                }
            }

            // Generate JWT token
            var authPayload = await authService.RefreshTokenForUserAsync(user).ConfigureAwait(false);
            var token = authPayload.Token ?? throw new InvalidOperationException("Failed to generate token.");

            // Set cookie
            SetAuthCookie(httpContextAccessor, token);

            return new GoogleAuthResponse
            {
                Success = true,
                Token = token,
                User = AuthUserInfo.FromApplicationUser(user),
                Message = "Login successful"
            };
        }
        catch (Exception ex)
        {
            return new GoogleAuthResponse 
            { 
                Success = false, 
                Message = $"Invalid Google token: {ex.Message}" 
            };
        }
#pragma warning restore CA1031
    }

    [Authorize]
    public static LogoutResponse LogoutAsync(
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        try
        {
            ClearAuthCookie(httpContextAccessor);
            
            return new LogoutResponse
            {
                Success = true,
                Message = "Logout successful"
            };
        }
        catch (Exception ex)
        {
            return new LogoutResponse
            {
                Success = false,
                Message = $"Logout failed: {ex.Message}"
            };
        }
    }

    private static void SetAuthCookie(IHttpContextAccessor httpContextAccessor, string token)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None, // Required for cross-origin if frontend is on different port/domain
                Expires = DateTime.UtcNow.AddDays(7)
            });
        }
    }

    private static void ClearAuthCookie(IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Response.Cookies.Append("access_token", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(-1) // Set to past date to expire the cookie
            });
        }
    }
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class GoogleAuthResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public AuthUserInfo? User { get; set; }
    public string? Message { get; set; }
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class AuthUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? AvatarS3Key { get; set; }
    public Guid? OrganizationId { get; set; }
    public UserType UserType { get; set; }
    
    public static AuthUserInfo FromApplicationUser(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        
        return new AuthUserInfo
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            AvatarS3Key = user.AvatarS3Key,
            OrganizationId = user.OrganizationId,
            UserType = user.UserType
        };
    }
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class LogoutResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
