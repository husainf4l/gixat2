namespace GixatBackend.Modules.Lookup.Models;

public class LookupItem
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
    
    public ICollection<LookupItem> Children { get; set; } = new List<LookupItem>();
    
    /// <summary>
    /// Optional metadata for extra information
    /// </summary>
    public string? Metadata { get; set; }
    
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
