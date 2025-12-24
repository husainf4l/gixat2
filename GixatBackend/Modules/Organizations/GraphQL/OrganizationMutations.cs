using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Common.Services;
using GixatBackend.Modules.Common.Services.Tenant;
using GixatBackend.Data;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Authorization;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Services;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Amazon.S3;

namespace GixatBackend.Modules.Organizations.GraphQL;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required by HotChocolate for schema discovery")]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by HotChocolate")]
public sealed record AddressInput(
    string Country, 
    string City, 
    string Street, 
    string PhoneCountryCode);

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required by HotChocolate for schema discovery")]
public sealed record CreateOrganizationInput(
    string Name, 
    AddressInput Address,
    Uri? LogoUrl,
    string? LogoAlt);

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required by HotChocolate for schema discovery")]
public sealed record UpdateOrganizationInput(
    string? Name,
    AddressInput? Address);

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required by HotChocolate for schema discovery")]
public sealed record LogoUploadResult(
    string LogoUrl,
    string Message);

[ExtendObjectType(OperationTypeNames.Mutation)]
[Authorize]
internal static class OrganizationMutations
{
    public static async Task<AuthPayload> CreateOrganizationAsync(
        CreateOrganizationInput input,
        ClaimsPrincipal claimsPrincipal,
        [Service] ApplicationDbContext context,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IAuthService authService,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(claimsPrincipal);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(authService);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("Not authorized");
        }

#pragma warning disable CA2007
        await using var transaction = await context.Database.BeginTransactionAsync().ConfigureAwait(false);
#pragma warning restore CA2007

        var address = new Address
        {
            Country = input.Address.Country,
            City = input.Address.City,
            Street = input.Address.Street,
            PhoneCountryCode = input.Address.PhoneCountryCode
        };

        AppMedia? logo = null;
        if (input.LogoUrl != null)
        {
            logo = new AppMedia
            {
                Url = input.LogoUrl,
                Alt = input.LogoAlt,
                Type = MediaType.Image
            };
        }

        var organization = new Organization
        {
            Name = input.Name,
            Address = address,
            Logo = logo
        };

