using System.IO;
using System.Threading.Tasks;
using System;
using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Common.Services.AWS
{
    [SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "Used by GraphQL mutations via dependency injection")]
    public interface IS3Service
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task DeleteFileAsync(string fileKey);
        
        /// <summary>
        /// Get a presigned URL for secure file access (24 hours validity)
        /// </summary>
        Uri GetFileUrl(string fileKey);
        
        /// <summary>
        /// Generate a presigned URL for direct upload from client to S3
        /// </summary>
        Task<string> GeneratePresignedUploadUrlAsync(string fileKey, string contentType, int expiresInMinutes = 15);
        
        /// <summary>
        /// Generate a presigned URL for secure file download with custom expiry
        /// </summary>
        Task<string> GeneratePresignedDownloadUrlAsync(string fileKey, int expiresInHours = 24);
        
        /// <summary>
        /// Download a file from S3 to temporary storage
        /// </summary>
        Task<string> DownloadToTempAsync(string fileKey, TempFileStorageService tempStorage);
    }
}

