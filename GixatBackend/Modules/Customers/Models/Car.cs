using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Organizations.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Customers.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class Car : IMustHaveOrganization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string Make { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Model { get; set; } = string.Empty;
    
    [Required]
    [Range(1900, 2100)]
    public int Year { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string LicensePlate { get; set; } = string.Empty;
    
    [MaxLength(17)]
    [RegularExpression(@"^[A-HJ-NPR-Z0-9]{17}$", ErrorMessage = "Invalid VIN format")]
    public string? VIN { get; set; }
    
    [MaxLength(50)]
    public string? Color { get; set; }
    
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
