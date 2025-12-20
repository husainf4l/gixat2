using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace GixatBackend.Modules.Users.GraphQL;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class AuthMutations
{
    public async Task<AuthPayload> RegisterAsync(
        RegisterInput input,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] RoleManager<IdentityRole> roleManager,
        [Service] IConfiguration configuration)
    {
        var user = new ApplicationUser
        {
            UserName = input.Email,
            Email = input.Email,
            FullName = input.FullName,
            UserType = input.UserType,
            OrganizationId = input.OrganizationId,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, input.Password);

        if (!result.Succeeded)
        {
            return new AuthPayload(null, null, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Ensure role exists and add user to role
        if (!await roleManager.RoleExistsAsync(input.Role))
        {
            await roleManager.CreateAsync(new IdentityRole(input.Role));
        }
        await userManager.AddToRoleAsync(user, input.Role);

        var token = await GenerateJwtToken(user, userManager, configuration);
        return new AuthPayload(token, user);
    }

    public async Task<AuthPayload> LoginAsync(
        LoginInput input,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IConfiguration configuration)
    {
        var user = await userManager.FindByEmailAsync(input.Email);

        if (user == null || !await userManager.CheckPasswordAsync(user, input.Password))
        {
            return new AuthPayload(null, null, "Invalid email or password");
        }

        var token = await GenerateJwtToken(user, userManager, configuration);
        return new AuthPayload(token, user);
    }

    private async Task<string> GenerateJwtToken(ApplicationUser user, UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

        if (user.OrganizationId != Guid.Empty)
        {
            claims.Add(new Claim("OrganizationId", user.OrganizationId.ToString()));
        }

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? "YourSuperSecretKeyWithAtLeast32Chars!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.Now.AddDays(Convert.ToDouble(configuration["Jwt:ExpireDays"] ?? "7"));

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
