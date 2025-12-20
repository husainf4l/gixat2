namespace GixatBackend.Modules.Common.Models;

public class Address
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string PhoneCountryCode { get; set; } = string.Empty; // e.g., 962
}
