using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Lookup.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class LookupItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The category of the lookup (e.g., "CarMake", "CarModel", "CarColor")
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// The display value (e.g., "Toyota")
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional parent ID for hierarchical lookups (e.g., Camry's parent is Toyota)
    /// </summary>
    public Guid? ParentId { get; set; }
    public LookupItem? Parent { get; set; }
    
    public ICollection<LookupItem> Children { get; } = new List<LookupItem>();
    
    /// <summary>
    /// Optional metadata for extra information
    /// </summary>
    public string? Metadata { get; set; }
    
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
