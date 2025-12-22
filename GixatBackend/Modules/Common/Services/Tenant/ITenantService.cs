using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Common.Services.Tenant;

[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "Used by ApplicationDbContext and GraphQL queries via dependency injection")]
public interface ITenantService
{
    Guid? OrganizationId { get; }
}
