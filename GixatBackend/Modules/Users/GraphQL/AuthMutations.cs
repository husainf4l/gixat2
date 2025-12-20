using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Globalization;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;

namespace GixatBackend.Modules.Users.GraphQL;

[ExtendObjectType(OperationTypeNames.Mutation)]
internal static class AuthMutations
{
    [AllowAnonymous]
    public static async Task<AuthPayload> RegisterAsync(
        RegisterInput input,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] RoleManager<IdentityRole> roleManager,
        [Service] IConfiguration configuration,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(roleManager);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        var user = new ApplicationUser
        {
            UserName = input.Email,
            Email = input.Email,
            FullName = input.FullName,
            UserType = input.UserType,
            OrganizationId = input.OrganizationId,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, input.Password).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return new AuthPayload(null, null, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Ensure role exists and add user to role
        if (!await roleManager.RoleExistsAsync(input.Role).ConfigureAwait(false))
        {
            await roleManager.CreateAsync(new IdentityRole(input.Role)).ConfigureAwait(false);
        }
        await userManager.AddToRoleAsync(user, input.Role).ConfigureAwait(false);

        var token = await GenerateJwtToken(user, userManager, configuration).ConfigureAwait(false);

        // Set HTTP-only cookie for web clients
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

        return new AuthPayload(token, user);
    }

    [AllowAnonymous]
    public static async Task<AuthPayload> LoginAsync(
        LoginInput input,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IConfiguration configuration,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        var user = await userManager.FindByEmailAsync(input.Email).ConfigureAwait(false);

        if (user == null || !await userManager.CheckPasswordAsync(user, input.Password).ConfigureAwait(false))
        {
            return new AuthPayload(null, null, "Invalid email or password");
        }

        var token = await GenerateJwtToken(user, userManager, configuration).ConfigureAwait(false);

        // Set HTTP-only cookie for web clients
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None, // Required for cross-origin
                Expires = DateTime.UtcNow.AddDays(7)
            });
        }

        return new AuthPayload(token, user);
    }

    private static async Task<string> GenerateJwtToken(ApplicationUser user, UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

        if (user.OrganizationId.HasValue)
        {
            claims.Add(new Claim("OrganizationId", user.OrganizationId.Value.ToString(null, CultureInfo.InvariantCulture)));
        }

        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? "YourSuperSecretKeyWithAtLeast32Chars!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.Now.AddDays(Convert.ToDouble(configuration["Jwt:ExpireDays"] ?? "7", CultureInfo.InvariantCulture));

        var token = new JwtSecurityToken(
            configuration["Jwt:Issuer"],
            configuration["Jwt:Audience"],
            claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
