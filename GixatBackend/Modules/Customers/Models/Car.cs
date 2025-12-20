using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Organizations.Models;

namespace GixatBackend.Modules.Customers.Models;

public class Car : IMustHaveOrganization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string? VIN { get; set; }
    public string? Color { get; set; }
    
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
