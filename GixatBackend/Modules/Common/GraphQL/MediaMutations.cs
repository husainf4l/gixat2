using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Common.Services;
using GixatBackend.Modules.Common.Services.AWS;
using HotChocolate.Types;
using System.Threading.Tasks;
using HotChocolate.Authorization;

namespace GixatBackend.Modules.Common.GraphQL
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    [Authorize]
    internal static class MediaMutations
    {
        public static async Task<AppMedia> UploadMediaAsync(
            IFile file,
            string? alt,
            [Service] IS3Service s3Service,
            [Service] IVirusScanService virusScanService)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(s3Service);
            ArgumentNullException.ThrowIfNull(virusScanService);

            // Validate file
            FileValidationService.ValidateFile(file);

            // Sanitize filename
            var sanitizedFileName = FileValidationService.SanitizeFileName(file.Name);

#pragma warning disable CA2000 // Dispose objects before losing scope
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
#pragma warning restore CA2000
            using var tempStorage = new TempFileStorageService(loggerFactory.CreateLogger<TempFileStorageService>());
            
            string? tempFilePath = null;
            try
            {
                // Step 1: Save to temporary storage
                using var uploadStream = file.OpenReadStream();
                tempFilePath = await tempStorage.SaveTempFileAsync(uploadStream, sanitizedFileName).ConfigureAwait(false);

                // Step 2: Scan for viruses
                using var scanStream = tempStorage.OpenTempFile(tempFilePath);
                var scanResult = await virusScanService.ScanFileAsync(scanStream, sanitizedFileName).ConfigureAwait(false);

                if (!scanResult.IsClean)
                {
                    throw new InvalidOperationException(
                        $"File failed security scan: {scanResult.Message}" + 
                        (scanResult.ThreatName != null ? $" (Threat: {scanResult.ThreatName})" : ""));
                }

                // Step 3: Upload to S3
                using var s3Stream = tempStorage.OpenTempFile(tempFilePath);
                var fileKey = await s3Service.UploadFileAsync(s3Stream, sanitizedFileName, file.ContentType ?? "application/octet-stream").ConfigureAwait(false);
                var url = s3Service.GetFileUrl(fileKey);

                return new AppMedia
                {
                    Url = url,
                    Alt = alt,
                    Type = (file.ContentType != null && file.ContentType.StartsWith("video", StringComparison.OrdinalIgnoreCase)) ? MediaType.Video : MediaType.Image
                };
            }
            finally
            {
                // Clean up temp file
                if (tempFilePath != null)
                {
                    tempStorage.DeleteTempFile(tempFilePath);
                }
            }
        }
    }
}