        context.Organizations.Add(organization);
        await context.SaveChangesAsync().ConfigureAwait(false);

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId).ConfigureAwait(false) ?? throw new InvalidOperationException("User not found");

        if (user.OrganizationId.HasValue)
        {
            throw new InvalidOperationException("User already belongs to an organization");
        }

        user.OrganizationId = organization.Id;
        await context.SaveChangesAsync().ConfigureAwait(false);

        await transaction.CommitAsync().ConfigureAwait(false);

        // Generate fresh auth payload with updated JWT token containing OrganizationId
        var authPayload = await authService.RefreshTokenForUserAsync(user).ConfigureAwait(false);
        
        // Automatically update the cookie with the new token
        if (authPayload.Token != null && httpContextAccessor.HttpContext != null)
        {
            httpContextAccessor.HttpContext.Response.Cookies.Append("access_token", authPayload.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)
            });
        }

        return authPayload;
    }

    public static async Task<bool> AssignUserToOrganizationAsync(
        Guid userId,
        Guid organizationId,
        [Service] ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var user = await context.Users.FindAsync(userId.ToString()).ConfigureAwait(false);
        if (user == null)
        {
            return false;
        }

        user.OrganizationId = organizationId;
        await context.SaveChangesAsync().ConfigureAwait(false);

        return true;
    }

    public static async Task<Organization> UpdateMyOrganizationAsync(
        UpdateOrganizationInput input,
        [Service] ApplicationDbContext context,
        [Service] ITenantService tenantService)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tenantService);
        ArgumentNullException.ThrowIfNull(input);

        var orgId = tenantService.OrganizationId;
        if (!orgId.HasValue)
        {
            throw new InvalidOperationException("Not authorized");
        }

        var organization = await context.Organizations
            .Include(o => o.Address)
            .Include(o => o.Logo)
            .FirstOrDefaultAsync(o => o.Id == orgId.Value)
            .ConfigureAwait(false);
            
        if (organization == null)
        {
            throw new InvalidOperationException("Organization not found");
        }

        // Update name if provided
        if (!string.IsNullOrWhiteSpace(input.Name))
        {
            organization.Name = input.Name;
        }

        // Update address if provided
        if (input.Address != null)
        {
            if (organization.Address == null)
            {
                organization.Address = new Address();
            }
            
            organization.Address.Country = input.Address.Country;
            organization.Address.City = input.Address.City;
            organization.Address.Street = input.Address.Street;
            organization.Address.PhoneCountryCode = input.Address.PhoneCountryCode;
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        return organization;
    }

    /// <summary>
    /// Upload logo for organization
    /// </summary>
    public static async Task<LogoUploadResult> UploadMyOrganizationLogoAsync(
        IFile file,
        [Service] ApplicationDbContext context,
        [Service] ITenantService tenantService,
        [Service] GixatBackend.Modules.Common.Services.AWS.IS3Service s3Service,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tenantService);
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(s3Service);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        var orgId = tenantService.OrganizationId;
        if (!orgId.HasValue)
        {
            throw new InvalidOperationException("Not authorized");
        }

        var organization = await context.Organizations
            .Include(o => o.Logo)
            .FirstOrDefaultAsync(o => o.Id == orgId.Value)
            .ConfigureAwait(false);
            
        if (organization == null)
        {
            throw new InvalidOperationException("Organization not found");
        }

        // Validate file
        const int maxSizeMB = 5;
        const int maxSizeBytes = maxSizeMB * 1024 * 1024;
        
        if (file.Length > maxSizeBytes)
        {
            throw new InvalidOperationException($"File size must not exceed {maxSizeMB}MB");
        }

        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        var contentType = file.ContentType?.ToLowerInvariant() ?? "image/jpeg";
        
        if (!allowedTypes.Contains(contentType))
        {
            throw new InvalidOperationException($"Invalid file type. Allowed: {string.Join(", ", allowedTypes)}");
        }

        // Delete old logo if exists
        if (organization.Logo != null && !string.IsNullOrEmpty(organization.Logo.Url?.ToString()))
        {
            try
            {
                var oldKey = organization.Logo.Url.ToString().Split('/').Last();
                if (oldKey.StartsWith("logos/", StringComparison.OrdinalIgnoreCase))
                {
                    await s3Service.DeleteFileAsync(oldKey).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is AmazonS3Exception || ex is HttpRequestException)
            {
                // Log but continue - old logo cleanup failure shouldn't block upload
            }
        }

        // Upload new logo
        await using var stream = file.OpenReadStream();
        var sanitizedFileName = SanitizeFileName(file.Name);
        var s3Key = $"logos/{orgId.Value}/{Guid.NewGuid()}-{sanitizedFileName}";
        
        var uploadedKey = await s3Service.UploadFileAsync(stream, s3Key, contentType).ConfigureAwait(false);

        // Generate API URL
        var fileName = uploadedKey.Split('/').Last();
        var logoUrl = GetLogoUrl(httpContextAccessor, orgId.Value.ToString(), fileName);

        // Update or create logo entity
        if (organization.Logo == null)
        {
            organization.Logo = new AppMedia
            {
                Url = new Uri(logoUrl),
                Type = MediaType.Image
            };
        }
        else
        {
            organization.Logo.Url = new Uri(logoUrl);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);

        return new LogoUploadResult(logoUrl, "Logo uploaded successfully");
    }

    /// <summary>
    /// Delete organization logo
    /// </summary>
    public static async Task<bool> DeleteMyOrganizationLogoAsync(
        [Service] ApplicationDbContext context,
        [Service] ITenantService tenantService,
        [Service] GixatBackend.Modules.Common.Services.AWS.IS3Service s3Service)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tenantService);
        ArgumentNullException.ThrowIfNull(s3Service);

        var orgId = tenantService.OrganizationId;
        if (!orgId.HasValue)
        {
            throw new InvalidOperationException("Not authorized");
        }

        var organization = await context.Organizations
            .Include(o => o.Logo)
            .FirstOrDefaultAsync(o => o.Id == orgId.Value)
            .ConfigureAwait(false);
            
        if (organization == null)
        {
            throw new InvalidOperationException("Organization not found");
        }

        if (organization.Logo == null || organization.Logo.Url == null)
        {
            return false;
        }

        // Delete from S3
        try
        {
            var s3Key = organization.Logo.Url.ToString().Split('/').Last();
            if (s3Key.StartsWith("logos/", StringComparison.OrdinalIgnoreCase))
            {
                await s3Service.DeleteFileAsync(s3Key).ConfigureAwait(false);
            }
        }
        catch
        {
            // Log but continue
        }

        // Remove logo reference
        context.Medias.Remove(organization.Logo);
        organization.LogoId = null;
        organization.Logo = null;

        await context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 100 ? sanitized[..100] : sanitized;
    }

    private static string GetLogoUrl(IHttpContextAccessor httpContextAccessor, string orgId, string fileName)
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            return $"/api/media/logos/{orgId}/{fileName}";
        }

        var scheme = request.Scheme;
        var host = request.Host.ToString();
        return $"{scheme}://{host}/api/media/logos/{orgId}/{fileName}";
    }
}
