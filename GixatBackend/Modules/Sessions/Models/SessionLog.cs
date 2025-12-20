using GixatBackend.Modules.Sessions.Enums;

namespace GixatBackend.Modules.Sessions.Models;

public class SessionLog
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
