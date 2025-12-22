using GixatBackend.Modules.Common.Models;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.JobCards.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class JobCardMedia
{
    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }
    
    public Guid MediaId { get; set; }
    public AppMedia? Media { get; set; }
    
    public JobCardMediaType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public enum JobCardMediaType
{
    BeforeWork = 0,
    DuringWork = 1,
    AfterWork = 2,
    Documentation = 3
}
