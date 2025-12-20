using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Media.Models;
using GixatBackend.Modules.Users.Models;

namespace GixatBackend.Modules.Organizations.Models;

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    
    public Guid? AddressId { get; set; }
    public Address? Address { get; set; }
    
    public Guid? LogoId { get; set; }
    public Media.Models.Media? Logo { get; set; }
    
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
