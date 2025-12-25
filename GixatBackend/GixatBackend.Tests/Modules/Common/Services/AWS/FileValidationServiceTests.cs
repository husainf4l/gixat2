using FluentAssertions;
using GixatBackend.Modules.Common.Services.AWS;
using HotChocolate.Types;
using Moq;

namespace GixatBackend.Tests.Modules.Common.Services.AWS;

public class FileValidationServiceTests
{
    #region ValidateFileMetadata Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateFileMetadata_ShouldThrow_WhenFileNameIsNullOrWhiteSpace(string? fileName)
    {
        // Act & Assert
        var act = () => FileValidationService.ValidateFileMetadata(fileName!, "image/jpeg");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("File name is required");
    }

    [Theory]
    [InlineData("../etc/passwd.jpg")]
    [InlineData("../../secrets.png")]
    [InlineData("..\\windows\\system32\\file.jpg")]
    [InlineData("/etc/passwd.jpg")]
    [InlineData("C:\\Windows\\file.jpg")]
    public void ValidateFileMetadata_ShouldThrow_WhenFileNameContainsPathTraversal(string fileName)
    {
        // Act & Assert
        var act = () => FileValidationService.ValidateFileMetadata(fileName, "image/jpeg");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Invalid file name");
    }

    [Fact]
    public void ValidateFileMetadata_ShouldThrow_WhenFileNameHasNoExtension()
    {
        // Act & Assert
        var act = () => FileValidationService.ValidateFileMetadata("filewithoutext", "image/jpeg");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("File must have an extension");
    }

    [Theory]
    [InlineData("file.exe")]
    [InlineData("malware.bat")]
    [InlineData("script.sh")]
    [InlineData("document.pdf")]
    [InlineData("archive.zip")]
    public void ValidateFileMetadata_ShouldThrow_WhenFileExtensionIsNotAllowed(string fileName)
    {
        // Act & Assert
        var act = () => FileValidationService.ValidateFileMetadata(fileName, null);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("File type * is not allowed*");
    }

    [Theory]
    [InlineData("image.jpg", "image/jpeg")]
    [InlineData("photo.png", "image/png")]
    [InlineData("graphic.gif", "image/gif")]
    [InlineData("picture.webp", "image/webp")]
    [InlineData("icon.bmp", "image/bmp")]
    [InlineData("vector.svg", "image/svg+xml")]
    public void ValidateFileMetadata_ShouldSucceed_ForValidImageFiles(string fileName, string contentType)
    {
        // Act & Assert
        var act = () => FileValidationService.ValidateFileMetadata(fileName, contentType);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("video.mp4", "video/mp4")]
    [InlineData("clip.webm", "video/webm")]
    [InlineData("movie.mov", "video/quicktime")]
    [InlineData("recording.avi", "video/x-msvideo")]
    [InlineData("film.mkv", "video/x-matroska")]
    public void ValidateFileMetadata_ShouldSucceed_ForValidVideoFiles(string fileName, string contentType)
    {
        // Act & Assert
        var act = () => FileValidationService.ValidateFileMetadata(fileName, contentType);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("application/zip")]
    [InlineData("text/plain")]
    [InlineData("application/octet-stream")]
    public void ValidateFileMetadata_ShouldThrow_WhenContentTypeIsNotAllowed(string contentType)
    {
        // Act & Assert
        var act = () => FileValidationService.ValidateFileMetadata("file.jpg", contentType);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Content type '{contentType}' is not allowed");
    }

    [Theory]
    [InlineData("image.jpg", "video/mp4")]
    [InlineData("video.mp4", "image/jpeg")]
    [InlineData("photo.png", "video/webm")]
    public void ValidateFileMetadata_ShouldThrow_WhenContentTypeDoesNotMatchExtension(string fileName, string contentType)
    {
        // Act & Assert
        var act = () => FileValidationService.ValidateFileMetadata(fileName, contentType);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("File extension does not match content type");
    }

    #endregion

    #region ValidateFile Tests

