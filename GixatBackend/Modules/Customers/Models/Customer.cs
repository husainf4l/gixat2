using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Organizations.Models;
using GixatBackend.Modules.Sessions.Models;
using GixatBackend.Modules.JobCards.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Customers.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class Customer : IMustHaveOrganization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; set; }
    
    [Required]
    [Phone]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
    
    public Guid? AddressId { get; set; }
    public Address? Address { get; set; }
    
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    public ICollection<Car> Cars { get; } = new List<Car>();
    public ICollection<GarageSession> Sessions { get; } = new List<GarageSession>();
    public ICollection<JobCard> JobCards { get; } = new List<JobCard>();
    
    // Denormalized computed fields - updated via triggers
    public DateTime? LastSessionDate { get; set; }
    public int TotalVisits { get; set; }
    public decimal TotalSpent { get; set; }
    public int ActiveJobCards { get; set; }
    public int TotalCars { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
