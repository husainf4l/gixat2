using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.JobCards.Models;

/// <summary>
/// Tracks @mentions in JobCard comments for notifications
/// </summary>
[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class JobCardCommentMention
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CommentId { get; set; }
    public JobCardComment? Comment { get; set; }

    [Required]
    [MaxLength(450)]
    public string MentionedUserId { get; set; } = string.Empty;
    public GixatBackend.Modules.Users.Models.ApplicationUser? MentionedUser { get; set; }

    /// <summary>
    /// Track if the mentioned user has seen this mention
    /// </summary>
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