    [Fact]
    public void ValidateFile_ShouldThrow_WhenFileIsNull()
    {
        // Act & Assert
        var act = () => FileValidationService.ValidateFile(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateFile_ShouldThrow_WhenFileNameIsNullOrWhiteSpace(string? fileName)
    {
        // Arrange
        var fileMock = new Mock<IFile>();
        fileMock.Setup(f => f.Name).Returns(fileName!);

        // Act & Assert
        var act = () => FileValidationService.ValidateFile(fileMock.Object);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("File name is required");
    }

    [Fact]
    public void ValidateFile_ShouldThrow_WhenFileSizeIsZero()
    {
        // Arrange
        var fileMock = new Mock<IFile>();
        fileMock.Setup(f => f.Name).Returns("image.jpg");
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        fileMock.Setup(f => f.Length).Returns(0);

        // Act & Assert
        var act = () => FileValidationService.ValidateFile(fileMock.Object);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("File is empty");
    }

    [Fact]
    public void ValidateFile_ShouldThrow_WhenImageExceedsMaxSize()
    {
        // Arrange - 11MB image (exceeds 10MB limit)
        var fileMock = new Mock<IFile>();
        fileMock.Setup(f => f.Name).Returns("large-image.jpg");
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        fileMock.Setup(f => f.Length).Returns(11 * 1024 * 1024);

        // Act & Assert
        var act = () => FileValidationService.ValidateFile(fileMock.Object);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("File size exceeds maximum allowed size of 10MB");
    }

    [Fact]
    public void ValidateFile_ShouldThrow_WhenVideoExceedsMaxSize()
    {
        // Arrange - 51MB video (exceeds 50MB limit)
        var fileMock = new Mock<IFile>();
        fileMock.Setup(f => f.Name).Returns("large-video.mp4");
        fileMock.Setup(f => f.ContentType).Returns("video/mp4");
        fileMock.Setup(f => f.Length).Returns(51L * 1024 * 1024);

        // Act & Assert
        var act = () => FileValidationService.ValidateFile(fileMock.Object);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("File size exceeds maximum allowed size of 50MB");
    }

    [Fact]
    public void ValidateFile_ShouldSucceed_ForValidImageWithinSizeLimit()
    {
        // Arrange - 5MB image (within 10MB limit)
        var fileMock = new Mock<IFile>();
        fileMock.Setup(f => f.Name).Returns("valid-image.jpg");
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        fileMock.Setup(f => f.Length).Returns(5 * 1024 * 1024);

        // Act & Assert
        var act = () => FileValidationService.ValidateFile(fileMock.Object);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateFile_ShouldSucceed_ForValidVideoWithinSizeLimit()
    {
        // Arrange - 30MB video (within 50MB limit)
        var fileMock = new Mock<IFile>();
        fileMock.Setup(f => f.Name).Returns("valid-video.mp4");
        fileMock.Setup(f => f.ContentType).Returns("video/mp4");
        fileMock.Setup(f => f.Length).Returns(30 * 1024 * 1024);

        // Act & Assert
        var act = () => FileValidationService.ValidateFile(fileMock.Object);
        act.Should().NotThrow();
    }

    #endregion

    #region SanitizeFileName Tests

    [Fact]
    public void SanitizeFileName_ShouldRemovePathTraversal()
    {
        // Arrange
        var maliciousFileName = "../../../etc/passwd.jpg";

        // Act
        var result = FileValidationService.SanitizeFileName(maliciousFileName);

        // Assert
        result.Should().NotContain("..");
        result.Should().EndWith(".jpg");
    }

    [Fact]
    public void SanitizeFileName_ShouldReplaceInvalidCharactersWithUnderscore()
    {
        // Arrange
        var fileNameWithInvalidChars = "file<>:|?*.jpg";

        // Act
        var result = FileValidationService.SanitizeFileName(fileNameWithInvalidChars);

        // Assert
        result.Should().NotContain("<");
        result.Should().NotContain(">");
        result.Should().NotContain(":");
        result.Should().NotContain("|");
        result.Should().NotContain("?");
        result.Should().EndWith(".jpg");
    }

    [Fact]
    public void SanitizeFileName_ShouldAddTimestampToPreventCollisions()
    {
        // Arrange
        var fileName = "myfile.jpg";

        // Act
        var result1 = FileValidationService.SanitizeFileName(fileName);
        Thread.Sleep(1100); // Wait to ensure different timestamp
        var result2 = FileValidationService.SanitizeFileName(fileName);

        // Assert
        result1.Should().NotBe(result2);
        result1.Should().Contain("myfile_");
        result2.Should().Contain("myfile_");
        result1.Should().EndWith(".jpg");
        result2.Should().EndWith(".jpg");
    }

    [Fact]
    public void SanitizeFileName_ShouldPreserveExtension()
    {
        // Arrange
        var fileName = "document.pdf";

        // Act
        var result = FileValidationService.SanitizeFileName(fileName);

        // Assert
        result.Should().EndWith(".pdf");
    }

    [Theory]
    [InlineData("C:\\Windows\\file.jpg", "file")]
    [InlineData("/etc/passwd.jpg", "passwd")]
    [InlineData("folder/subfolder/file.jpg", "file")]
    public void SanitizeFileName_ShouldStripPathInformation(string fileNameWithPath, string expectedBaseName)
    {
        // Act
        var result = FileValidationService.SanitizeFileName(fileNameWithPath);

        // Assert
        result.Should().NotContain("\\");
        result.Should().NotContain("/");
        result.Should().Contain($"{expectedBaseName}_");
        result.Should().EndWith(".jpg");
        // Should contain timestamp in format YYYYMMDDHHMMSS
        result.Should().MatchRegex($@"{expectedBaseName}_\d{{14}}\.jpg$");
    }

    #endregion
}
