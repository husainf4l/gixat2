using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Common.Services;
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
            [Service] IS3Service s3Service)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(s3Service);

            using var stream = file.OpenReadStream();
            var fileKey = await s3Service.UploadFileAsync(stream, file.Name, file.ContentType ?? "application/octet-stream").ConfigureAwait(false);
            var url = s3Service.GetFileUrl(fileKey);

            return new AppMedia
            {
                Url = url,
                Alt = alt,
                Type = (file.ContentType != null && file.ContentType.StartsWith("video", StringComparison.OrdinalIgnoreCase)) ? MediaType.Video : MediaType.Image
            };
        }
    }
}

