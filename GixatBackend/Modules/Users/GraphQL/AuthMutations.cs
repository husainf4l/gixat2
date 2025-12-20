using System.Security.Claims;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Services;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;

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
