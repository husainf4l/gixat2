using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Customers.Models;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Organizations.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    
    public Guid? AddressId { get; set; }
    public Address? Address { get; set; }
    
    public Guid? LogoId { get; set; }
    public AppMedia? Logo { get; set; }
    
    public ICollection<ApplicationUser> Users { get; } = new List<ApplicationUser>();
    public ICollection<Customer> Customers { get; } = new List<Customer>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
