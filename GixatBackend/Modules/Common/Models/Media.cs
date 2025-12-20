namespace GixatBackend.Modules.Common.Models;

public enum MediaType
{
    Image = 0,
    Video = 1
}

public class Media
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = string.Empty;
    public string? Alt { get; set; }
    public MediaType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
