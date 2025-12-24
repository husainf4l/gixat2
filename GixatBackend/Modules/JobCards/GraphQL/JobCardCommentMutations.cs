using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using GixatBackend.Modules.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace GixatBackend.Modules.JobCards.GraphQL;

[ExtendObjectType(OperationTypeNames.Mutation)]
[Authorize]
internal static partial class JobCardCommentMutations
{
    [GeneratedRegex(@"@(\w+)", RegexOptions.Compiled)]
    private static partial Regex MentionPattern();

    /// <summary>
    /// Add a comment/message to the JobCard chat
    /// </summary>
    public static async Task<JobCardComment> AddJobCardCommentAsync(
        Guid jobCardId,
        string content,
        Guid? jobItemId,
        Guid? parentCommentId,
        ApplicationDbContext context,
        ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new BusinessRuleViolationException("InvalidInput", "Comment content cannot be empty");
        }

        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        // Verify JobCard exists
        var jobCard = await context.JobCards
            .FirstOrDefaultAsync(jc => jc.Id == jobCardId)
            .ConfigureAwait(false);

        if (jobCard == null)
        {
            throw new EntityNotFoundException("JobCard", jobCardId);
        }

        // Verify JobItem exists if provided
        if (jobItemId.HasValue)
        {
            var jobItem = await context.JobItems
                .FirstOrDefaultAsync(ji => ji.Id == jobItemId.Value && ji.JobCardId == jobCardId)
                .ConfigureAwait(false);

            if (jobItem == null)
            {
                throw new EntityNotFoundException("JobItem", jobItemId.Value);
            }
        }

        // Verify parent comment exists if this is a reply
        if (parentCommentId.HasValue)
        {
            var parentComment = await context.JobCardComments
                .FirstOrDefaultAsync(c => c.Id == parentCommentId.Value && c.JobCardId == jobCardId)
                .ConfigureAwait(false);

            if (parentComment == null)
            {
                throw new EntityNotFoundException("ParentComment", parentCommentId.Value);
            }
        }

        var comment = new JobCardComment
        {
            JobCardId = jobCardId,
            JobItemId = jobItemId,
            AuthorId = userId,
            Content = content,
            ParentCommentId = parentCommentId
        };

        context.JobCardComments.Add(comment);

        // Extract and create mentions
        var mentions = ExtractMentions(content);
        if (mentions.Count > 0)
        {
            // Get valid user IDs from the organization
            var userIds = await context.Users
                .Where(u => mentions.Contains(u.UserName ?? string.Empty))
                .Select(u => u.Id)
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var mentionedUserId in userIds)
            {
                var mention = new JobCardCommentMention
                {
                    CommentId = comment.Id,
                    MentionedUserId = mentionedUserId
                };
                comment.Mentions.Add(mention);
                context.JobCardCommentMentions.Add(mention);
            }
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        return comment;
    }

    /// <summary>
    /// Edit an existing comment (only by author)
    /// </summary>
    public static async Task<JobCardComment> EditJobCardCommentAsync(
        Guid commentId,
        string content,
        ApplicationDbContext context,
        ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new BusinessRuleViolationException("InvalidInput", "Comment content cannot be empty");
        }

        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var comment = await context.JobCardComments
            .Include(c => c.Mentions)
            .FirstOrDefaultAsync(c => c.Id == commentId)
            .ConfigureAwait(false);

        if (comment == null)
        {
            throw new EntityNotFoundException("Comment", commentId);
        }

        // Only author can edit their comment
        if (comment.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("You can only edit your own comments");
        }

        comment.Content = content;
        comment.IsEdited = true;
        comment.EditedAt = DateTime.UtcNow;
        comment.UpdatedAt = DateTime.UtcNow;

        // Update mentions if they changed
        var newMentions = ExtractMentions(content);
        var existingMentions = comment.Mentions.Select(m => m.MentionedUserId).ToHashSet();

        // Remove old mentions
        var mentionsToRemove = comment.Mentions
            .Where(m => !newMentions.Contains(m.MentionedUser?.UserName ?? string.Empty))
            .ToList();
        foreach (var mention in mentionsToRemove)
        {
            context.JobCardCommentMentions.Remove(mention);
        }

        // Add new mentions
        var userIds = await context.Users
            .Where(u => newMentions.Contains(u.UserName ?? string.Empty) && !existingMentions.Contains(u.Id))
            .Select(u => u.Id)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var mentionedUserId in userIds)
        {
            var mention = new JobCardCommentMention
            {
                CommentId = comment.Id,
                MentionedUserId = mentionedUserId
            };
            comment.Mentions.Add(mention);
            context.JobCardCommentMentions.Add(mention);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        return comment;
    }

    /// <summary>
    /// Delete a comment (soft delete - only by author)
    /// </summary>
    public static async Task<JobCardComment> DeleteJobCardCommentAsync(
        Guid commentId,
        ApplicationDbContext context,
        ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var comment = await context.JobCardComments
            .FirstOrDefaultAsync(c => c.Id == commentId)
            .ConfigureAwait(false);

        if (comment == null)
        {
            throw new EntityNotFoundException("Comment", commentId);
        }

        // Only author can delete their comment
        if (comment.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("You can only delete your own comments");
        }

        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.UtcNow;
        comment.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync().ConfigureAwait(false);
        return comment;
    }

    /// <summary>
    /// Mark mentions as read for current user
    /// </summary>
    public static async Task<bool> MarkMentionsAsReadAsync(
        List<Guid> mentionIds,
        ApplicationDbContext context,
        ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var mentions = await context.JobCardCommentMentions
            .Where(m => mentionIds.Contains(m.Id) && m.MentionedUserId == userId)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var mention in mentions)
        {
            mention.IsRead = true;
            mention.ReadAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Extract @mentions from comment content
    /// </summary>
    private static HashSet<string> ExtractMentions(string content)
    {
        var mentions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var matches = MentionPattern().Matches(content);

        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                mentions.Add(match.Groups[1].Value);
            }
        }

        return mentions;
    }
}
