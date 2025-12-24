using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.JobCards.Models;

/// <summary>
/// Represents a comment/message in the JobCard chat/discussion thread.
/// Enables team collaboration with threaded discussions and @mentions.
/// </summary>
[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public sealed class JobCardComment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid JobCardId { get; set; }
    public JobCard? JobCard { get; set; }

    /// <summary>
    /// Optional: Link comment to specific JobItem for context
    /// </summary>
    public Guid? JobItemId { get; set; }
    public JobItem? JobItem { get; set; }

    [Required]
    [MaxLength(450)]
    public string AuthorId { get; set; } = string.Empty;
    public GixatBackend.Modules.Users.Models.ApplicationUser? Author { get; set; }

    [Required]
    [MaxLength(5000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional: For threaded replies (comment on a comment)
    /// </summary>
    public Guid? ParentCommentId { get; set; }
    public JobCardComment? ParentComment { get; set; }

    /// <summary>
    /// Child replies to this comment
    /// </summary>
    public ICollection<JobCardComment> Replies { get; } = new List<JobCardComment>();

    /// <summary>
    /// Users mentioned in this comment (@username)
    /// </summary>
    public ICollection<JobCardCommentMention> Mentions { get; } = new List<JobCardCommentMention>();

    /// <summary>
    /// Track if comment has been edited
    /// </summary>
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }

    /// <summary>
    /// Soft delete - hide but keep for audit trail
    /// </summary>
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
