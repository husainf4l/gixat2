using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Sessions.Models;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.JobCards.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public enum JobCardStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class JobCard : IMustHaveOrganization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid? SessionId { get; set; }
    public GarageSession? Session { get; set; }
    
    public Guid CarId { get; set; }
    public Car? Car { get; set; }
    
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    public JobCardStatus Status { get; set; } = JobCardStatus.Pending;
    
    public string? InternalNotes { get; set; }
    
    public ICollection<JobItem> Items { get; } = new List<JobItem>();
    
    public decimal TotalEstimatedCost { get; set; }
    public decimal TotalActualCost { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
