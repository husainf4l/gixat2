using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Sessions.Enums;

namespace GixatBackend.Modules.Sessions.Models;

public enum SessionStage
{
    Intake = 0,
    Inspection = 1,
    TestDrive = 2,
    General = 3
}

public class SessionMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid SessionId { get; set; }
    public GarageSession? Session { get; set; }
    
    public Guid MediaId { get; set; }
    public Media? Media { get; set; }
    
    public SessionStage Stage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
