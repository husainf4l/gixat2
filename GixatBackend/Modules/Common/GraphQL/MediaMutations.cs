using GixatBackend.Modules.Common.Models;
using GixatBackend.Modules.Common.Services;
using HotChocolate.Types;
using System.Threading.Tasks;

namespace GixatBackend.Modules.Common.GraphQL
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class MediaMutations
    {
        public async Task<Media> UploadMediaAsync(
            IFile file,
            string? alt,
            [Service] IS3Service s3Service)
        {
            using var stream = file.OpenReadStream();
            var fileKey = await s3Service.UploadFileAsync(stream, file.Name, file.ContentType ?? "application/octet-stream");
            var url = s3Service.GetFileUrl(fileKey);

            return new Media
            {
                Url = url,
                Alt = alt,
                Type = file.ContentType?.StartsWith("video") == true ? MediaType.Video : MediaType.Image
            };
        }
    }
}
