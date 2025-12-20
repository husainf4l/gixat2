using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Common.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class Address
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string PhoneCountryCode { get; set; } = string.Empty; // e.g., 962
}
