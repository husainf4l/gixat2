using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Common.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class Address
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Street { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string PhoneCountryCode { get; set; } = string.Empty; // e.g., 962
}
