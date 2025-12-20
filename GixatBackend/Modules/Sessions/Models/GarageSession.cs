using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Sessions.Enums;

namespace GixatBackend.Modules.Sessions.Models;

public class GarageSession : IMustHaveOrganization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid CarId { get; set; }
    public Car? Car { get; set; }
    
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    public SessionStatus Status { get; set; } = SessionStatus.Intake;
    
    // Intake Phase
    public string? IntakeNotes { get; set; }
    public string? CustomerRequests { get; set; }
    
    // Inspection Phase
    public string? InspectionNotes { get; set; }
    
    // Test Drive Phase
    public string? TestDriveNotes { get; set; }
    
    // Initial Report
    public string? InitialReport { get; set; }
    
    public ICollection<SessionMedia> Media { get; set; } = new List<SessionMedia>();
    public ICollection<SessionLog> Logs { get; set; } = new List<SessionLog>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
