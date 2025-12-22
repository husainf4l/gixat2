using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Customers.Models;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Sessions.Models;
using System.ComponentModel.DataAnnotations;
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
    
    [Required]
    public Guid CarId { get; set; }
    public Car? Car { get; set; }
    
    [Required]
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    [Required]
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    [MaxLength(450)]
    public string? AssignedTechnicianId { get; set; }
    public GixatBackend.Modules.Users.Models.ApplicationUser? AssignedTechnician { get; set; }
    
    [Required]
    public JobCardStatus Status { get; set; } = JobCardStatus.Pending;
    
    [MaxLength(5000)]
    public string? InternalNotes { get; set; }
    
    public ICollection<JobItem> Items { get; } = new List<JobItem>();
    public ICollection<JobCardMedia> Media { get; } = new List<JobCardMedia>();
    
    [Range(0, double.MaxValue)]
    public decimal TotalEstimatedCost { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal TotalActualCost { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal TotalEstimatedLabor { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal TotalActualLabor { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal TotalEstimatedParts { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal TotalActualParts { get; set; }
    
    public bool IsApprovedByCustomer { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
