using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Common.Services.AWS;

[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "Used by GraphQL mutations via dependency injection")]
public interface IImageCompressionService
{
    /// <summary>
    /// Compresses an image file to reduce file size while maintaining quality
    /// </summary>
    /// <param name="inputStream">Input image stream</param>
    /// <param name="outputPath">Path where compressed image will be saved</param>
    /// <param name="quality">Compression quality (1-100, default 85)</param>
    /// <param name="maxWidth">Maximum width in pixels (optional)</param>
    /// <param name="maxHeight">Maximum height in pixels (optional)</param>
    /// <returns>Compressed image file path</returns>
    Task<string> CompressImageAsync(
        Stream inputStream, 
        string outputPath, 
        int quality = 85, 
        int? maxWidth = null, 
        int? maxHeight = null);

    /// <summary>
    /// Compresses a video file using FFmpeg
    /// </summary>
    /// <param name="inputPath">Input video file path</param>
    /// <param name="outputPath">Output video file path</param>
    /// <param name="crf">Constant Rate Factor (0-51, lower is better quality, default 28)</param>
    /// <returns>Compressed video file path</returns>
    Task<string> CompressVideoAsync(string inputPath, string outputPath, int crf = 28);
}
