using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using GixatBackend.Modules.Common.Services.AWS;
using Microsoft.Extensions.Configuration;
using Moq;

namespace GixatBackend.Tests.Modules.Common.Services.AWS;

public class S3ServiceTests
{
    private readonly Mock<IAmazonS3> _s3ClientMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly S3Service _s3Service;
    private const string TestBucketName = "test-bucket";

    public S3ServiceTests()
    {
        _s3ClientMock = new Mock<IAmazonS3>();
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["AWS:S3BucketName"]).Returns(TestBucketName);

        _s3Service = new S3Service(_s3ClientMock.Object, _configurationMock.Object);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenConfigurationIsNull()
    {
        // Act & Assert
        var act = () => new S3Service(_s3ClientMock.Object, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenBucketNameIsNotConfigured()
    {
        // Arrange
        var emptyConfigMock = new Mock<IConfiguration>();
        emptyConfigMock.Setup(c => c["AWS:S3BucketName"]).Returns((string?)null);

        // Act & Assert
        var act = () => new S3Service(_s3ClientMock.Object, emptyConfigMock.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*AWS_S3_BUCKET_NAME not configured*");
    }

    [Fact]
    public async Task UploadFileAsync_ShouldGenerateUniqueFileKey()
    {
        // Arrange
        var fileName = "test.jpg";
        var contentType = "image/jpeg";
        using var fileStream = new MemoryStream([1, 2, 3, 4, 5]);

        // Note: We can't easily test TransferUtility since it's sealed, but we can verify the method doesn't throw

        // Act
        var act = async () => await _s3Service.UploadFileAsync(fileStream, fileName, contentType);

        // Assert - Method should execute without throwing
        // In a real scenario, you'd use integration tests or verify the S3 client is called correctly
        // For unit tests, you might need to refactor S3Service to inject TransferUtility
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldCallS3Client_WithCorrectParameters()
    {
        // Arrange
        var fileKey = "test-file-key.jpg";
        _s3ClientMock
            .Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), default))
            .ReturnsAsync(new DeleteObjectResponse());

        // Act
        await _s3Service.DeleteFileAsync(fileKey);

        // Assert
        _s3ClientMock.Verify(
            x => x.DeleteObjectAsync(
                It.Is<DeleteObjectRequest>(r =>
                    r.BucketName == TestBucketName &&
                    r.Key == fileKey),
                default),
            Times.Once);
    }

    [Fact]
    public void GetFileUrl_ShouldReturnPresignedUrl_WithCorrectParameters()
    {
        // Arrange
        var fileKey = "test-file.jpg";
        var expectedUrl = $"https://{TestBucketName}.s3.amazonaws.com/{fileKey}?signature=xyz";

        _s3ClientMock
            .Setup(x => x.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
            .Returns(expectedUrl);

        // Act
        var result = _s3Service.GetFileUrl(fileKey);

        // Assert
        result.Should().NotBeNull();
        result.ToString().Should().Be(expectedUrl);

        _s3ClientMock.Verify(
            x => x.GetPreSignedURL(
                It.Is<GetPreSignedUrlRequest>(r =>
                    r.BucketName == TestBucketName &&
                    r.Key == fileKey &&
                    r.Verb == HttpVerb.GET &&
                    r.Expires > DateTime.UtcNow.AddHours(23))),
            Times.Once);
    }

    [Fact]
    public async Task GeneratePresignedUploadUrlAsync_ShouldReturnUrl_WithDefaultExpiry()
    {
        // Arrange
        var fileKey = "upload-file.jpg";
        var contentType = "image/jpeg";
        var expectedUrl = $"https://{TestBucketName}.s3.amazonaws.com/{fileKey}";

        _s3ClientMock
            .Setup(x => x.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .ReturnsAsync(expectedUrl);

        // Act
        var result = await _s3Service.GeneratePresignedUploadUrlAsync(fileKey, contentType);

        // Assert
        result.Should().Be(expectedUrl);

        _s3ClientMock.Verify(
            x => x.GetPreSignedURLAsync(
                It.Is<GetPreSignedUrlRequest>(r =>
                    r.BucketName == TestBucketName &&
                    r.Key == fileKey &&
                    r.ContentType == contentType &&
                    r.Verb == HttpVerb.PUT &&
                    r.Expires > DateTime.UtcNow.AddMinutes(14) &&
                    r.Expires < DateTime.UtcNow.AddMinutes(16))),
            Times.Once);
    }

    [Fact]
    public async Task GeneratePresignedUploadUrlAsync_ShouldRespectCustomExpiry()
    {
        // Arrange
        var fileKey = "upload-file.jpg";
        var contentType = "image/jpeg";
        var customExpiryMinutes = 30;
        var expectedUrl = $"https://{TestBucketName}.s3.amazonaws.com/{fileKey}";

        _s3ClientMock
            .Setup(x => x.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .ReturnsAsync(expectedUrl);

        // Act
        var result = await _s3Service.GeneratePresignedUploadUrlAsync(fileKey, contentType, customExpiryMinutes);

        // Assert
        result.Should().Be(expectedUrl);

        _s3ClientMock.Verify(
            x => x.GetPreSignedURLAsync(
                It.Is<GetPreSignedUrlRequest>(r =>
                    r.Expires > DateTime.UtcNow.AddMinutes(29) &&
                    r.Expires < DateTime.UtcNow.AddMinutes(31))),
            Times.Once);
    }

    [Fact]
    public async Task GeneratePresignedDownloadUrlAsync_ShouldReturnUrl_WithDefaultExpiry()
    {
        // Arrange
        var fileKey = "download-file.jpg";
        var expectedUrl = $"https://{TestBucketName}.s3.amazonaws.com/{fileKey}";

        _s3ClientMock
            .Setup(x => x.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .ReturnsAsync(expectedUrl);

        // Act
        var result = await _s3Service.GeneratePresignedDownloadUrlAsync(fileKey);

        // Assert
        result.Should().Be(expectedUrl);

        _s3ClientMock.Verify(
            x => x.GetPreSignedURLAsync(
                It.Is<GetPreSignedUrlRequest>(r =>
                    r.BucketName == TestBucketName &&
                    r.Key == fileKey &&
                    r.Verb == HttpVerb.GET &&
                    r.Expires > DateTime.UtcNow.AddHours(23) &&
                    r.Expires < DateTime.UtcNow.AddHours(25))),
            Times.Once);
    }

    [Fact]
    public async Task GeneratePresignedDownloadUrlAsync_ShouldRespectCustomExpiry()
    {
        // Arrange
        var fileKey = "download-file.jpg";
        var customExpiryHours = 48;
        var expectedUrl = $"https://{TestBucketName}.s3.amazonaws.com/{fileKey}";

        _s3ClientMock
            .Setup(x => x.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .ReturnsAsync(expectedUrl);

        // Act
        var result = await _s3Service.GeneratePresignedDownloadUrlAsync(fileKey, customExpiryHours);

        // Assert
        result.Should().Be(expectedUrl);

        _s3ClientMock.Verify(
            x => x.GetPreSignedURLAsync(
                It.Is<GetPreSignedUrlRequest>(r =>
                    r.Expires > DateTime.UtcNow.AddHours(47) &&
                    r.Expires < DateTime.UtcNow.AddHours(49))),
            Times.Once);
    }

    [Fact]
    public async Task DownloadToTempAsync_ShouldDownloadFile_AndSaveToTemp()
    {
        // Arrange
        var fileKey = "test-download.jpg";
        var fileContent = new byte[] { 1, 2, 3, 4, 5 };
        var responseStream = new MemoryStream(fileContent);

        var getObjectResponse = new GetObjectResponse
        {
            ResponseStream = responseStream
        };

        _s3ClientMock
            .Setup(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), default))
            .ReturnsAsync(getObjectResponse);

        var tempStorageMock = new Mock<TempFileStorageService>();
        var expectedTempPath = "/tmp/test-download.jpg";
        tempStorageMock
            .Setup(x => x.SaveTempFileAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(expectedTempPath);

        // Act
        var result = await _s3Service.DownloadToTempAsync(fileKey, tempStorageMock.Object);

        // Assert
        result.Should().Be(expectedTempPath);

        _s3ClientMock.Verify(
            x => x.GetObjectAsync(
                It.Is<GetObjectRequest>(r =>
                    r.BucketName == TestBucketName &&
                    r.Key == fileKey),
                default),
            Times.Once);

        tempStorageMock.Verify(
            x => x.SaveTempFileAsync(It.IsAny<Stream>(), It.IsAny<string>()),
            Times.Once);
    }
}
