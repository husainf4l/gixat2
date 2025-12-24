using Microsoft.AspNetCore.Identity;
using GixatBackend.Modules.Users.Enums;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Common.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Users.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class ApplicationUser : IdentityUser, IMustHaveOrganization
{
    [MaxLength(200)]
    public string? FullName { get; set; }
    
    [MaxLength(500)]
    public string? AvatarS3Key { get; set; }
    
    [MaxLength(1000)]
    public string? Bio { get; set; }
    
    [Required]
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
