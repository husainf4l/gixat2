using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Sessions.Enums;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Sessions.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class GarageSession : IMustHaveOrganization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid CarId { get; set; }
    public Car? Car { get; set; }
    
    [Required]
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    [Required]
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    [Required]
    public SessionStatus Status { get; set; } = SessionStatus.CustomerRequest;
    
    // General Customer Requests (can be set at any time)
    [MaxLength(2000)]
    public string? CustomerRequests { get; set; }
    
    // Inspection Phase
    [Range(0, 999999)]
    public int? Mileage { get; set; }
    
    [MaxLength(2000)]
    public string? InspectionNotes { get; set; }
    
    [MaxLength(2000)]
    public string? InspectionRequests { get; set; }
    
    // Test Drive Phase
    [MaxLength(2000)]
    public string? TestDriveNotes { get; set; }
    
    [MaxLength(2000)]
    public string? TestDriveRequests { get; set; }
    
    // Initial Report
    [MaxLength(5000)]
    public string? InitialReport { get; set; }
    
    public ICollection<SessionMedia> Media { get; } = new List<SessionMedia>();
    public ICollection<SessionLog> Logs { get; } = new List<SessionLog>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
