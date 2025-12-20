using System.IO;
using System.Threading.Tasks;

namespace GixatBackend.Modules.Common.Services
{
    public interface IS3Service
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task DeleteFileAsync(string fileKey);
        string GetFileUrl(string fileKey);
    }
}
