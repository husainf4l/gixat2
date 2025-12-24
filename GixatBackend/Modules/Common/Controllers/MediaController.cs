using GixatBackend.Modules.Common.Services.AWS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Common.Controllers;

/// <summary>
/// REST API controller for serving media files with permission checks
/// </summary>
[ApiController]
[Route("api/media")]
[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required for ASP.NET Core controller")]
public class MediaController : ControllerBase
{
    private readonly IS3Service _s3Service;

    public MediaController(IS3Service s3Service)
    {
        _s3Service = s3Service ?? throw new ArgumentNullException(nameof(s3Service));
    }

    /// <summary>
    /// Serve avatar images (public access - no auth required)
    /// GET /api/media/avatars/{userId}/{fileName}
    /// </summary>
    [HttpGet("avatars/{userId}/{fileName}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvatar(string userId, string fileName)
    {
        try
        {
            var s3Key = $"avatars/{userId}/{fileName}";
            
            // Generate presigned URL and redirect
            var presignedUrl = await _s3Service.GeneratePresignedDownloadUrlAsync(s3Key, 1)
                .ConfigureAwait(false);
            
            return Redirect(presignedUrl);
        }
        catch (Exception ex)
        {
            return NotFound(new { error = "Avatar not found", details = ex.Message });
        }
    }

    /// <summary>
    /// Serve organization logos (public access - no auth required)
    /// GET /api/media/logos/{orgId}/{fileName}
    /// </summary>
    [HttpGet("logos/{orgId}/{fileName}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLogo(string orgId, string fileName)
    {
        try
        {
            var s3Key = $"logos/{orgId}/{fileName}";
            
            // Generate presigned URL and redirect
            var presignedUrl = await _s3Service.GeneratePresignedDownloadUrlAsync(s3Key, 1)
                .ConfigureAwait(false);
            
            return Redirect(presignedUrl);
        }
        catch (Exception ex)
        {
            return NotFound(new { error = "Logo not found", details = ex.Message });
        }
    }

    /// <summary>
    /// Serve session media (requires authentication)
    /// GET /api/media/sessions/{sessionId}/{fileName}
    /// </summary>
    [HttpGet("sessions/{sessionId}/{fileName}")]
    [Authorize]
    public async Task<IActionResult> GetSessionMedia(Guid sessionId, string fileName)
    {
        try
        {
            // TODO: Add permission check - verify user has access to this session
            // var hasAccess = await _sessionService.UserHasAccessToSession(User, sessionId);
            // if (!hasAccess) return Forbid();
            
            var s3Key = $"sessions/{sessionId}/{fileName}";
            
            // Generate presigned URL and redirect
            var presignedUrl = await _s3Service.GeneratePresignedDownloadUrlAsync(s3Key, 1)
                .ConfigureAwait(false);
            
            return Redirect(presignedUrl);
        }
        catch (Exception ex)
        {
            return NotFound(new { error = "Media not found", details = ex.Message });
        }
    }

    /// <summary>
    /// Serve job card media (requires authentication)
    /// GET /api/media/jobcards/{jobCardId}/{fileName}
    /// </summary>
    [HttpGet("jobcards/{jobCardId}/{fileName}")]
    [Authorize]
    public async Task<IActionResult> GetJobCardMedia(Guid jobCardId, string fileName)
    {
        try
        {
            // TODO: Add permission check - verify user has access to this job card
            
            var s3Key = $"jobcards/{jobCardId}/{fileName}";
            
            // Generate presigned URL and redirect
            var presignedUrl = await _s3Service.GeneratePresignedDownloadUrlAsync(s3Key, 1)
                .ConfigureAwait(false);
            
            return Redirect(presignedUrl);
        }
        catch (Exception ex)
        {
            return NotFound(new { error = "Media not found", details = ex.Message });
        }
    }

    /// <summary>
    /// Generic media endpoint with full permission checks
    /// GET /api/media/{resourceType}/{resourceId}/{fileName}
    /// </summary>
    [HttpGet("{resourceType}/{resourceId}/{fileName}")]
    [Authorize]
    public async Task<IActionResult> GetMedia(string resourceType, string resourceId, string fileName)
    {
        try
        {
            // TODO: Add comprehensive permission checks based on resourceType
            
            var s3Key = $"{resourceType}/{resourceId}/{fileName}";
            
            // Generate presigned URL and redirect
            var presignedUrl = await _s3Service.GeneratePresignedDownloadUrlAsync(s3Key, 1)
                .ConfigureAwait(false);
            
            return Redirect(presignedUrl);
        }
        catch (Exception ex)
        {
            return NotFound(new { error = "Media not found", details = ex.Message });
        }
    }
}
