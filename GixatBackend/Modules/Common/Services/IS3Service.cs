using System.IO;
using System.Threading.Tasks;
using System;

namespace GixatBackend.Modules.Common.Services
{
    internal interface IS3Service
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task DeleteFileAsync(string fileKey);
        Uri GetFileUrl(string fileKey);
    }
}

