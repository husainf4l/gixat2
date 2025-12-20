using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Common.Services
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
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead // Adjust based on your needs
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
            // For me-central-1 and other regions, the URL format is:
            // https://{bucket-name}.s3.{region}.amazonaws.com/{key}
            var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "me-central-1";
            return new Uri($"https://{_bucketName}.s3.{region}.amazonaws.com/{fileKey}");
        }
    }
}

