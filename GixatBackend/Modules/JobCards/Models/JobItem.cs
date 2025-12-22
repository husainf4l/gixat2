using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.JobCards.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public enum JobItemStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class JobItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }
    
    [MaxLength(450)]
    public string? AssignedTechnicianId { get; set; }
    public GixatBackend.Modules.Users.Models.ApplicationUser? AssignedTechnician { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public JobItemStatus Status { get; set; } = JobItemStatus.Pending;
    
    [Range(0, double.MaxValue)]
    public decimal EstimatedLaborCost { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal EstimatedPartsCost { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal ActualLaborCost { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal ActualPartsCost { get; set; }
    
    public decimal EstimatedCost => EstimatedLaborCost + EstimatedPartsCost;
    public decimal ActualCost => ActualLaborCost + ActualPartsCost;
    
    public bool IsApprovedByCustomer { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    [MaxLength(2000)]
    public string? TechnicianNotes { get; set; }
    public ICollection<JobItemMedia> Media { get; } = new List<JobItemMedia>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
