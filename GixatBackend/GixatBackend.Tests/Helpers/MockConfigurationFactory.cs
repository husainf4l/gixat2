using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Cryptography;

namespace GixatBackend.Tests.Helpers;

/// <summary>
/// Factory for creating mocked IConfiguration with common test settings
/// </summary>
public static class MockConfigurationFactory
{
    public static Mock<IConfiguration> CreateWithJwtSettings()
    {
        var configMock = new Mock<IConfiguration>();

        // Generate RSA keys for testing
        using var rsa = RSA.Create();
        var privateKey = rsa.ExportRSAPrivateKeyPem();
        var publicKey = rsa.ExportRSAPublicKeyPem();

        configMock.Setup(c => c["Jwt:PrivateKey"]).Returns(privateKey);
        configMock.Setup(c => c["Jwt:PublicKey"]).Returns(publicKey);
        configMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        configMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        configMock.Setup(c => c["Jwt:ExpireDays"]).Returns("1");

        return configMock;
    }

    public static Mock<IConfiguration> CreateWithAwsSettings()
    {
        var configMock = new Mock<IConfiguration>();

        configMock.Setup(c => c["AWS:Region"]).Returns("us-east-1");
        configMock.Setup(c => c["AWS:S3BucketName"]).Returns("test-bucket");
        configMock.Setup(c => c["AWS:AccessKey"]).Returns("test-access-key");
        configMock.Setup(c => c["AWS:SecretKey"]).Returns("test-secret-key");

        return configMock;
    }
}
