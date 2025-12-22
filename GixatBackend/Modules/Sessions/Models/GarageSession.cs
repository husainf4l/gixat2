using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Sessions.Enums;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Sessions.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class GarageSession : IMustHaveOrganization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid CarId { get; set; }
    public Car? Car { get; set; }
    
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    public SessionStatus Status { get; set; } = SessionStatus.Intake;
    
    // General Customer Requests (can be set at any time)
    public string? CustomerRequests { get; set; }
    
    // Intake Phase
    public string? IntakeNotes { get; set; }
    public string? IntakeRequests { get; set; }
    
    // Inspection Phase
    public string? InspectionNotes { get; set; }
    public string? InspectionRequests { get; set; }
    
    // Test Drive Phase
    public string? TestDriveNotes { get; set; }
    public string? TestDriveRequests { get; set; }
    
    // Initial Report
    public string? InitialReport { get; set; }
    
    public ICollection<SessionMedia> Media { get; } = new List<SessionMedia>();
    public ICollection<SessionLog> Logs { get; } = new List<SessionLog>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
