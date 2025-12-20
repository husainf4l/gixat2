using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Organizations.Models;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Customers.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class Car : IMustHaveOrganization
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
