using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Sessions.Models;

namespace GixatBackend.Modules.JobCards.Models;

public enum JobCardStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public class JobCard : IMustHaveOrganization
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
    
    public ICollection<JobItem> Items { get; set; } = new List<JobItem>();
    
    public decimal TotalEstimatedCost { get; set; }
    public decimal TotalActualCost { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
