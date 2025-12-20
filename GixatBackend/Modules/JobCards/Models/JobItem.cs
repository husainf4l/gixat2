namespace GixatBackend.Modules.JobCards.Models;

public enum JobItemStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public class JobItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }
    
    public string Description { get; set; } = string.Empty;
    public JobItemStatus Status { get; set; } = JobItemStatus.Pending;
    
    public decimal EstimatedCost { get; set; }
    public decimal ActualCost { get; set; }
    
    public string? TechnicianNotes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
