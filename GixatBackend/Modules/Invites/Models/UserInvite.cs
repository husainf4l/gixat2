using System.Diagnostics.CodeAnalysis;
using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Invites.Enums;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Users.Models;

namespace GixatBackend.Modules.Invites.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class UserInvite : IMustHaveOrganization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    
    public string InviteCode { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    
    public InviteStatus Status { get; set; } = InviteStatus.Pending;
    
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    public string? InviterId { get; set; }
    public ApplicationUser? Inviter { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
