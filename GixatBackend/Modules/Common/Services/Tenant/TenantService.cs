using System.Security.Claims;

using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Common.Services.Tenant;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by DI")]
internal sealed class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? OrganizationId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("OrganizationId")?.Value;
            if (Guid.TryParse(claim, out var organizationId))
            {
                return organizationId;
            }
            return null;
        }
    }
}
