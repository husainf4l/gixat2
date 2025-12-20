using GixatBackend.Modules.Sessions.Enums;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Sessions.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class SessionLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid SessionId { get; set; }
    public GarageSession? Session { get; set; }
    
    public SessionStatus FromStatus { get; set; }
    public SessionStatus ToStatus { get; set; }
    
    public string? Notes { get; set; }
    public string? ChangedByUserId { get; set; } // Optional: link to ApplicationUser if needed
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
