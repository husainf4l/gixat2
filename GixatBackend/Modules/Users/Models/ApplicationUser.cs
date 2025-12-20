using Microsoft.AspNetCore.Identity;
using GixatBackend.Modules.Users.Enums;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Common.Models;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Users.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class ApplicationUser : IdentityUser, IMustHaveOrganization
{
    public string? FullName { get; set; }
    public UserType UserType { get; set; }
    
    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    Guid IMustHaveOrganization.OrganizationId 
    { 
        get => OrganizationId ?? Guid.Empty; 
        set => OrganizationId = value; 
    }
}
