using Microsoft.AspNetCore.Identity;
using GixatBackend.Modules.Users.Enums;
using GixatBackend.Modules.Organizations.Models;

namespace GixatBackend.Modules.Users.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public UserType UserType { get; set; }
    
    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
