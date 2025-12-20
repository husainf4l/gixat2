using System.Security.Claims;

namespace GixatBackend.Modules.Common.Services;

public class TenantService : ITenantService
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
