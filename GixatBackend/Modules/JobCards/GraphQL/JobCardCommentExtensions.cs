using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Modules.Users.Models;
using GixatBackend.Modules.Users.Services;
using GixatBackend.Modules.JobCards.Services;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.JobCards.GraphQL;

[ExtendObjectType<JobCardComment>]
[Authorize]
internal sealed class JobCardCommentExtensions
{
    /// <summary>
    /// Load comment author using DataLoader
    /// </summary>
    [GraphQLName("author")]
    public static async Task<ApplicationUser?> GetAuthorAsync(
        [Parent] JobCardComment comment,
        UserByIdDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comment);
        ArgumentNullException.ThrowIfNull(dataLoader);

        if (string.IsNullOrEmpty(comment.AuthorId))
        {
            return null;
        }

        return await dataLoader.LoadAsync(comment.AuthorId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Load JobCard using DataLoader
    /// </summary>
    [GraphQLName("jobCard")]
    public static async Task<JobCard?> GetJobCardAsync(
        [Parent] JobCardComment comment,
        JobCardByIdDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comment);
        ArgumentNullException.ThrowIfNull(dataLoader);

        return await dataLoader.LoadAsync(comment.JobCardId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Load JobItem using DataLoader (if comment is on specific item)
    /// </summary>
    [GraphQLName("jobItem")]
    public static async Task<JobItem?> GetJobItemAsync(
        [Parent] JobCardComment comment,
        JobItemByIdDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comment);
        ArgumentNullException.ThrowIfNull(dataLoader);

        if (!comment.JobItemId.HasValue)
        {
            return null;
        }

        return await dataLoader.LoadAsync(comment.JobItemId.Value, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Load parent comment using DataLoader (for threaded replies)
    /// </summary>
    [GraphQLName("parentComment")]
    public static async Task<JobCardComment?> GetParentCommentAsync(
        [Parent] JobCardComment comment,
        JobCardCommentByIdDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comment);
        ArgumentNullException.ThrowIfNull(dataLoader);

        if (!comment.ParentCommentId.HasValue)
        {
            return null;
        }

        return await dataLoader.LoadAsync(comment.ParentCommentId.Value, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Load replies using DataLoader
    /// </summary>
    [GraphQLName("replies")]
    public static async Task<IEnumerable<JobCardComment>> GetRepliesAsync(
        [Parent] JobCardComment comment,
        CommentRepliesDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comment);
        ArgumentNullException.ThrowIfNull(dataLoader);

        return await dataLoader.LoadAsync(comment.Id, cancellationToken).ConfigureAwait(false) ?? Enumerable.Empty<JobCardComment>();
    }

    /// <summary>
    /// Load mentions using DataLoader
    /// </summary>
    [GraphQLName("mentions")]
    public static async Task<IEnumerable<JobCardCommentMention>> GetMentionsAsync(
        [Parent] JobCardComment comment,
        CommentMentionsDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comment);
        ArgumentNullException.ThrowIfNull(dataLoader);

        return await dataLoader.LoadAsync(comment.Id, cancellationToken).ConfigureAwait(false) ?? Enumerable.Empty<JobCardCommentMention>();
    }
}

[ExtendObjectType<JobCardCommentMention>]
[Authorize]
internal sealed class JobCardCommentMentionExtensions
{
    /// <summary>
    /// Load mentioned user using DataLoader
    /// </summary>
    [GraphQLName("mentionedUser")]
    public static async Task<ApplicationUser?> GetMentionedUserAsync(
        [Parent] JobCardCommentMention mention,
        UserByIdDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mention);
        ArgumentNullException.ThrowIfNull(dataLoader);

        if (string.IsNullOrEmpty(mention.MentionedUserId))
        {
            return null;
        }

        return await dataLoader.LoadAsync(mention.MentionedUserId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Load comment using DataLoader
    /// </summary>
    [GraphQLName("comment")]
    public static async Task<JobCardComment?> GetCommentAsync(
        [Parent] JobCardCommentMention mention,
        JobCardCommentByIdDataLoader dataLoader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mention);
        ArgumentNullException.ThrowIfNull(dataLoader);

        return await dataLoader.LoadAsync(mention.CommentId, cancellationToken).ConfigureAwait(false);
    }
}
