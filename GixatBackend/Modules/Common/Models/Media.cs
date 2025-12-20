using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Common.Models;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public enum MediaType
{
    Image = 0,
    Video = 1
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class AppMedia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Uri? Url { get; set; }
    public string? Alt { get; set; }
    public MediaType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
