using GixatBackend.Modules.Common.Models;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.JobCards.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class JobItemMedia
{
    public Guid JobItemId { get; set; }
    public JobItem? JobItem { get; set; }
    
    public Guid MediaId { get; set; }
    public AppMedia? Media { get; set; }
    
    public JobCardMediaType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
