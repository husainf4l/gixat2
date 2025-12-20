using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Organizations.Models;

namespace GixatBackend.Modules.Customers.Models;

public class Customer : IMustHaveOrganization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    
    public Guid? AddressId { get; set; }
    public Address? Address { get; set; }
    
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    
    public ICollection<Car> Cars { get; set; } = new List<Car>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
