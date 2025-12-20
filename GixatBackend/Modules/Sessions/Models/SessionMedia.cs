using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Sessions.Enums;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Sessions.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public enum SessionStage
{
    Intake = 0,
    Inspection = 1,
    TestDrive = 2,
    General = 3
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class SessionMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid SessionId { get; set; }
    public GarageSession? Session { get; set; }
    
    public Guid MediaId { get; set; }
    public AppMedia? Media { get; set; }
    
    public SessionStage Stage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
