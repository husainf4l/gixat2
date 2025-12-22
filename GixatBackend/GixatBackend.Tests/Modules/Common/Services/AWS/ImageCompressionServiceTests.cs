using FluentAssertions;
using GixatBackend.Modules.Common.Services.AWS;
using Microsoft.Extensions.Logging;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace GixatBackend.Tests.Modules.Common.Services.AWS;

public class ImageCompressionServiceTests
{
    private readonly Mock<ILogger<ImageCompressionService>> _loggerMock;
    private readonly ImageCompressionService _service;

    public ImageCompressionServiceTests()
    {
        _loggerMock = new Mock<ILogger<ImageCompressionService>>();
        _service = new ImageCompressionService(_loggerMock.Object);
    }

    [Fact]
    public async Task CompressImageAsync_ShouldCompressImage_ToJpeg()
    {
        // Arrange
        var testImage = new Image<Rgba32>(800, 600, Color.Blue);
        using var inputStream = new MemoryStream();
        await testImage.SaveAsJpegAsync(inputStream);
        inputStream.Position = 0;

        var outputPath = Path.GetTempFileName() + ".jpg";

        try
        {
            // Act
            var result = await _service.CompressImageAsync(inputStream, outputPath, quality: 75);

            // Assert
            result.Should().Be(outputPath);
            File.Exists(outputPath).Should().BeTrue();

            var fileInfo = new FileInfo(outputPath);
            fileInfo.Length.Should().BeGreaterThan(0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            testImage.Dispose();
        }
    }

    [Fact]
    public async Task CompressImageAsync_ShouldResizeImage_WhenMaxDimensionsProvided()
    {
        // Arrange
        var testImage = new Image<Rgba32>(1920, 1080, Color.Red);
        using var inputStream = new MemoryStream();
        await testImage.SaveAsJpegAsync(inputStream);
        inputStream.Position = 0;

        var outputPath = Path.GetTempFileName() + ".jpg";

        try
        {
            // Act
            var result = await _service.CompressImageAsync(
                inputStream, outputPath, quality: 85, maxWidth: 800, maxHeight: 600);

            // Assert
            result.Should().Be(outputPath);

            using var outputImage = await Image.LoadAsync(outputPath);
            outputImage.Width.Should().BeLessThanOrEqualTo(800);
            outputImage.Height.Should().BeLessThanOrEqualTo(600);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            testImage.Dispose();
        }
    }

    [Fact]
    public async Task CompressImageAsync_ShouldPreserveAspectRatio()
    {
        // Arrange
        var testImage = new Image<Rgba32>(1600, 900, Color.Green); // 16:9 aspect ratio
        using var inputStream = new MemoryStream();
        await testImage.SaveAsJpegAsync(inputStream);
        inputStream.Position = 0;

        var outputPath = Path.GetTempFileName() + ".jpg";

        try
        {
            // Act
            await _service.CompressImageAsync(
                inputStream, outputPath, maxWidth: 800, maxHeight: 800);

            // Assert
            using var outputImage = await Image.LoadAsync(outputPath);
            var aspectRatio = (double)outputImage.Width / outputImage.Height;
            aspectRatio.Should().BeApproximately(16.0 / 9.0, 0.01);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            testImage.Dispose();
        }
    }

    [Fact]
    public async Task CompressImageAsync_ShouldSupportPngFormat()
    {
        // Arrange
        var testImage = new Image<Rgba32>(400, 300, Color.Yellow);
        using var inputStream = new MemoryStream();
        await testImage.SaveAsPngAsync(inputStream);
        inputStream.Position = 0;

        var outputPath = Path.GetTempFileName() + ".png";

        try
        {
            // Act
            var result = await _service.CompressImageAsync(inputStream, outputPath);

            // Assert
            File.Exists(outputPath).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            testImage.Dispose();
        }
    }

    [Fact]
    public async Task CompressImageAsync_ShouldReduceFileSize()
    {
        // Arrange
        var testImage = new Image<Rgba32>(1920, 1080, Color.Purple);
        using var highQualityStream = new MemoryStream();
        await testImage.SaveAsJpegAsync(highQualityStream);
        var originalSize = highQualityStream.Length;

        highQualityStream.Position = 0;
        var outputPath = Path.GetTempFileName() + ".jpg";

        try
        {
            // Act - Compress with lower quality
            await _service.CompressImageAsync(
                highQualityStream, outputPath, quality: 50, maxWidth: 800);

            // Assert
            var compressedInfo = new FileInfo(outputPath);
            compressedInfo.Length.Should().BeLessThan(originalSize);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            testImage.Dispose();
        }
    }

    [Fact]
    public async Task CompressVideoAsync_ShouldCopyOriginalFile()
    {
        // Arrange
        var inputPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();

        await File.WriteAllBytesAsync(inputPath, new byte[] { 1, 2, 3, 4, 5 });

        try
        {
            // Act
            var result = await _service.CompressVideoAsync(inputPath, outputPath);

            // Assert
            result.Should().Be(outputPath);
            File.Exists(outputPath).Should().BeTrue();

            var inputBytes = await File.ReadAllBytesAsync(inputPath);
            var outputBytes = await File.ReadAllBytesAsync(outputPath);
            outputBytes.Should().BeEquivalentTo(inputBytes);
        }
        finally
        {
            if (File.Exists(inputPath))
                File.Delete(inputPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}
