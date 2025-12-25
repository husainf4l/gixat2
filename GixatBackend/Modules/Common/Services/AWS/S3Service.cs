using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using IOPath = System.IO.Path;

using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Common.Services.AWS
{
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by DI")]
    internal sealed class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public S3Service(IAmazonS3 s3Client, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            _s3Client = s3Client;
            _bucketName = Environment.GetEnvironmentVariable("AWS_S3_BUCKET_NAME") 
                          ?? configuration["AWS:S3BucketName"] 
                          ?? throw new ArgumentNullException(nameof(configuration), "AWS_S3_BUCKET_NAME not configured.");
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var fileKey = $"{Guid.NewGuid()}-{fileName}";

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = fileKey,
                BucketName = _bucketName,
                ContentType = contentType
                // No ACL - files are private by default, access via presigned URLs
            };

            using var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.UploadAsync(uploadRequest).ConfigureAwait(false);

            return fileKey;
        }

        public async Task DeleteFileAsync(string fileKey)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = fileKey
            };

            await _s3Client.DeleteObjectAsync(deleteRequest).ConfigureAwait(false);
        }

        public Uri GetFileUrl(string fileKey)
        {
            // Generate a presigned URL for secure, temporary access (24 hours)
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = fileKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(24)
            };

            var presignedUrl = _s3Client.GetPreSignedURL(request);
            return new Uri(presignedUrl);
        }

        public async Task<string> GeneratePresignedUploadUrlAsync(string fileKey, string contentType, int expiresInMinutes = 15)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = fileKey,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
                ContentType = contentType
            };

            var presignedUrl = await _s3Client.GetPreSignedURLAsync(request).ConfigureAwait(false);
            return presignedUrl;
        }

        /// <summary>
        /// Generate a presigned URL for secure file download with custom expiry
        /// </summary>
        public async Task<string> GeneratePresignedDownloadUrlAsync(string fileKey, int expiresInHours = 24)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = fileKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(expiresInHours)
            };

            var presignedUrl = await _s3Client.GetPreSignedURLAsync(request).ConfigureAwait(false);
            return presignedUrl;
        }

        public async Task<string> DownloadToTempAsync(string fileKey, ITempFileStorageService tempStorage)
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = fileKey
            };

            using var response = await _s3Client.GetObjectAsync(getRequest).ConfigureAwait(false);
            using var responseStream = response.ResponseStream;
            
            var tempFilePath = await tempStorage.SaveTempFileAsync(responseStream, IOPath.GetFileName(fileKey)).ConfigureAwait(false);
            return tempFilePath;
        }
    }
}

