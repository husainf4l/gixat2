using GixatBackend.Data;
using GixatBackend.Modules.JobCards.Models;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;
using System.Security.Claims;

namespace GixatBackend.Modules.JobCards.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
[Authorize]
internal static class JobCardCommentQueries
{
    /// <summary>
    /// Get all comments for a JobCard (chat thread)
    /// </summary>
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<JobCardComment> GetJobCardComments(
        Guid jobCardId,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.JobCardComments
            .AsNoTracking()
            .Where(c => c.JobCardId == jobCardId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt);
    }

    /// <summary>
    /// Get comments for a specific JobItem
    /// </summary>
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<JobCardComment> GetJobItemComments(
        Guid jobItemId,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.JobCardComments
            .AsNoTracking()
            .Where(c => c.JobItemId == jobItemId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt);
    }

    /// <summary>
    /// Get all unread mentions for the current user
    /// </summary>
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<JobCardCommentMention> GetMyUnreadMentions(
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

        return context.JobCardCommentMentions
            .AsNoTracking()
            .Include(m => m.Comment)
                .ThenInclude(c => c!.JobCard)
            .Include(m => m.Comment)
                .ThenInclude(c => c!.Author)
            .Where(m => m.MentionedUserId == userId && !m.IsRead && !m.Comment!.IsDeleted)
            .OrderByDescending(m => m.CreatedAt);
    }

    /// <summary>
    /// Get unread mention count for the current user
    /// </summary>
    public static async Task<int> GetUnreadMentionCountAsync(
        ApplicationDbContext context,
        ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return 0;
        }

        return await context.JobCardCommentMentions
            .Where(m => m.MentionedUserId == userId && !m.IsRead && !m.Comment!.IsDeleted)
            .CountAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get a single comment by ID
    /// </summary>
    public static async Task<JobCardComment?> GetCommentByIdAsync(
        Guid commentId,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return await context.JobCardComments
            .AsNoTracking()
            .Include(c => c.Author)
            .Include(c => c.Mentions)
                .ThenInclude(m => m.MentionedUser)
            .Include(c => c.Replies.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Author)
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get recent activity (comments) across all JobCards in organization
    /// Useful for activity feed/timeline
    /// </summary>
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<JobCardComment> GetRecentJobCardActivity(
        Guid organizationId,
        ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.JobCardComments
            .AsNoTracking()
            .Include(c => c.JobCard)
            .Where(c => c.JobCard!.OrganizationId == organizationId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt);
    }
}
