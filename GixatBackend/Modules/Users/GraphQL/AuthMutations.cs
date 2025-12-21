using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Services;
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

            // Check if account exists
            var account = await context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Provider == "Google" && a.ProviderAccountId == googleId).ConfigureAwait(false);

            ApplicationUser user;

            if (account != null && account.User != null)
            {
                // Existing user
                user = account.User;
                
                // Update account info
                account.IdToken = idToken;
                account.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
            else
            {
                // Check if user exists by email
                var existingUser = await userManager.FindByEmailAsync(email).ConfigureAwait(false);

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
                }                // Create account link
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
                await context.SaveChangesAsync().ConfigureAwait(false);
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
                User = user,
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
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class GoogleAuthResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public ApplicationUser? User { get; set; }
    public string? Message { get; set; }
}
