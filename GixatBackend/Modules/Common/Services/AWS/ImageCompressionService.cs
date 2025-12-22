using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using IOPath = System.IO.Path;
using Microsoft.Extensions.Logging;

namespace GixatBackend.Modules.Common.Services.AWS;

internal sealed partial class ImageCompressionService : IImageCompressionService
{
    private readonly ILogger<ImageCompressionService> _logger;

    // High-performance logging using LoggerMessage source generator
    [LoggerMessage(Level = LogLevel.Information, Message = "Image compressed successfully: {OutputPath}, Original: {OriginalWidth}x{OriginalHeight}")]
    private partial void LogImageCompressed(string outputPath, int originalWidth, int originalHeight);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to compress image: {OutputPath}")]
    private partial void LogCompressionError(Exception ex, string outputPath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Video compression not yet implemented. Using original file: {InputPath}")]
    private partial void LogVideoCompressionNotImplemented(string inputPath);

    public ImageCompressionService(ILogger<ImageCompressionService> logger)
    {
        _logger = logger;
    }

    public async Task<string> CompressImageAsync(
        Stream inputStream, 
        string outputPath, 
        int quality = 85, 
        int? maxWidth = null, 
        int? maxHeight = null)
    {
        try
        {
            using var image = await Image.LoadAsync(inputStream).ConfigureAwait(false);
            
            // Resize if dimensions specified
            if (maxWidth.HasValue || maxHeight.HasValue)
            {
                var resizeOptions = new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxWidth ?? image.Width, maxHeight ?? image.Height)
                };
                image.Mutate(x => x.Resize(resizeOptions));
            }

            // Determine format from output path
            var extension = IOPath.GetExtension(outputPath).ToUpperInvariant();
            
#pragma warning disable CA2007 // Do not use ConfigureAwait with await using
            await using var outputStream = File.Create(outputPath);
#pragma warning restore CA2007
            
            switch (extension)
            {
                case ".JPG":
                case ".JPEG":
                    await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = quality }).ConfigureAwait(false);
                    break;
                
                case ".png":
                    await image.SaveAsPngAsync(outputStream, new PngEncoder 
                    { 
                        CompressionLevel = PngCompressionLevel.BestCompression 
                    }).ConfigureAwait(false);
                    break;
                
                case ".webp":
                    await image.SaveAsWebpAsync(outputStream, new WebpEncoder 
                    { 
                        Quality = quality,
                        Method = WebpEncodingMethod.BestQuality
                    }).ConfigureAwait(false);
                    break;
                
                default:
                    // Default to JPEG for unknown formats
                    await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = quality }).ConfigureAwait(false);
                    break;
            }

            LogImageCompressed(outputPath, image.Width, image.Height);

            return outputPath;
        }
        catch (UnauthorizedAccessException ex)
        {
            LogCompressionError(ex, outputPath);
            throw;
        }
        catch (IOException ex)
        {
            LogCompressionError(ex, outputPath);
            throw;
        }
        catch (NotSupportedException ex)
        {
            LogCompressionError(ex, outputPath);
            throw;
        }
    }

    public async Task<string> CompressVideoAsync(string inputPath, string outputPath, int crf = 28)
    {
        // TODO: Implement FFmpeg video compression
        // This requires FFmpeg to be installed on the server
        // Can use FFMpegCore NuGet package for .NET wrapper
        
        LogVideoCompressionNotImplemented(inputPath);
        
        // For now, just copy the file
        File.Copy(inputPath, outputPath, overwrite: true);
        
        await Task.CompletedTask.ConfigureAwait(false);
        return outputPath;
    }
}
