using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GixatBackend.Data;
using GixatBackend.Modules.Invites.Enums;
using GixatBackend.Modules.Invites.Models;
using GixatBackend.Modules.Users.Enums;
using GixatBackend.Modules.Users.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Users.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by DI")]
internal sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _context = context;
    }

    public async Task<AuthPayload> RegisterAsync(RegisterInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        UserInvite? invite = null;
        if (!string.IsNullOrEmpty(input.InviteCode))
        {
            invite = await _context.UserInvites
                .FirstOrDefaultAsync(i => i.InviteCode == input.InviteCode && i.Status == InviteStatus.Pending && i.ExpiryDate > DateTime.UtcNow)
                .ConfigureAwait(false);

            if (invite == null)
            {
                return new AuthPayload(null, null, "Invalid or expired invite code");
            }
        }

        var user = new ApplicationUser
        {
            UserName = input.Email,
            Email = input.Email,
            FullName = input.FullName,
            UserType = invite != null ? UserType.Organizational : input.UserType,
            OrganizationId = invite?.OrganizationId, // Only set if invited to an organization
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, input.Password).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return new AuthPayload(null, null, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        var roleToAssign = invite != null ? invite.Role : input.Role;

        // Ensure role exists and add user to role
        if (!await _roleManager.RoleExistsAsync(roleToAssign).ConfigureAwait(false))
        {
            await _roleManager.CreateAsync(new IdentityRole(roleToAssign)).ConfigureAwait(false);
        }
        await _userManager.AddToRoleAsync(user, roleToAssign).ConfigureAwait(false);

        if (invite != null)
        {
            invite.Status = InviteStatus.Accepted;
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        var token = await GenerateJwtToken(user).ConfigureAwait(false);

        return new AuthPayload(token, user);
    }

    public async Task<AuthPayload> LoginAsync(LoginInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var user = await _userManager.FindByEmailAsync(input.Email).ConfigureAwait(false);

        if (user == null || !await _userManager.CheckPasswordAsync(user, input.Password).ConfigureAwait(false))
        {
            return new AuthPayload(null, null, "Invalid email or password");
        }

        var token = await GenerateJwtToken(user).ConfigureAwait(false);

        return new AuthPayload(token, user);
    }

    public async Task<AuthPayload> RefreshTokenForUserAsync(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var token = await GenerateJwtToken(user).ConfigureAwait(false);
        return new AuthPayload(token, user);
    }

    private async Task<string> GenerateJwtToken(ApplicationUser user)
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

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var jwtKey = _configuration["Jwt:Key"] 
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["Jwt:ExpireDays"] ?? "7", CultureInfo.InvariantCulture));

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
