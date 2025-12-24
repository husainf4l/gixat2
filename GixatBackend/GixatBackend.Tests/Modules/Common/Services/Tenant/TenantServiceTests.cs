using FluentAssertions;
using GixatBackend.Modules.Common.Services.Tenant;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace GixatBackend.Tests.Modules.Common.Services.Tenant;

public class TenantServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly TenantService _tenantService;

    public TenantServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _tenantService = new TenantService(_httpContextAccessorMock.Object);
    }

    [Fact]
    public void OrganizationId_ShouldReturnNull_WhenHttpContextIsNull()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _tenantService.OrganizationId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void OrganizationId_ShouldReturnNull_WhenUserIsNull()
    {
        // Arrange
        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns((ClaimsPrincipal?)null!);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        // Act
        var result = _tenantService.OrganizationId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void OrganizationId_ShouldReturnNull_WhenOrganizationIdClaimDoesNotExist()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("UserId", Guid.NewGuid().ToString()),
            new("Email", "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(principal);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        // Act
        var result = _tenantService.OrganizationId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void OrganizationId_ShouldReturnNull_WhenOrganizationIdClaimIsInvalidGuid()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("OrganizationId", "not-a-guid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(principal);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        // Act
        var result = _tenantService.OrganizationId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void OrganizationId_ShouldReturnOrganizationId_WhenValidClaimExists()
    {
        // Arrange
        var expectedOrgId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("OrganizationId", expectedOrgId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(principal);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        // Act
        var result = _tenantService.OrganizationId;

        // Assert
        result.Should().Be(expectedOrgId);
    }

    [Fact]
    public void OrganizationId_ShouldHandleMultipleCalls_Consistently()
    {
        // Arrange
        var expectedOrgId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("OrganizationId", expectedOrgId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(principal);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        // Act
        var result1 = _tenantService.OrganizationId;
        var result2 = _tenantService.OrganizationId;
        var result3 = _tenantService.OrganizationId;

        // Assert
        result1.Should().Be(expectedOrgId);
        result2.Should().Be(expectedOrgId);
        result3.Should().Be(expectedOrgId);
    }
}
